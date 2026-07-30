using System.Collections.Generic;
using UnityEngine;

namespace Backend.Object.Management
{
    public partial class TableManager
    {
        #region Map Index Types

        private struct MapTemplateCache
        {
            public MapNodeType[] AllowedTypes;
            public int[] TypeWeights;
            public bool HasForcedType;
            public MapNodeType ForcedType;
        }

        #endregion

        #region Map Index Fields

        private readonly Dictionary<(int chapter, int floor), MapTemplateData> _mapTemplateByChapterFloor = new();
        private readonly Dictionary<(int chapter, int floor), MapTemplateCache> _mapTemplateCacheByChapterFloor = new();
        private readonly Dictionary<int, int> _mapFloorCountByChapter = new();
        private readonly Dictionary<MapNodeType, MapNodeTypeData> _mapNodeTypeByType = new();
        private MapEventData[] _mapEvents = EmptyMapEvents;
        private readonly Dictionary<string, MapEventData> _mapEventById = new();
        private readonly Dictionary<int, MapEventData[]> _mapEventsByFloor = new();
        private RestOptionData[] _restOptions = EmptyRestOptions;
        private TreasureOptionData[] _treasureOptions = EmptyTreasureOptions;

        #endregion

        #region Map Index Build

        private void BuildMapIndex()
        {
            _mapTemplateByChapterFloor.Clear();
            _mapTemplateCacheByChapterFloor.Clear();
            _mapFloorCountByChapter.Clear();
            _mapNodeTypeByType.Clear();
            _mapEventById.Clear();
            _mapEventsByFloor.Clear();

            if (_tableLinker?.MapTemplateTable == null)
            {
                Debug.LogError("[TableManager] MapTemplateTable이 없습니다.");
            }
            else
            {
                var chapterMaxFloor = new Dictionary<int, int>();

                foreach (var template in _tableLinker.MapTemplateTable.dataList)
                {
                    if (template == null) continue;

                    var key = (template.chapter, template.floor);
                    _mapTemplateByChapterFloor[key] = template;
                    _mapTemplateCacheByChapterFloor[key] = ParseMapTemplate(template);

                    if (!chapterMaxFloor.TryGetValue(template.chapter, out var maxFloor) || template.floor > maxFloor)
                    {
                        chapterMaxFloor[template.chapter] = template.floor;
                    }
                }

                foreach (var pair in chapterMaxFloor)
                {
                    _mapFloorCountByChapter[pair.Key] = pair.Value;
                }
            }

            if (_tableLinker?.MapNodeTypeTable == null)
            {
                Debug.LogError("[TableManager] MapNodeTypeTable이 없습니다.");
            }
            else
            {
                foreach (var row in _tableLinker.MapNodeTypeTable.dataList)
                {
                    if (row == null) continue;
                    _mapNodeTypeByType[row.node_type] = row;
                }
            }

            if (_tableLinker?.MapEventTable == null)
            {
                Debug.LogError("[TableManager] MapEventTable이 없습니다.");
                _mapEvents = EmptyMapEvents;
            }
            else
            {
                var list = new List<MapEventData>(_tableLinker.MapEventTable.dataList.Count);

                foreach (var row in _tableLinker.MapEventTable.dataList)
                {
                    if (row == null) continue;

                    list.Add(row);

                    if (!string.IsNullOrEmpty(row.event_id))
                    {
                        _mapEventById[row.event_id] = row;
                    }
                }

                _mapEvents = list.Count > 0 ? list.ToArray() : EmptyMapEvents;
                BuildMapEventsByFloorIndex();
            }

            if (_tableLinker?.RestOptionTable == null)
            {
                Debug.LogError("[TableManager] RestOptionTable이 없습니다.");
                _restOptions = EmptyRestOptions;
            }
            else
            {
                var list = new List<RestOptionData>(_tableLinker.RestOptionTable.dataList.Count);

                foreach (var row in _tableLinker.RestOptionTable.dataList)
                {
                    if (row != null) list.Add(row);
                }

                _restOptions = list.Count > 0 ? list.ToArray() : EmptyRestOptions;
            }

            if (_tableLinker?.TreasureOptionTable == null)
            {
                Debug.LogError("[TableManager] TreasureOptionTable이 없습니다.");
                _treasureOptions = EmptyTreasureOptions;
            }
            else
            {
                var list = new List<TreasureOptionData>(_tableLinker.TreasureOptionTable.dataList.Count);

                foreach (var row in _tableLinker.TreasureOptionTable.dataList)
                {
                    if (row != null) list.Add(row);
                }

                _treasureOptions = list.Count > 0 ? list.ToArray() : EmptyTreasureOptions;
            }
        }

