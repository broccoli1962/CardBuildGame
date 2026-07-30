#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Backend.Object.Management
{
    public partial class TableManager
    {
        private readonly HashSet<string> _validatedLocalizeKeys = new();

        private void ValidateTables()
        {
            var errorCount = 0;
            var warningCount = 0;

            try
            {
                errorCount += ValidateTablesInternal(ref warningCount);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[TableManager] 테이블 검증 중 예외 발생: {ex.Message}");
                errorCount++;
            }

            var summary = $"[TableManager] 테이블 검증 완료 — 오류 {errorCount}건, 경고 {warningCount}건";
            if (errorCount > 0)
            {
                Debug.LogError(summary);
            }
            else if (warningCount > 0)
            {
                Debug.LogWarning(summary);
            }
            else
            {
                Debug.Log(summary);
            }
        }

        private int ValidateTablesInternal(ref int warningCount)
        {
            var errorCount = 0;
            _validatedLocalizeKeys.Clear();

            if (_tableLinker?.StageTable != null)
            {
                foreach (var stage in _tableLinker.StageTable.dataList)
                {
                    if (stage == null) continue;

                    var pool = ParseCommaSeparatedPool(stage.monster_pool);
                    for (var i = 0; i < pool.Length; i++)
                    {
                        if (!_monsterById.ContainsKey(pool[i]))
                        {
                            Debug.LogError($"[TableManager] Stage '{stage.stage_id}' monster_pool 항목 '{pool[i]}'에 해당하는 MonsterData가 없습니다.");
                            errorCount++;
                        }
                    }
                }
            }

            if (_tableLinker?.MonsterActionTable != null)
            {
                foreach (var action in _tableLinker.MonsterActionTable.dataList)
                {
                    if (action == null) continue;

                    if (!_monsterById.ContainsKey(action.monster_id))
                    {
                        Debug.LogError($"[TableManager] MonsterAction '{action.action_id}'의 monster_id '{action.monster_id}'에 해당하는 MonsterData가 없습니다.");
                        errorCount++;
                    }
                }
            }

            var monstersWithTierZeroAction = new HashSet<string>();
            if (_tableLinker?.MonsterActionTable != null)
            {
                foreach (var action in _tableLinker.MonsterActionTable.dataList)
                {
                    if (action == null) continue;
                    if (action.tier_min == 0)
                    {
                        monstersWithTierZeroAction.Add(action.monster_id);
                    }
                }
            }

            if (_tableLinker?.StageTable != null)
            {
                var stageMonsters = new HashSet<string>();

                foreach (var stage in _tableLinker.StageTable.dataList)
                {
                    if (stage == null) continue;

                    var pool = ParseCommaSeparatedPool(stage.monster_pool);
                    for (var i = 0; i < pool.Length; i++)
                    {
                        stageMonsters.Add(pool[i]);
                    }
                }

                foreach (var monsterId in stageMonsters)
                {
                    if (!monstersWithTierZeroAction.Contains(monsterId))
                    {
                        Debug.LogError($"[TableManager] 스테이지 참조 몬스터 '{monsterId}'에 tier_min == 0 인 액션이 없습니다.");
                        errorCount++;
                    }
                }
            }

            if (_tableLinker?.MapTemplateTable != null)
            {
                var seenKeys = new HashSet<(int chapter, int floor)>();
                var chapterFloors = new Dictionary<int, SortedSet<int>>();

                foreach (var template in _tableLinker.MapTemplateTable.dataList)
                {
                    if (template == null) continue;

                    var key = (template.chapter, template.floor);
                    if (!seenKeys.Add(key))
                    {
                        Debug.LogError($"[TableManager] MapTemplate ({template.chapter}, {template.floor}) 중복입니다.");
                        errorCount++;
                    }

                    if (!chapterFloors.TryGetValue(template.chapter, out var floors))
                    {
                        floors = new SortedSet<int>();
                        chapterFloors[template.chapter] = floors;
                    }

                    floors.Add(template.floor);
                }

                foreach (var pair in chapterFloors)
                {
                    var expected = 1;
                    foreach (var floor in pair.Value)
                    {
                        if (floor != expected)
                        {
                            Debug.LogError($"[TableManager] MapTemplate chapter {pair.Key}의 floor가 1부터 연속적이지 않습니다 (누락: {expected}).");
                            errorCount++;
                            break;
                        }

                        expected++;
                    }
                }
            }

            foreach (MapNodeType nodeType in Enum.GetValues(typeof(MapNodeType)))
            {
                if (!_mapNodeTypeByType.ContainsKey(nodeType))
                {
                    Debug.LogError($"[TableManager] MapNodeType '{nodeType}'에 해당하는 MapNodeTypeData가 없습니다.");
                    errorCount++;
                }
            }

            foreach (CardEffectType effectType in Enum.GetValues(typeof(CardEffectType)))
            {
                if (!_cardEffectTypeByType.ContainsKey(effectType))
                {
                    Debug.LogError($"[TableManager] CardEffectType '{effectType}'에 해당하는 CardEffectTypeData가 없습니다.");
                    errorCount++;
                }

                if (!_cardPowerWeightByType.ContainsKey(effectType))
                {
                    Debug.LogError($"[TableManager] CardEffectType '{effectType}'에 해당하는 CardPowerWeightData가 없습니다.");
                    errorCount++;
                }
            }

            if (_tableLinker?.ThreatScalingTable != null)
            {
                var hasZeroRow = false;
                var seenTiers = new HashSet<int>();
                var tierThreatMins = new List<(int tier, float threatMin)>();

                foreach (var row in _tableLinker.ThreatScalingTable.dataList)
                {
                    if (row == null) continue;

                    if (Mathf.Approximately(row.threat_min, 0f))
                    {
                        hasZeroRow = true;
                    }

                    if (!seenTiers.Add(row.tier))
                    {
                        Debug.LogError($"[TableManager] ThreatScaling tier {row.tier} 중복입니다.");
                        errorCount++;
                    }

                    tierThreatMins.Add((row.tier, row.threat_min));
                }

                if (!hasZeroRow)
                {
                    Debug.LogError("[TableManager] ThreatScalingData에 threat_min == 0 행이 없습니다.");
                    errorCount++;
                }

                tierThreatMins.Sort((a, b) => a.tier.CompareTo(b.tier));
                for (var i = 1; i < tierThreatMins.Count; i++)
                {
                    if (tierThreatMins[i].threatMin <= tierThreatMins[i - 1].threatMin)
                    {
                        Debug.LogError($"[TableManager] ThreatScaling tier {tierThreatMins[i].tier}의 threat_min이 이전 tier보다 크지 않습니다.");
                        errorCount++;
                    }
                }
            }

            if (_tableLinker?.BaseCardTable != null)
            {
                foreach (var card in _tableLinker.BaseCardTable.dataList)
                {
                    if (card == null) continue;

                    if (!Enum.IsDefined(typeof(CardType), card.card_type))
                    {
                        Debug.LogError($"[TableManager] BaseCard '{card.card_id}'의 card_type '{card.card_type}'가 정의되지 않았습니다.");
                        errorCount++;
                    }

                    if (card.count < 1)
                    {
                        Debug.LogError($"[TableManager] BaseCard '{card.card_id}'의 count가 1 미만입니다.");
                        errorCount++;
                    }
                }
            }

            errorCount += ValidateLocalizationKeys(ref warningCount);

            errorCount += ValidateBalanceKeys();

            return errorCount;
        }

        private int ValidateLocalizationKeys(ref int warningCount)
        {
            var errorCount = 0;

            if (_tableLinker?.MonsterTable != null)
            {
                foreach (var row in _tableLinker.MonsterTable.dataList)
                {
                    if (row == null) continue;
                    errorCount += CheckLocalizeKey(row.name_key);
                }
            }

            if (_tableLinker?.MonsterActionTable != null)
            {
                foreach (var row in _tableLinker.MonsterActionTable.dataList)
                {
                    if (row == null) continue;
                    errorCount += CheckLocalizeKey(row.display_key);
                }
            }

            if (_tableLinker?.BaseCardTable != null)
            {
                foreach (var row in _tableLinker.BaseCardTable.dataList)
                {
                    if (row == null) continue;
                    errorCount += CheckLocalizeKey(row.name_key);
                    errorCount += CheckLocalizeKey(row.desc_key);
                }
            }

            if (_tableLinker?.MapNodeTypeTable != null)
            {
                foreach (var row in _tableLinker.MapNodeTypeTable.dataList)
                {
                    if (row == null) continue;
                    errorCount += CheckLocalizeKey(row.name_key);
                    errorCount += CheckLocalizeKey(row.desc_key);
                }
            }

            if (_tableLinker?.ThreatScalingTable != null)
            {
                foreach (var row in _tableLinker.ThreatScalingTable.dataList)
                {
                    if (row == null) continue;
                    errorCount += CheckLocalizeKey(row.desc_key);
                }
            }

            if (_tableLinker?.RestOptionTable != null)
            {
                foreach (var row in _tableLinker.RestOptionTable.dataList)
                {
                    if (row == null) continue;
                    errorCount += CheckLocalizeKey(row.name_key);
                    errorCount += CheckLocalizeKey(row.desc_key);
                }
            }

            if (_tableLinker?.TreasureOptionTable != null)
            {
                foreach (var row in _tableLinker.TreasureOptionTable.dataList)
                {
                    if (row == null) continue;
                    errorCount += CheckLocalizeKey(row.name_key);
                    errorCount += CheckLocalizeKey(row.desc_key);
                }
            }

            if (_tableLinker?.MapEventTable != null)
            {
                foreach (var row in _tableLinker.MapEventTable.dataList)
                {
                    if (row == null) continue;
                    errorCount += CheckLocalizeKey(row.name_key);
                    errorCount += CheckLocalizeKey(row.desc_key);
                    errorCount += CheckLocalizeKey(row.option_a_key);
                    errorCount += CheckLocalizeKey(row.option_b_key);
                }
            }

            if (_tableLinker?.CardEffectTypeTable != null)
            {
                foreach (var row in _tableLinker.CardEffectTypeTable.dataList)
                {
                    if (row == null) continue;
                    errorCount += CheckLocalizeKey(row.description_key);
                }
            }

            return errorCount;
        }

        private int CheckLocalizeKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return 0;
            if (!_validatedLocalizeKeys.Add(key)) return 0;

            try
            {
                var text = key.GetLocalizeText(0, 0, 0);
                if (text == "!" + key)
                {
                    Debug.LogError($"[TableManager] 로컬라이즈 키 누락: {key}");
                    return 1;
                }
            }
            catch (System.FormatException)
            {
                return 0;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[TableManager] 로컬라이즈 키 검증 실패 ({key}): {ex.Message}");
                return 1;
            }

            return 0;
        }

        private int ValidateBalanceKeys()
        {
            var errorCount = 0;

            var keys = new[]
            {
                BalanceKey.ManaCostMultiplier,
                BalanceKey.UsabilityFactorNormal,
                BalanceKey.UsabilityFactorManaOver,
                BalanceKey.PlayerStartHp,
                BalanceKey.PlayerStartMaxHp,
                BalanceKey.PlayerStartMaxMana,
                BalanceKey.HandSize,
                BalanceKey.BaseDeckLimit,
                BalanceKey.DeckMinSize,
                BalanceKey.CardNameMaxLength,
                BalanceKey.CardDescMaxLength,
                BalanceKey.CardManaCostMax,
                BalanceKey.HpMultCap,
                BalanceKey.AtkMultCap,
                BalanceKey.BossAtkMultCap,
                BalanceKey.EnemyTurnDelay,
                BalanceKey.LlmTimeout,
                BalanceKey.LlmTemperature,
                BalanceKey.MapGenMaxRetry,
                BalanceKey.MapFloorCount,
                BalanceKey.EliteBonusMaxHp,
                BalanceKey.BlankCardThreatRatio,
            };

            for (var i = 0; i < keys.Length; i++)
            {
                if (!_balanceConstants.ContainsKey(keys[i]))
                {
                    Debug.LogError($"[TableManager] BalanceConstant 누락 키: {keys[i]}");
                    errorCount++;
                }
            }

            return errorCount;
        }
    }
}
#endif
