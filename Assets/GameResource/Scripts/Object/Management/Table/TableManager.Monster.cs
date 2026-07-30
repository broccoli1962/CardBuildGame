using System.Collections.Generic;
using UnityEngine;

namespace Backend.Object.Management
{
    public partial class TableManager
    {
        #region Monster Index Types

        private struct ParsedMonsterAction
        {
            public MonsterActionData Data;
            public bool IsWildcard;
            public int[] Slots;
        }

        private struct MonsterTierCache
        {
            public MonsterActionData[] AvailableActions;
            public ParsedMonsterAction[] AvailableParsed;
            public int CycleLength;
        }

        #endregion

        #region Monster Index Fields

        private readonly Dictionary<string, MonsterData> _monsterById = new();
        private readonly Dictionary<string, List<ParsedMonsterAction>> _parsedActionsByMonsterId = new();
        private readonly Dictionary<(string monsterId, int tier), MonsterTierCache> _monsterTierCache = new();

        #endregion

        #region Monster Index Build

        private void BuildMonsterIndex()
        {
            _monsterById.Clear();
            _parsedActionsByMonsterId.Clear();
            _monsterTierCache.Clear();

            if (_tableLinker?.MonsterTable == null)
            {
                Debug.LogError("[TableManager] MonsterTable이 없습니다.");
            }
            else
            {
                foreach (var monster in _tableLinker.MonsterTable.dataList)
                {
                    if (monster == null || string.IsNullOrEmpty(monster.monster_id)) continue;
                    _monsterById[monster.monster_id] = monster;
                }
            }

            if (_tableLinker?.MonsterActionTable == null)
            {
                Debug.LogError("[TableManager] MonsterActionTable이 없습니다.");
                return;
            }

            foreach (var action in _tableLinker.MonsterActionTable.dataList)
            {
                if (action == null || string.IsNullOrEmpty(action.monster_id)) continue;

                if (!_parsedActionsByMonsterId.TryGetValue(action.monster_id, out var list))
                {
                    list = new List<ParsedMonsterAction>();
                    _parsedActionsByMonsterId[action.monster_id] = list;
                }

                list.Add(new ParsedMonsterAction
                {
                    Data = action,
                    IsWildcard = ParseTurnCycle(action.turn_cycle, out var slots),
                    Slots = slots,
                });
            }
        }

        private static bool ParseTurnCycle(string turnCycle, out int[] slots)
        {
            slots = EmptyInts;

            if (string.IsNullOrWhiteSpace(turnCycle))
            {
                return false;
            }

            var trimmed = turnCycle.Trim();
            if (trimmed == "*")
            {
                return true;
            }

            var parts = trimmed.Split(',');
            var slotList = new List<int>(parts.Length);

            foreach (var part in parts)
            {
                var token = part.Trim();
                if (string.IsNullOrEmpty(token)) continue;

                if (int.TryParse(token, out var slot))
                {
                    slotList.Add(slot);
                }
            }

            slots = slotList.Count > 0 ? slotList.ToArray() : EmptyInts;
            return false;
        }

        #endregion

        #region Monster Tier Cache

        private MonsterTierCache GetOrCreateTierCache(string monsterId, int tier)
        {
            var key = (monsterId, tier);
            if (_monsterTierCache.TryGetValue(key, out var cache))
            {
                return cache;
            }

            if (!_parsedActionsByMonsterId.TryGetValue(monsterId, out var allActions))
            {
                cache = new MonsterTierCache
                {
                    AvailableActions = EmptyMonsterActions,
                    AvailableParsed = System.Array.Empty<ParsedMonsterAction>(),
                    CycleLength = 1,
                };
                _monsterTierCache[key] = cache;
                return cache;
            }

            var available = new List<MonsterActionData>();
            var availableParsed = new List<ParsedMonsterAction>();

            foreach (var parsed in allActions)
            {
                if (parsed.Data.tier_min <= tier)
                {
                    available.Add(parsed.Data);
                    availableParsed.Add(parsed);
                }
            }

            cache = new MonsterTierCache
            {
                AvailableActions = available.Count > 0 ? available.ToArray() : EmptyMonsterActions,
                AvailableParsed = availableParsed.Count > 0 ? availableParsed.ToArray() : System.Array.Empty<ParsedMonsterAction>(),
                CycleLength = ComputeCycleLength(availableParsed),
            };

            _monsterTierCache[key] = cache;
            return cache;
        }