        private MapTemplateCache ParseMapTemplate(MapTemplateData template)
        {
            var cache = new MapTemplateCache
            {
                AllowedTypes = EmptyMapNodeTypes,
                TypeWeights = EmptyInts,
                HasForcedType = false,
            };

            var allowedTokens = SplitTrimmed(template.allowed_types);
            var weightTokens = SplitTrimmed(template.type_weights);

            if (allowedTokens.Count != weightTokens.Count)
            {
                Debug.LogError($"[TableManager] MapTemplate ({template.chapter}, {template.floor}) allowed_types/type_weights 개수 불일치");
                return cache;
            }

            var allowedTypes = new List<MapNodeType>(allowedTokens.Count);
            var typeWeights = new List<int>(weightTokens.Count);

            for (var i = 0; i < allowedTokens.Count; i++)
            {
                if (!System.Enum.TryParse(allowedTokens[i], out MapNodeType nodeType))
                {
                    Debug.LogError($"[TableManager] MapTemplate ({template.chapter}, {template.floor}) 잘못된 node type: {allowedTokens[i]}");
                    continue;
                }

                if (!int.TryParse(weightTokens[i], out var weight))
                {
                    Debug.LogError($"[TableManager] MapTemplate ({template.chapter}, {template.floor}) 잘못된 weight: {weightTokens[i]}");
                    continue;
                }

                allowedTypes.Add(nodeType);
                typeWeights.Add(weight);
            }

            cache.AllowedTypes = allowedTypes.Count > 0 ? allowedTypes.ToArray() : EmptyMapNodeTypes;
            cache.TypeWeights = typeWeights.Count > 0 ? typeWeights.ToArray() : EmptyInts;

            if (!string.IsNullOrWhiteSpace(template.forced_type))
            {
                if (System.Enum.TryParse(template.forced_type.Trim(), out MapNodeType forcedType))
                {
                    cache.HasForcedType = true;
                    cache.ForcedType = forcedType;
                }
                else
                {
                    Debug.LogError($"[TableManager] MapTemplate ({template.chapter}, {template.floor}) 잘못된 forced_type: {template.forced_type}");
                }
            }

            return cache;
        }

        private static List<string> SplitTrimmed(string csv)
        {
            var result = new List<string>();

            if (string.IsNullOrWhiteSpace(csv))
            {
                return result;
            }

            var parts = csv.Split(',');
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                {
                    result.Add(trimmed);
                }
            }

            return result;
        }

        private void BuildMapEventsByFloorIndex()
        {
            var maxFloor = 0;

            foreach (var pair in _mapFloorCountByChapter)
            {
                if (pair.Value > maxFloor)
                {
                    maxFloor = pair.Value;
                }
            }

            for (var floor = 1; floor <= maxFloor; floor++)
            {
                var eventsForFloor = new List<MapEventData>();

                for (var i = 0; i < _mapEvents.Length; i++)
                {
                    if (_mapEvents[i].min_floor <= floor)
                    {
                        eventsForFloor.Add(_mapEvents[i]);
                    }
                }

                _mapEventsByFloor[floor] = eventsForFloor.Count > 0 ? eventsForFloor.ToArray() : EmptyMapEvents;
            }
        }

        #endregion

        #region Map Accessors

        /// <summary>
        /// 챕터·층에 해당하는 맵 템플릿 데이터를 조회합니다.
        /// </summary>
        public static MapTemplateData GetMapTemplate(int chapter, int floor)
        {
            if (!TryGetTable(out var manager)) return null;

            return manager._mapTemplateByChapterFloor.TryGetValue((chapter, floor), out var template) ? template : null;
        }

