using System.Collections.Generic;
using Backend.AddressableKey;
using Backend.Object.GameSystems.Gameplay;
using Backend.Object.Management;
using Backend.Object.Management.Pool;
using Backend.Object.UI;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace Backend.Object.Controller
{
    public class CardController : MonoBehaviour
    {
        [SerializeField] private float _cardSpacing = 20f;

        private RectTransform _container;
        private Pooling<Card> _pool;
        private readonly List<Card> _activeCards = new();
        private CompositeDisposable _disposables;

        /// <summary>
        /// 카드 풀을 생성하고 손패 변경 구독을 시작합니다.
        /// </summary>
        public async UniTask InitializeAsync(RectTransform container)
        {
            if (container == null)
            {
                Debug.LogError("[CardController] container is null.");
                return;
            }

            _container = container;

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
                .Subscribe(_ => RefreshHand())
                .AddTo(_disposables);

            RefreshHand();
        }

        private void RefreshHand()
        {
            if (_pool == null)
                return;

            foreach (var card in _activeCards)
            {
                if (card != null)
                    _pool.Release(card);
            }

            _activeCards.Clear();

            foreach (var runtimeCard in DeckSystem.Hand)
            {
                var card = _pool.Get();
                if (card == null)
                    continue;

                card.Bind(runtimeCard);
                card.CachedTransform.SetParent(_container, false);
                _activeCards.Add(card);
            }

            ApplyLayout();
        }

        private void ApplyLayout()
        {
            var count = _activeCards.Count;
            if (count == 0)
                return;

            var width = _activeCards[0].CachedRectTransform.rect.width;
            var totalWidth = count * width + (count - 1) * _cardSpacing;

            for (var i = 0; i < count; i++)
            {
                var x = -totalWidth * 0.5f + width * 0.5f + i * (width + _cardSpacing);
                _activeCards[i].CachedRectTransform.anchoredPosition = new Vector2(x, 0f);
            }
        }

        private void OnDestroy()
        {
            if (GameStateUtil.IsQuitting)
                return;

            _disposables?.Dispose();
            _disposables = null;

            if (_pool != null)
            {
                foreach (var card in _activeCards)
                {
                    if (card != null)
                        _pool.Release(card);
                }
            }

            _activeCards.Clear();
            ObjectPoolManager.ReleasePool<Card>();
        }
    }
}
