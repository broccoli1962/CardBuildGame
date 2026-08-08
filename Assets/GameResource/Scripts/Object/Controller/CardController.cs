using System;
using System.Collections.Generic;
using System.Threading;
using Backend.AddressableKey;
using Backend.Object.GameSystems.Gameplay;
using Backend.Object.Management;
using Backend.Object.Management.Pool;
using Backend.Object.UI;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using R3;
using UnityEngine;

namespace Backend.Object.Controller
{
    public class CardController : MonoBehaviour
    {
        [SerializeField] private float _cardSpacing = 20f;
        [SerializeField] private float _discardDuration = 0.35f;
        [SerializeField] private float _discardStagger = 0.05f;
        [SerializeField] private float _drawDuration = 0.3f;
        [SerializeField] private float _drawStagger = 0.08f;
        [SerializeField] private float _layoutDuration = 0.2f;
        [SerializeField] private float _travelStartScale = 0.2f;

        private RectTransform _container;
        private RectTransform _drawPileAnchor;
        private RectTransform _discardPileAnchor;
        private Pooling<Card> _pool;
        private readonly List<Card> _activeCards = new();
        private readonly HashSet<Card> _leasedCards = new();
        private readonly HashSet<int> _settledUids = new();
        private CompositeDisposable _disposables;
        private CancellationTokenSource _refreshCts;
        private Canvas _canvas;
        private bool _isRefreshing;
        private int _refreshVersion;

        /// <summary>
        /// 카드 풀을 생성하고 손패 변경 구독을 시작합니다.
        /// </summary>
        public async UniTask InitializeAsync(
            RectTransform container,
            RectTransform drawPileAnchor = null,
            RectTransform discardPileAnchor = null)
        {
            if (container == null)
            {
                Debug.LogError("[CardController] container is null.");
                return;
            }

            _container = container;
            _drawPileAnchor = drawPileAnchor;
            _discardPileAnchor = discardPileAnchor;
            _canvas = container.GetComponentInParent<Canvas>();

            _pool = await ObjectPoolManager.GetOrCreatePoolAsync<Card>(
                AddressableKeys.UI.Get<Card>(),
                parent: container);

            if (_pool == null)
            {
                Debug.LogError("[CardController] Failed to create card pool.");
                return;
            }

            _disposables?.Dispose();
            _disposables = new CompositeDisposable();

            DeckSystem.OnHandChanged
                .Subscribe(_ => RefreshHandAnimated().Forget())
                .AddTo(_disposables);

            // DeckSystem.Initialize 시점의 OnHandChanged는 구독 전에 이미 지나갔을 수 있다.
            RefreshHandAnimated().Forget();
        }

        private async UniTaskVoid RefreshHandAnimated()
        {
            if (_pool == null || _container == null)
                return;

            var interrupted = _isRefreshing;
            _refreshCts?.Cancel();
            _refreshCts?.Dispose();

            // 딜/버리기 도중 재진입하면, 비행 중인 임대 카드까지 즉시 회수한다.
            // (toRemove 애니 중인 카드는 _activeCards에 없어 이전에는 풀 반환이 늦어졌음)
            if (interrupted)
                ReleaseAllLeasedCards();

            var version = ++_refreshVersion;
            _isRefreshing = true;
            _refreshCts = new CancellationTokenSource();
            var token = _refreshCts.Token;

            try
            {
                await ApplyHandDeltaAsync(token);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                if (version == _refreshVersion)
                    _isRefreshing = false;
            }
        }

