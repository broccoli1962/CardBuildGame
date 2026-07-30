using System.Collections.Generic;
using UnityEngine;

namespace Backend.Object.Management
{
    public partial class TableManager
    {
        #region Balance Constants

        /// <summary>
        /// BalanceConstant 시트 키 상수 모음.
        /// </summary>
        public static class BalanceKey
        {
            public const string ManaCostMultiplier = "mana_cost_multiplier";
            public const string UsabilityFactorNormal = "usability_factor_normal";
            public const string UsabilityFactorManaOver = "usability_factor_mana_over";
            public const string PlayerStartHp = "player_start_hp";
            public const string PlayerStartMaxHp = "player_start_max_hp";
            public const string PlayerStartMaxMana = "player_start_max_mana";
            public const string HandSize = "hand_size";
            public const string BaseDeckLimit = "base_deck_limit";
            public const string DeckMinSize = "deck_min_size";
            public const string CardNameMaxLength = "card_name_max_length";
            public const string CardDescMaxLength = "card_desc_max_length";
            public const string CardManaCostMax = "card_mana_cost_max";
            public const string HpMultCap = "hp_mult_cap";
            public const string AtkMultCap = "atk_mult_cap";
            public const string BossAtkMultCap = "boss_atk_mult_cap";
            public const string EnemyTurnDelay = "enemy_turn_delay";
            public const string LlmTimeout = "llm_timeout";
            public const string LlmTemperature = "llm_temperature";
            public const string MapGenMaxRetry = "map_gen_max_retry";
            public const string MapFloorCount = "map_floor_count";
            public const string EliteBonusMaxHp = "elite_bonus_max_hp";
            public const string BlankCardThreatRatio = "blank_card_threat_ratio";
        }

        #endregion

        #region Balance Index Fields

        private readonly Dictionary<string, float> _balanceConstants = new();

        #endregion

        #region Balance Index Build

        private void BuildBalanceIndex()
        {
            _balanceConstants.Clear();

            if (_tableLinker?.BalanceConstantTable == null)
            {
                Debug.LogError("[TableManager] BalanceConstantTable이 없습니다.");
                return;
            }

            foreach (var row in _tableLinker.BalanceConstantTable.dataList)
            {
                if (row == null || string.IsNullOrEmpty(row.key)) continue;

                if (_balanceConstants.ContainsKey(row.key))
                {
                    Debug.LogWarning($"[TableManager] BalanceConstant 중복 키: {row.key}");
                    continue;
                }

                _balanceConstants[row.key] = row.value;
            }
        }

        #endregion

        #region Balance Accessors

        /// <summary>
        /// 밸런스 상수를 float로 조회합니다.
        /// </summary>
        public static float GetFloat(string key, float defaultValue = 0f)
        {
            if (!TryGetTable(out var manager)) return defaultValue;
            if (string.IsNullOrEmpty(key)) return defaultValue;

            return manager._balanceConstants.TryGetValue(key, out var value) ? value : defaultValue;
        }

        /// <summary>
        /// 밸런스 상수를 int로 조회합니다 (반올림).
        /// </summary>
        public static int GetInt(string key, int defaultValue = 0)
        {
            if (!TryGetTable(out var manager)) return defaultValue;
            if (string.IsNullOrEmpty(key)) return defaultValue;

            return manager._balanceConstants.TryGetValue(key, out var value) ? Mathf.RoundToInt(value) : defaultValue;
        }

        /// <summary>
        /// 밸런스 상수 키 존재 여부를 확인합니다.
        /// </summary>
        public static bool HasConstant(string key)
        {
            if (!TryGetTable(out var manager)) return false;
            if (string.IsNullOrEmpty(key)) return false;

            return manager._balanceConstants.ContainsKey(key);
        }

        #endregion
    }
}