        /// <summary>
        /// 챕터의 맵 층 수를 반환합니다.
        /// </summary>
        public static int GetMapFloorCount(int chapter)
        {
            if (!TryGetTable(out var manager)) return 0;

            return manager._mapFloorCountByChapter.TryGetValue(chapter, out var count) ? count : 0;
        }

        /// <summary>
        /// 챕터·층에서 허용되는 노드 타입 목록을 반환합니다.
        /// </summary>
        public static IReadOnlyList<MapNodeType> GetAllowedNodeTypes(int chapter, int floor)
        {
            if (!TryGetTable(out var manager)) return EmptyMapNodeTypes;

            if (!manager._mapTemplateCacheByChapterFloor.TryGetValue((chapter, floor), out var cache))
            {
                return EmptyMapNodeTypes;
            }

            return cache.AllowedTypes;
        }

        /// <summary>
        /// 챕터·층의 노드 타입 가중치 목록을 반환합니다.
        /// </summary>
        public static IReadOnlyList<int> GetNodeTypeWeights(int chapter, int floor)
        {
            if (!TryGetTable(out var manager)) return EmptyInts;

            if (!manager._mapTemplateCacheByChapterFloor.TryGetValue((chapter, floor), out var cache))
            {
                return EmptyInts;
            }

            return cache.TypeWeights;
        }

        /// <summary>
        /// 챕터·층에 강제 노드 타입이 설정되어 있는지 확인합니다.
        /// </summary>
        public static bool TryGetForcedNodeType(int chapter, int floor, out MapNodeType nodeType)
        {
            nodeType = default;

            if (!TryGetTable(out var manager)) return false;

            if (!manager._mapTemplateCacheByChapterFloor.TryGetValue((chapter, floor), out var cache))
            {
                return false;
            }

            if (!cache.HasForcedType)
            {
                return false;
            }

            nodeType = cache.ForcedType;
            return true;
        }

        /// <summary>
        /// MapNodeType 데이터를 조회합니다.
        /// </summary>
        public static MapNodeTypeData GetMapNodeType(MapNodeType nodeType)
        {
            if (!TryGetTable(out var manager)) return null;

            return manager._mapNodeTypeByType.TryGetValue(nodeType, out var row) ? row : null;
        }

        /// <summary>
        /// 모든 맵 이벤트 데이터 목록을 반환합니다.
        /// </summary>
        public static IReadOnlyList<MapEventData> GetMapEvents()
        {
            if (!TryGetTable(out var manager)) return EmptyMapEvents;

            return manager._mapEvents;
        }

        /// <summary>
        /// event_id로 맵 이벤트 데이터를 조회합니다.
        /// </summary>
        public static MapEventData GetMapEvent(string eventId)
        {
            if (!TryGetTable(out var manager)) return null;
            if (string.IsNullOrEmpty(eventId)) return null;

            return manager._mapEventById.TryGetValue(eventId, out var row) ? row : null;
        }

        /// <summary>
        /// 해당 층 이상에서 등장 가능한 맵 이벤트 목록을 반환합니다.
        /// </summary>
        public static IReadOnlyList<MapEventData> GetMapEventsForFloor(int floor)
        {
            if (!TryGetTable(out var manager)) return EmptyMapEvents;

            return manager._mapEventsByFloor.TryGetValue(floor, out var events) ? events : EmptyMapEvents;
        }

        /// <summary>
        /// 모든 휴식 노드 옵션 목록을 반환합니다.
        /// </summary>
        public static IReadOnlyList<RestOptionData> GetRestOptions()
        {
            if (!TryGetTable(out var manager)) return EmptyRestOptions;

            return manager._restOptions;
        }

        /// <summary>
        /// 모든 보물 노드 옵션 목록을 반환합니다.
        /// </summary>
        public static IReadOnlyList<TreasureOptionData> GetTreasureOptions()
        {
            if (!TryGetTable(out var manager)) return EmptyTreasureOptions;

            return manager._treasureOptions;
        }

        #endregion
    }
}