        private async UniTask ApplyHandDeltaAsync(CancellationToken token)
        {
            // 다음 스테이지/전투 시작: 동일 Uid 재사용을 kept로 오인하지 않고 드로우 파일에서 다시 딜한다.
            if (DeckSystem.ConsumeFullHandReset())
                ReleaseAllLeasedCards();

            var desired = new List<RuntimeCard>(DeckSystem.Hand);
            var desiredUids = new HashSet<int>(desired.Count);
            for (var i = 0; i < desired.Count; i++)
                desiredUids.Add(desired[i].Uid);

            // 손패 진입 연출이 끝나지 않은 카드는 kept 대상에서 제외하고 다시 드로우한다.
            for (var i = _activeCards.Count - 1; i >= 0; i--)
            {
                var card = _activeCards[i];
                var runtime = card != null ? card.BoundCard : null;
                if (card == null || runtime == null || !_settledUids.Contains(runtime.Uid))
                {
                    ReleaseCard(card);
                    _activeCards.RemoveAt(i);
                    if (runtime != null)
                        _settledUids.Remove(runtime.Uid);
                }
            }

            var toRemove = new List<(Card card, RuntimeCard runtime)>();
            for (var i = _activeCards.Count - 1; i >= 0; i--)
            {
                var card = _activeCards[i];
                var runtime = card != null ? card.BoundCard : null;
                if (card == null || runtime == null || !desiredUids.Contains(runtime.Uid))
                {
                    if (card != null)
                        toRemove.Add((card, runtime));
                    if (runtime != null)
                        _settledUids.Remove(runtime.Uid);
                    _activeCards.RemoveAt(i);
                }
            }

            var keptByUid = new Dictionary<int, Card>(_activeCards.Count);
            for (var i = 0; i < _activeCards.Count; i++)
            {
                var card = _activeCards[i];
                if (card?.BoundCard != null)
                    keptByUid[card.BoundCard.Uid] = card;
            }

            var drawnCards = new List<Card>();
            var drawnSet = new HashSet<Card>();
            _activeCards.Clear();

            for (var i = 0; i < desired.Count; i++)
            {
                var runtime = desired[i];
                if (keptByUid.TryGetValue(runtime.Uid, out var existing) &&
                    _settledUids.Contains(runtime.Uid))
                {
                    _activeCards.Add(existing);
                    keptByUid.Remove(runtime.Uid);
                    continue;
                }

                if (keptByUid.TryGetValue(runtime.Uid, out var unsettledExisting))
                {
                    ReleaseCard(unsettledExisting);
                    keptByUid.Remove(runtime.Uid);
                }

                var card = SpawnCard(runtime);
                if (card == null)
                    continue;

                card.SetInteractable(false);
                var start = GetAnchorLocalPoint(_drawPileAnchor);
                card.CachedRectTransform.anchoredPosition = start;
                card.CachedTransform.localScale = Vector3.one * _travelStartScale;
                _activeCards.Add(card);
                drawnCards.Add(card);
                drawnSet.Add(card);
            }

            foreach (var orphan in keptByUid.Values)
                ReleaseCard(orphan);

            // 드로우/레이아웃 연출 중에는 이미 자리 잡은 카드도 사용할 수 없게 막는다.
            // (중간에 사용하면 OnHandChanged로 리프레시가 끊겨 미정착 카드가 처음부터 다시 뽑힌다)
            SetActiveCardsInteractable(false);

            var targets = ComputeLayoutTargets(_activeCards.Count);

            var phaseTasks = new List<UniTask>(toRemove.Count + _activeCards.Count);
            for (var i = 0; i < toRemove.Count; i++)
            {
                var (card, runtime) = toRemove[i];
                phaseTasks.Add(AnimateDiscardAsync(card, runtime, i * _discardStagger, token));
            }

            for (var i = 0; i < _activeCards.Count; i++)
            {
                var card = _activeCards[i];
                if (drawnSet.Contains(card))
                    continue;

                phaseTasks.Add(AnimateMoveAsync(
                    card,
                    targets[i],
                    Vector3.one,
                    _layoutDuration,
                    Ease.OutCubic,
                    0f,
                    token));
            }

            if (phaseTasks.Count > 0)
                await UniTask.WhenAll(phaseTasks);

            token.ThrowIfCancellationRequested();

            for (var d = 0; d < drawnCards.Count; d++)
            {
                var card = drawnCards[d];
                var index = _activeCards.IndexOf(card);
                if (index < 0)
                    continue;

                card.CachedTransform.localScale = Vector3.one * _travelStartScale;
                AudioManager.PlaySfx("Card_Flip");
                await AnimateMoveAsync(
                    card,
                    targets[index],
                    Vector3.one,
                    _drawDuration,
                    Ease.OutBack,
                    0f,
                    token);

                if (card.BoundCard != null)
                    _settledUids.Add(card.BoundCard.Uid);

                if (_drawStagger > 0f && d < drawnCards.Count - 1)
                    await UniTask.Delay(TimeSpan.FromSeconds(_drawStagger), cancellationToken: token);
            }

            for (var i = 0; i < _activeCards.Count; i++)
            {
                var card = _activeCards[i];
                if (card?.BoundCard != null)
                    _settledUids.Add(card.BoundCard.Uid);
            }

            SetActiveCardsInteractable(true);
        }

        private void SetActiveCardsInteractable(bool interactable)
        {
            for (var i = 0; i < _activeCards.Count; i++)
            {
                var card = _activeCards[i];
                if (card != null)
                    card.SetInteractable(interactable);
            }
        }

        private Card SpawnCard(RuntimeCard runtime)
        {
            var card = _pool.Get();
            if (card == null)
                return null;

            _leasedCards.Add(card);
            card.Bind(runtime);
            card.CachedTransform.SetParent(_container, false);
            return card;
        }

        private void ReleaseAllLeasedCards()
        {
            _settledUids.Clear();
            _activeCards.Clear();

            if (_leasedCards.Count == 0)
                return;

            var leased = new List<Card>(_leasedCards);
            _leasedCards.Clear();

            for (var i = 0; i < leased.Count; i++)
            {
                var card = leased[i];
                if (card == null || _pool == null)
                    continue;

                card.ResetTravelVisual();
                card.SetInteractable(true);
                _pool.Release(card);
            }
        }