        private static int ComputeCycleLength(IReadOnlyList<ParsedMonsterAction> availableActions)
        {
            var maxSlot = 0;

            for (var i = 0; i < availableActions.Count; i++)
            {
                var action = availableActions[i];
                if (action.IsWildcard) continue;

                for (var j = 0; j < action.Slots.Length; j++)
                {
                    if (action.Slots[j] > maxSlot)
                    {
                        maxSlot = action.Slots[j];
                    }
                }
            }

            return maxSlot > 0 ? maxSlot : 1;
        }

        private static bool ActionMatchesSlot(ParsedMonsterAction action, int slot)
        {
            if (action.IsWildcard) return true;

            for (var i = 0; i < action.Slots.Length; i++)
            {
                if (action.Slots[i] == slot)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Monster Accessors

        /// <summary>
        /// monster_id로 몬스터 데이터를 조회합니다.
        /// </summary>
        public static MonsterData GetMonster(string monsterId)
        {
            if (!TryGetTable(out var manager)) return null;
            if (string.IsNullOrEmpty(monsterId)) return null;

            return manager._monsterById.TryGetValue(monsterId, out var monster) ? monster : null;
        }

        /// <summary>
        /// 해당 위협 티어에서 사용 가능한 몬스터 액션 목록을 반환합니다.
        /// </summary>
        public static IReadOnlyList<MonsterActionData> GetMonsterActions(string monsterId, int threatTier = 0)
        {
            if (!TryGetTable(out var manager)) return EmptyMonsterActions;
            if (string.IsNullOrEmpty(monsterId)) return EmptyMonsterActions;

            return manager.GetOrCreateTierCache(monsterId, threatTier).AvailableActions;
        }

        /// <summary>
        /// turnIndex에 해당하는 다음 몬스터 액션을 반환합니다.
        /// </summary>
        public static MonsterActionData GetNextMonsterAction(string monsterId, int turnIndex, int threatTier = 0)
        {
            if (!TryGetTable(out var manager)) return null;
            if (string.IsNullOrEmpty(monsterId)) return null;

            var cache = manager.GetOrCreateTierCache(monsterId, threatTier);
            if (cache.AvailableParsed.Length == 0)
            {
                return null;
            }

            if (turnIndex < 1)
            {
                turnIndex = 1;
            }

            var cycleLength = cache.CycleLength;
            var slot = ((turnIndex - 1) % cycleLength) + 1;

            MonsterActionData wildcardMatch = null;

            for (var i = 0; i < cache.AvailableParsed.Length; i++)
            {
                var parsed = cache.AvailableParsed[i];
                if (!ActionMatchesSlot(parsed, slot)) continue;

                if (!parsed.IsWildcard)
                {
                    return parsed.Data;
                }

                if (wildcardMatch == null)
                {
                    wildcardMatch = parsed.Data;
                }
            }

            return wildcardMatch;
        }

        /// <summary>
        /// 해당 위협 티어에서 몬스터 액션 사이클 길이를 반환합니다.
        /// </summary>
        public static int GetMonsterActionCycleLength(string monsterId, int threatTier = 0)
        {
            if (!TryGetTable(out var manager)) return 1;
            if (string.IsNullOrEmpty(monsterId)) return 1;

            return manager.GetOrCreateTierCache(monsterId, threatTier).CycleLength;
        }

        #endregion
    }
}
