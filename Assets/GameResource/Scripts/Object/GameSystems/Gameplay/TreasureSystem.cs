using Backend.Object.Management;
using R3;
using UnityEngine;

namespace Backend.Object.GameSystems.Gameplay
{
    /// <summary>
    /// 맵 Treasure 노드의 영구 강화 선택과 세션 생명주기를 관리합니다.
    /// </summary>
    public static class TreasureSystem
    {
        private static readonly Subject<Unit> _onTreasureStarted = new();
        private static readonly Subject<Unit> _onTreasureEnded = new();

        private static bool _isActive;
        private static MapNode _sourceNode;

        public static Observable<Unit> OnTreasureStarted => _onTreasureStarted;
        public static Observable<Unit> OnTreasureEnded => _onTreasureEnded;
        public static bool IsActive => _isActive;
        public static MapNode SourceNode => _sourceNode;

        public static void Initialize()
        {
            ResetState();
        }

        public static void Dispose()
        {
            ResetState();
        }

        /// <summary>
        /// Treasure 노드 진입 시 세션을 시작합니다.
        /// </summary>
        public static bool BeginTreasure(MapNode node)
        {
            if (node.NodeType != MapNodeType.Treasure)
            {
                Debug.LogWarning($"[TreasureSystem] BeginTreasure called with non-treasure node: {node.NodeType}");
                return false;
            }

            _isActive = true;
            _sourceNode = node;
            GameManager.SetPhase(GamePhase.TreasureNode);
            _onTreasureStarted.OnNext(Unit.Default);
            return true;
        }

        /// <summary>
        /// 최대 체력을 영구적으로 올립니다.
        /// </summary>
        public static void ChooseLife()
        {
            if (!_isActive)
                return;

            var option = FindOption(TreasureOptionType.LifeGrail);
            var amount = option != null ? Mathf.Max(1, option.value) : 3;
            PlayerStateSystem.ChangeMaxHp(amount);
            FinishAndReturnToMap();
        }

        /// <summary>
        /// 최대 마나를 영구적으로 올립니다.
        /// </summary>
        public static void ChooseMana()
        {
            if (!_isActive)
                return;

            var option = FindOption(TreasureOptionType.ManaSource);
            var amount = option != null ? Mathf.Max(1, option.value) : 1;
            PlayerStateSystem.ChangeMaxMana(amount);
            PlayerStateSystem.RecoverMana(amount);
            FinishAndReturnToMap();
        }

        /// <summary>
        /// 보상을 받지 않고 떠납니다.
        /// </summary>
        public static void ChoosePass()
        {
            if (!_isActive)
                return;

            FinishAndReturnToMap();
        }

        private static TreasureOptionData FindOption(TreasureOptionType type)
        {
            var options = TableManager.GetTreasureOptions();
            for (var i = 0; i < options.Count; i++)
            {
                var option = options[i];
                if (option != null && option.effect_type == type)
                    return option;
            }

            return null;
        }

        private static void FinishAndReturnToMap()
        {
            _isActive = false;
            _onTreasureEnded.OnNext(Unit.Default);
            MapSystem.CompleteCurrentNode();
            ResetState();
        }

        private static void ResetState()
        {
            _isActive = false;
            _sourceNode = default;
        }
    }
}