        private async UniTask AnimateDiscardAsync(
            Card card,
            RuntimeCard runtime,
            float delaySeconds,
            CancellationToken token)
        {
            if (card == null)
                return;

            try
            {
                card.SetInteractable(false);
                card.CachedTransform.SetAsLastSibling();

                var anchor = ResolveRemovalAnchor(runtime);
                var target = GetAnchorLocalPoint(anchor);
                await AnimateMoveAsync(
                    card,
                    target,
                    Vector3.one * _travelStartScale,
                    _discardDuration,
                    Ease.InCubic,
                    delaySeconds,
                    token);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            finally
            {
                ReleaseCard(card);
            }
        }

        private async UniTask AnimateMoveAsync(
            Card card,
            Vector2 targetPos,
            Vector3 targetScale,
            float duration,
            Ease ease,
            float delaySeconds,
            CancellationToken token)
        {
            if (card == null)
                return;

            if (delaySeconds > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken: token);

            token.ThrowIfCancellationRequested();

            var rect = card.CachedRectTransform;
            var tr = card.CachedTransform;
            var fromPos = rect.anchoredPosition;
            var fromScale = tr.localScale;

            if (duration <= 0f)
            {
                rect.anchoredPosition = targetPos;
                tr.localScale = targetScale;
                return;
            }

            var posHandle = LMotion.Create(fromPos, targetPos, duration)
                .WithEase(ease)
                .Bind(v => rect.anchoredPosition = v);

            var scaleHandle = LMotion.Create(fromScale, targetScale, duration)
                .WithEase(ease)
                .BindToLocalScale(tr);

            try
            {
                await UniTask.WhenAll(
                    posHandle.ToUniTask(token),
                    scaleHandle.ToUniTask(token));

                if (card != null)
                {
                    card.CachedRectTransform.anchoredPosition = targetPos;
                    card.CachedTransform.localScale = targetScale;
                }
            }
            catch (OperationCanceledException)
            {
                if (posHandle.IsActive())
                    posHandle.Cancel();
                if (scaleHandle.IsActive())
                    scaleHandle.Cancel();
                throw;
            }
        }

        private RectTransform ResolveRemovalAnchor(RuntimeCard runtime)
        {
            if (runtime != null && ContainsCard(DeckSystem.DiscardPile, runtime))
                return _discardPileAnchor;

            return _drawPileAnchor != null ? _drawPileAnchor : _discardPileAnchor;
        }

        private static bool ContainsCard(IReadOnlyList<RuntimeCard> pile, RuntimeCard card)
        {
            if (pile == null || card == null)
                return false;

            for (var i = 0; i < pile.Count; i++)
            {
                if (pile[i] == card)
                    return true;
            }

            return false;
        }

        private Vector2[] ComputeLayoutTargets(int count)
        {
            var targets = new Vector2[count];
            if (count == 0)
                return targets;

            var width = _activeCards[0].CachedRectTransform.rect.width;
            var totalWidth = count * width + (count - 1) * _cardSpacing;

            for (var i = 0; i < count; i++)
            {
                var x = -totalWidth * 0.5f + width * 0.5f + i * (width + _cardSpacing);
                targets[i] = new Vector2(x, 0f);
            }

            return targets;
        }

        private Vector2 GetAnchorLocalPoint(RectTransform anchor)
        {
            if (anchor == null || _container == null)
                return Vector2.zero;

            // 버튼 전체가 아니라 실제 아이콘(Image) rect 중앙을 우선 사용한다.
            var visual = ResolveVisualRect(anchor);
            var worldCenter = visual.TransformPoint(visual.rect.center);

            // PlayerCardsContainer pivot은 하단(0.5,0), Card 앵커는 중앙(0.5,0.5)이다.
            // ScreenPointToLocalPoint 결과를 그대로 anchoredPosition에 넣으면
            // 컨테이너 높이 절반만큼 위로 치우친다. 앵커 기준 좌표로 변환한다.
            var localInContainer = (Vector2)_container.InverseTransformPoint(worldCenter);
            var rect = _container.rect;
            var anchorReference = new Vector2(
                Mathf.Lerp(rect.xMin, rect.xMax, 0.5f),
                Mathf.Lerp(rect.yMin, rect.yMax, 0.5f));

            return localInContainer - anchorReference;
        }

        private static RectTransform ResolveVisualRect(RectTransform anchor)
        {
            if (anchor == null)
                return null;

            var image = anchor.Find("ButtonImage") as RectTransform;
            return image != null ? image : anchor;
        }

        private void ReleaseCard(Card card)
        {
            if (card == null || _pool == null)
                return;

            // 인터럽트 시 ReleaseAllLeasedCards가 먼저 회수했을 수 있다.
            if (!_leasedCards.Remove(card))
                return;

            if (card.BoundCard != null)
                _settledUids.Remove(card.BoundCard.Uid);

            card.ResetTravelVisual();
            card.SetInteractable(true);
            _pool.Release(card);
        }

        private void OnDestroy()
        {
            if (GameStateUtil.IsQuitting)
                return;

            _refreshCts?.Cancel();
            _refreshCts?.Dispose();
            _refreshCts = null;

            _disposables?.Dispose();
            _disposables = null;

            ReleaseAllLeasedCards();
            ObjectPoolManager.ReleasePool<Card>();
        }
    }
}
