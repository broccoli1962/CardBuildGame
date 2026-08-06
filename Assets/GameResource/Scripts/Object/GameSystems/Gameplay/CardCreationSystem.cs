using System;
using System.Collections.Generic;
using System.Threading;
using Backend.Object.GameSystems.Llm;
using Backend.Object.Management;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace Backend.Object.GameSystems.Gameplay
{
    /// <summary>
    /// 전투 클리어 후 AI 카드 생성 세션을 관리합니다.
    /// </summary>
    public static class CardCreationSystem
    {
        #region Fields

        private static readonly Subject<Unit> _onSessionStarted = new();
        private static readonly Subject<Unit> _onSessionEnded = new();
        private static readonly Subject<Unit> _onStateChanged = new();
        private static readonly List<GeneratedCardData> _candidates = new();

        private static bool _isActive;
        private static bool _isGenerating;
        private static bool _hasGenerated;
        private static bool _isEliteDraft;
        private static int _selectedIndex;
        private static MapNode _sourceNode;
        private static CancellationTokenSource _generateCts;

        #endregion

        #region Properties

        public static Observable<Unit> OnSessionStarted => _onSessionStarted;
        public static Observable<Unit> OnSessionEnded => _onSessionEnded;
        public static Observable<Unit> OnStateChanged => _onStateChanged;
        public static bool IsActive => _isActive;
        public static bool IsGenerating => _isGenerating;
        public static bool HasGenerated => _hasGenerated;
        public static bool IsEliteDraft => _isEliteDraft;
        public static int SelectedIndex => _selectedIndex;
        public static IReadOnlyList<GeneratedCardData> Candidates => _candidates;
        public static MapNode SourceNode => _sourceNode;

        public static GeneratedCardData SelectedCard =>
            _selectedIndex >= 0 && _selectedIndex < _candidates.Count ? _candidates[_selectedIndex] : null;

        #endregion

        #region Public Methods

        public static void Initialize()
        {
            ResetSession();
        }

        public static void Dispose()
        {
            CancelGenerate();
            ResetSession();
        }

        /// <summary>
        /// 현재 클리어한 노드가 카드 생성 기회를 주는지 확인합니다.
        /// </summary>
        public static bool ShouldOfferCreation(MapNode node)
        {
            if (node.NodeType is MapNodeType.Boss)
                return false;

            var typeData = TableManager.GetMapNodeType(node.NodeType);
            if (typeData == null)
                return node.NodeType is MapNodeType.Battle or MapNodeType.Elite;

            return typeData.has_battle && typeData.gen_count > 0;
        }

        /// <summary>
        /// 카드 생성 세션을 시작합니다.
        /// </summary>
        public static void BeginSession(MapNode node)
        {
            CancelGenerate();
            _isActive = true;
            _isGenerating = false;
            _hasGenerated = false;
            _selectedIndex = 0;
            _sourceNode = node;
            _isEliteDraft = node.NodeType == MapNodeType.Elite;
            _candidates.Clear();

            if (_isEliteDraft)
            {
                var bonus = TableManager.GetInt(TableManager.BalanceKey.EliteBonusMaxHp, 2);
                if (bonus != 0)
                    PlayerStateSystem.ChangeMaxHp(bonus);
            }

            GameManager.SetPhase(GamePhase.CardGeneration);
            _onSessionStarted.OnNext(Unit.Default);
            _onStateChanged.OnNext(Unit.Default);
        }

        public static async UniTask GenerateAsync(string concept)
        {
            if (!_isActive || _hasGenerated || _isGenerating)
                return;

            _isGenerating = true;
            _onStateChanged.OnNext(Unit.Default);

            CancelGenerate();
            _generateCts = new CancellationTokenSource();
            var token = _generateCts.Token;

            try
            {
                _candidates.Clear();

                if (_isEliteDraft)
                {
                    var first = await CardGenerationService.GenerateAsync(concept, 0.7f, token);
                    var second = await CardGenerationService.GenerateAsync(concept, 1.05f, token);
                    if (first.Card != null)
                        _candidates.Add(first.Card);
                    if (second.Card != null)
                        _candidates.Add(second.Card);
                }
                else
                {
                    var result = await CardGenerationService.GenerateAsync(concept, null, token);
                    if (result.Card != null)
                        _candidates.Add(result.Card);
                }

                if (_candidates.Count == 0)
                    _candidates.Add(FallbackCardGenerator.Generate(concept));

                _selectedIndex = 0;
                _hasGenerated = true;
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning("[CardCreationSystem] Generation cancelled.");
            }
            finally
            {
                _isGenerating = false;
                _onStateChanged.OnNext(Unit.Default);
            }
        }

        public static void SelectCandidate(int index)
        {
            if (!_hasGenerated || index < 0 || index >= _candidates.Count)
                return;

            _selectedIndex = index;
            _onStateChanged.OnNext(Unit.Default);
        }

        /// <summary>
        /// 선택한 생성 카드를 덱에 추가하고 맵으로 복귀합니다.
        /// </summary>
        public static bool ConfirmAddSelected()
        {
            if (!_isActive || !_hasGenerated)
                return false;

            var card = SelectedCard;
            if (card == null)
                return false;

            if (!DeckSystem.TryAddGeneratedCard(card, out var runtime))
                return false;

            MapSystem.AddThreat(runtime.PowerScore);
            EndSessionAndReturnToMap();
            return true;
        }

        /// <summary>
        /// 생성 없이, 또는 생성 카드를 버리고 맵으로 복귀합니다.
        /// </summary>
        public static void SkipAndReturnToMap()
        {
            if (!_isActive)
                return;

            CancelGenerate();
            EndSessionAndReturnToMap();
        }

        /// <summary>
        /// 위협도 미리보기용: 선택 카드 추가 시 예상 위협도/티어.
        /// </summary>
        public static void GetThreatPreview(out float currentThreat, out float nextThreat, out int currentTier, out int nextTier)
        {
            currentThreat = MapSystem.ThreatScore.CurrentValue;
            currentTier = MapSystem.ThreatTier;
            var cps = SelectedCard?.power_score ?? 0f;
            nextThreat = Mathf.Max(0f, currentThreat + cps);
            nextTier = TableManager.GetThreatTier(nextThreat);
        }

        #endregion

        #region Private

        private static void EndSessionAndReturnToMap()
        {
            _isActive = false;
            _isGenerating = false;
            CancelGenerate();
            _onSessionEnded.OnNext(Unit.Default);
            _onStateChanged.OnNext(Unit.Default);
            MapSystem.CompleteCurrentNode();
            ResetSessionFields();
        }

        private static void CancelGenerate()
        {
            if (_generateCts == null)
                return;

            _generateCts.Cancel();
            _generateCts.Dispose();
            _generateCts = null;
        }

        private static void ResetSession()
        {
            CancelGenerate();
            ResetSessionFields();
        }

        private static void ResetSessionFields()
        {
            _isActive = false;
            _isGenerating = false;
            _hasGenerated = false;
            _isEliteDraft = false;
            _selectedIndex = 0;
            _sourceNode = default;
            _candidates.Clear();
        }

        #endregion
    }
}
