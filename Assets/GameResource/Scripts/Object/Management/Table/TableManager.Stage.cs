using System.Collections.Generic;
using UnityEngine;

namespace Backend.Object.Management
{
    public partial class TableManager
    {
        #region Stage Index Fields

        private readonly Dictionary<string, StageData> _stageById = new();
        private readonly Dictionary<(int chapter, int floor), StageData> _stageByChapterFloor = new();
        private readonly Dictionary<(int chapter, int floor), string[]> _monsterPoolByChapterFloor = new();
        private readonly Dictionary<int, StageData[]> _stagesByChapter = new();

        #endregion

        #region Stage Index Build

        private void BuildStageIndex()
        {
            _stageById.Clear();
            _stageByChapterFloor.Clear();
            _monsterPoolByChapterFloor.Clear();
            _stagesByChapter.Clear();

            if (_tableLinker?.StageTable == null)
            {
                Debug.LogError("[TableManager] StageTable이 없습니다.");
                return;
            }

            var chapterStages = new Dictionary<int, List<StageData>>();

            foreach (var stage in _tableLinker.StageTable.dataList)
            {
                if (stage == null) continue;

                if (!string.IsNullOrEmpty(stage.stage_id))
                {
                    _stageById[stage.stage_id] = stage;
                }

                var chapterFloorKey = (stage.chapter, stage.floor);
                _stageByChapterFloor[chapterFloorKey] = stage;
                _monsterPoolByChapterFloor[chapterFloorKey] = ParseCommaSeparatedPool(stage.monster_pool);

                if (!chapterStages.TryGetValue(stage.chapter, out var list))
                {
                    list = new List<StageData>();
                    chapterStages[stage.chapter] = list;
                }

                list.Add(stage);
            }

            foreach (var pair in chapterStages)
            {
                _stagesByChapter[pair.Key] = pair.Value.ToArray();
            }
        }

        private static string[] ParseCommaSeparatedPool(string pool)
        {
            if (string.IsNullOrWhiteSpace(pool))
            {
                return EmptyStrings;
            }

            var parts = pool.Split(',');
            var result = new List<string>(parts.Length);

            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                {
                    result.Add(trimmed);
                }
            }

            return result.Count > 0 ? result.ToArray() : EmptyStrings;
        }

        #endregion

        #region Stage Accessors

        /// <summary>
        /// stage_id로 스테이지 데이터를 조회합니다.
        /// </summary>
        public static StageData GetStage(string stageId)
        {
            if (!TryGetTable(out var manager)) return null;
            if (string.IsNullOrEmpty(stageId)) return null;

            return manager._stageById.TryGetValue(stageId, out var stage) ? stage : null;
        }

        /// <summary>
        /// 챕터와 층으로 스테이지 데이터를 조회합니다.
        /// </summary>
        public static StageData GetStage(int chapter, int floor)
        {
            if (!TryGetTable(out var manager)) return null;

            return manager._stageByChapterFloor.TryGetValue((chapter, floor), out var stage) ? stage : null;
        }

        /// <summary>
        /// 챕터에 속한 모든 스테이지 목록을 반환합니다.
        /// </summary>
        public static IReadOnlyList<StageData> GetStages(int chapter)
        {
            if (!TryGetTable(out var manager)) return EmptyStages;

            return manager._stagesByChapter.TryGetValue(chapter, out var stages) ? stages : EmptyStages;
        }

        /// <summary>
        /// 챕터·층에 해당하는 몬스터 풀 ID 목록을 반환합니다.
        /// </summary>
        public static IReadOnlyList<string> GetMonsterPool(int chapter, int floor)
        {
            if (!TryGetTable(out var manager)) return EmptyStrings;

            return manager._monsterPoolByChapterFloor.TryGetValue((chapter, floor), out var pool) ? pool : EmptyStrings;
        }

        #endregion
    }
}
