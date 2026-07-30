using System.Collections.Generic;
using UnityEngine;

namespace Backend.Object.Management
{
    public partial class TableManager
    {
        #region Card Index Fields

        private BaseCardData[] _baseCards = EmptyBaseCards;
        private readonly Dictionary<string, BaseCardData> _baseCardById = new();
        private readonly Dictionary<CardEffectType, CardEffectTypeData> _cardEffectTypeByType = new();
        private readonly Dictionary<CardEffectType, CardPowerWeightData> _cardPowerWeightByType = new();

        #endregion

        #region Card Index Build

        private void BuildCardIndex()
        {
            _baseCardById.Clear();
            _cardEffectTypeByType.Clear();
            _cardPowerWeightByType.Clear();

            if (_tableLinker?.BaseCardTable == null)
            {
                Debug.LogError("[TableManager] BaseCardTable이 없습니다.");
                _baseCards = EmptyBaseCards;
            }
            else
            {
                var list = new List<BaseCardData>(_tableLinker.BaseCardTable.dataList.Count);

                foreach (var card in _tableLinker.BaseCardTable.dataList)
                {
                    if (card == null) continue;

                    list.Add(card);

                    if (!string.IsNullOrEmpty(card.card_id))
                    {
                        _baseCardById[card.card_id] = card;
                    }
                }

                _baseCards = list.Count > 0 ? list.ToArray() : EmptyBaseCards;
            }

            if (_tableLinker?.CardEffectTypeTable == null)
            {
                Debug.LogError("[TableManager] CardEffectTypeTable이 없습니다.");
            }
            else
            {
                foreach (var row in _tableLinker.CardEffectTypeTable.dataList)
                {
                    if (row == null) continue;
                    _cardEffectTypeByType[row.effect_type] = row;
                }
            }

            if (_tableLinker?.CardPowerWeightTable == null)
            {
                Debug.LogError("[TableManager] CardPowerWeightTable이 없습니다.");
            }
            else
            {
                foreach (var row in _tableLinker.CardPowerWeightTable.dataList)
                {
                    if (row == null) continue;
                    _cardPowerWeightByType[row.effect_type] = row;
                }
            }
        }

        #endregion

        #region Card Accessors

        /// <summary>
        /// 모든 기본 카드 데이터 목록을 반환합니다.
        /// </summary>
        public static IReadOnlyList<BaseCardData> GetBaseCards()
        {
            if (!TryGetTable(out var manager)) return EmptyBaseCards;

            return manager._baseCards;
        }

        /// <summary>
        /// card_id로 기본 카드 데이터를 조회합니다.
        /// </summary>
        public static BaseCardData GetBaseCard(string cardId)
        {
            if (!TryGetTable(out var manager)) return null;
            if (string.IsNullOrEmpty(cardId)) return null;

            return manager._baseCardById.TryGetValue(cardId, out var card) ? card : null;
        }

        /// <summary>
        /// 카드 효과 타입 데이터를 조회합니다.
        /// </summary>
        public static CardEffectTypeData GetCardEffectType(CardEffectType effectType)
        {
            if (!TryGetTable(out var manager)) return null;

            return manager._cardEffectTypeByType.TryGetValue(effectType, out var row) ? row : null;
        }

        /// <summary>
        /// 해당 카드 효과 타입이 허용되는지 확인합니다.
        /// </summary>
        public static bool IsAllowedEffectType(CardEffectType effectType)
        {
            if (!TryGetTable(out var manager)) return false;

            return manager._cardEffectTypeByType.ContainsKey(effectType);
        }

        /// <summary>
        /// 카드 효과 값을 테이블 min/max 범위로 클램프합니다.
        /// </summary>
        public static int ClampEffectValue(CardEffectType effectType, int value)
        {
            if (!TryGetTable(out var manager)) return value;

            if (!manager._cardEffectTypeByType.TryGetValue(effectType, out var row))
            {
                Debug.LogWarning($"[TableManager] 알 수 없는 CardEffectType: {effectType}");
                return value;
            }

            if (value < row.min_value) return row.min_value;
            if (value > row.max_value) return row.max_value;
            return value;
        }

        /// <summary>
        /// 카드 효과 타입의 파워 가중치 데이터를 조회합니다.
        /// </summary>
        public static CardPowerWeightData GetCardPowerWeight(CardEffectType effectType)
        {
            if (!TryGetTable(out var manager)) return null;

            return manager._cardPowerWeightByType.TryGetValue(effectType, out var row) ? row : null;
        }

        /// <summary>
        /// 카드 효과의 파워 점수를 계산합니다.
        /// </summary>
        public static float GetEffectPowerScore(CardEffectType effectType, int value)
        {
            if (!TryGetTable(out var manager)) return 0f;

            if (!manager._cardPowerWeightByType.TryGetValue(effectType, out var weightRow))
            {
                Debug.LogWarning($"[TableManager] CardPowerWeight 누락: {effectType}");
                return 0f;
            }

            if (manager._cardEffectTypeByType.TryGetValue(effectType, out var effectRow) && effectRow.is_bool)
            {
                return weightRow.flat_bonus;
            }

            return value * weightRow.weight + weightRow.flat_bonus;
        }

        #endregion
    }
}
