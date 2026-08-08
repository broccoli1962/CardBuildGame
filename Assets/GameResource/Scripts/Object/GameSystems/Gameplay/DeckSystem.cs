using System.Collections.Generic;
using Backend.Object.GameSystems.Llm;
using Backend.Object.Management;
using R3;
using UnityEngine;

namespace Backend.Object.GameSystems.Gameplay
{
    public static class DeckSystem
    {
        #region Fields

        private static readonly List<RuntimeCard> _masterDeck = new();
        private static readonly List<RuntimeCard> _drawPile = new();
        private static readonly List<RuntimeCard> _hand = new();
        private static readonly List<RuntimeCard> _discardPile = new();
        private static readonly Subject<Unit> _onHandChanged = new();
        private static readonly ReactiveProperty<int> _masterCount = new(0);
        private static bool _fullHandResetPending;

        #endregion

        #region Properties

        public static Observable<Unit> OnHandChanged => _onHandChanged;
        public static IReadOnlyList<RuntimeCard> Hand => _hand;
        public static IReadOnlyList<RuntimeCard> MasterDeck => _masterDeck;
        public static IReadOnlyList<RuntimeCard> DrawPile => _drawPile;
        public static IReadOnlyList<RuntimeCard> DiscardPile => _discardPile;
        public static ReadOnlyReactiveProperty<int> MasterCount => _masterCount;
        public static int DrawPileCount => _drawPile.Count;
        public static int DiscardPileCount => _discardPile.Count;

        #endregion

        #region Public Methods

        public static void Initialize()
        {
            if (!TableManager.IsInitialized)
            {
                Debug.LogError("[DeckSystem] TableManager is not initialized.");
                return;
            }

            _masterDeck.Clear();
            _drawPile.Clear();
            _hand.Clear();
            _discardPile.Clear();

            foreach (var data in TableManager.GetBaseCards())
            {
                if (data == null) continue;

                var count = Mathf.Max(0, data.count);
                for (var i = 0; i < count; i++)
                {
                    var card = RuntimeCard.CreateFromBaseCard(data);
                    if (card != null)
                        _masterDeck.Add(card);
                }
            }

            PublishMasterCount();
            PrepareForBattle();
            Debug.Log($"[DeckSystem] Master deck initialized with {_masterDeck.Count} cards.");
        }

        /// <summary>
        /// 노드 전투 시작 시 마스터 덱을 셔플해 드로우 파일을 재구성합니다.
        /// </summary>
        public static void PrepareForBattle()
        {
            _hand.Clear();
            _discardPile.Clear();
            _drawPile.Clear();
            _drawPile.AddRange(_masterDeck);
            Shuffle(_drawPile);

            // 마스터 덱 RuntimeCard(Uid)는 전투 간에 재사용된다.
            // CardController가 이전 손패를 kept로 오인하지 않도록 전체 재딜을 요청한다.
            _fullHandResetPending = true;
            DrawInitialHand();
        }

        /// <summary>
        /// 전투 경계에서 손패 뷰를 전부 다시 딜해야 하면 true를 반환하고 플래그를 소비합니다.
        /// </summary>
        public static bool ConsumeFullHandReset()
        {
            if (!_fullHandResetPending)
                return false;

            _fullHandResetPending = false;
            return true;
        }

        public static void DrawInitialHand()
        {
            // 손패를 버리면 카드가 소실되므로, 다시 뽑기 전에 드로우 파일로 되돌린다.
            if (_hand.Count > 0)
            {
                _drawPile.AddRange(_hand);
                _hand.Clear();
            }

            DrawToHandSize(force: true);
        }

        /// <summary>
        /// 손패를 HandSize까지 드로우합니다. 덱이 비면 버린 더미를 셔플해 보충합니다.
        /// </summary>
        public static void DrawToHandSize(bool force = false)
        {
            var handSize = TableManager.GetInt(TableManager.BalanceKey.HandSize, 5);
            if (!force && _hand.Count >= handSize)
                return;

            var changed = false;

            while (_hand.Count < handSize)
            {
                if (_drawPile.Count == 0)
                {
                    if (_discardPile.Count == 0)
                        break;

                    _drawPile.AddRange(_discardPile);
                    _discardPile.Clear();
                    Shuffle(_drawPile);
                }

                if (_drawPile.Count == 0)
                    break;

                var index = _drawPile.Count - 1;
                _hand.Add(_drawPile[index]);
                _drawPile.RemoveAt(index);
                changed = true;
            }

            if (_hand.Count < handSize && _drawPile.Count == 0 && _discardPile.Count == 0)
                Debug.LogWarning($"[DeckSystem] Draw pile empty before filling hand ({_hand.Count}/{handSize}). Master={_masterDeck.Count}");

            if (changed || force)
                _onHandChanged.OnNext(Unit.Default);
        }

        public static bool ContainsInHand(RuntimeCard card)
        {
            return card != null && _hand.Contains(card);
        }

        public static bool TryPlayCard(RuntimeCard card)
        {
            return BattleSystem.TryPlayCard(card);
        }

        public static void DiscardFromHand(RuntimeCard card)
        {
            if (card == null || !_hand.Remove(card))
                return;

            _discardPile.Add(card);
            _onHandChanged.OnNext(Unit.Default);
        }

        public static void DiscardHand()
        {
            if (_hand.Count == 0)
                return;

            _discardPile.AddRange(_hand);
            _hand.Clear();
            _onHandChanged.OnNext(Unit.Default);
        }

        /// <summary>
        /// 생성 카드를 마스터 덱에 추가합니다. deck_limit은 마스터 크기와 함께 증가합니다.
        /// </summary>
        public static bool TryAddGeneratedCard(GeneratedCardData data, out RuntimeCard runtimeCard)
        {
            runtimeCard = RuntimeCard.CreateFromGenerated(data);
            if (runtimeCard == null)
                return false;

            _masterDeck.Add(runtimeCard);
            PublishMasterCount();
            Debug.Log($"[DeckSystem] Added generated card '{runtimeCard.DisplayName}' (CPS {runtimeCard.PowerScore:0.#}). Master={_masterDeck.Count}");
            return true;
        }

        public static void Dispose()
        {
            _masterDeck.Clear();
            _drawPile.Clear();
            _hand.Clear();
            _discardPile.Clear();
            _fullHandResetPending = true;
            PublishMasterCount();
            _onHandChanged.OnNext(Unit.Default);
        }

        #endregion

        #region Private Methods

        private static void PublishMasterCount()
        {
            _masterCount.Value = _masterDeck.Count;
        }

        private static void Shuffle(List<RuntimeCard> pile)
        {
            for (var i = pile.Count - 1; i > 0; i--)
            {
                var j = Random.Range(0, i + 1);
                (pile[i], pile[j]) = (pile[j], pile[i]);
            }
        }

        #endregion
    }
}
