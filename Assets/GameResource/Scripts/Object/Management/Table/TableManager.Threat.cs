using System.Collections.Generic;
using UnityEngine;

namespace Backend.Object.Management
{
    public partial class TableManager
    {
        #region Threat Index Fields

        private ThreatScalingData[] _threatScalingSorted = System.Array.Empty<ThreatScalingData>();
        private readonly Dictionary<int, ThreatScalingData> _threatScalingByTier = new();
        private readonly Dictionary<int, EliteScalingData> _eliteScalingByChapter = new();

        #endregion

        #region Threat Index Build

        private void BuildThreatIndex()
        {
            _threatScalingByTier.Clear();
            _eliteScalingByChapter.Clear();

            if (_tableLinker?.ThreatScalingTable == null)
            {
                Debug.LogError("[TableManager] ThreatScalingTable이 없습니다.");
                _threatScalingSorted = System.Array.Empty<ThreatScalingData>();
            }
            else
            {
                var list = new List<ThreatScalingData>();
                foreach (var row in _tableLinker.ThreatScalingTable.dataList)
                {
                    if (row == null) continue;
                    list.Add(row);
                    _threatScalingByTier[row.tier] = row;
                }

                list.Sort((a, b) => b.threat_min.CompareTo(a.threat_min));
                _threatScalingSorted = list.ToArray();
            }

            if (_tableLinker?.EliteScalingTable == null)
            {
                Debug.LogError("[TableManager] EliteScalingTable이 없습니다.");
            }
            else
            {
                foreach (var row in _tableLinker.EliteScalingTable.dataList)
                {
                    if (row == null) continue;
                    _eliteScalingByChapter[row.chapter] = row;
                }
            }
        }

        #endregion

        #region Threat Accessors

        /// <summary>
        /// 위협 점수에 해당하는 ThreatScaling 데이터를 반환합니다.
        /// </summary>
        public static ThreatScalingData GetThreatScaling(float threatScore)
        {
            if (!TryGetTable(out var manager)) return null;

            var sorted = manager._threatScalingSorted;
            if (sorted.Length == 0)
            {
                return null;
            }

            for (var i = 0; i < sorted.Length; i++)
            {
                var row = sorted[i];
                if (row.threat_min <= threatScore)
                {
                    return row;
                }
            }

            return sorted[sorted.Length - 1];
        }

        /// <summary>
        /// 티어로 ThreatScaling 데이터를 조회합니다.
        /// </summary>
        public static ThreatScalingData GetThreatScalingByTier(int tier)
        {
            if (!TryGetTable(out var manager)) return null;

            return manager._threatScalingByTier.TryGetValue(tier, out var row) ? row : null;
        }

        /// <summary>
        /// 위협 점수에 해당하는 위협 티어를 반환합니다.
        /// </summary>
        public static int GetThreatTier(float threatScore)
        {
            if (!TryGetTable(out var manager)) return 0;

            var scaling = GetThreatScaling(threatScore);
            return scaling != null ? scaling.tier : 0;
        }

        /// <summary>
        /// 챕터에 해당하는 EliteScaling 데이터를 조회합니다.
        /// </summary>
        public static EliteScalingData GetEliteScaling(int chapter)
        {
            if (!TryGetTable(out var manager)) return null;

            return manager._eliteScalingByChapter.TryGetValue(chapter, out var row) ? row : null;
        }

        #endregion
    }
}
