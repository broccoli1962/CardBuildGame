using System;
using System.Collections.Generic;
using Backend.Object.Management;
using UnityEngine;

namespace Backend.Object.GameSystems.Gameplay
{
    /// <summary>
    /// 시드 기반 맵 절차 생성기. TableManager 템플릿·가드레일을 적용하며 결정론적으로 동작한다.
    /// </summary>
    public static class MapGenerator
    {
        private const int MaxAttempts = 20;

        public static MapNode[] Generate(int chapter, int seed)
        {
            for (var attempt = 0; attempt < MaxAttempts; attempt++)
            {
                var rng = new System.Random(unchecked(seed + attempt * 9973));
                if (TryGenerate(chapter, rng, out var nodes) && Validate(nodes, chapter))
                    return nodes;
            }

            Debug.LogWarning($"[MapGenerator] Guardrail failed for seed={seed}. Using fallback map.");
            return BuildFallbackMap(chapter);
        }

        private static bool TryGenerate(int chapter, System.Random rng, out MapNode[] nodes)
        {
            nodes = null;
            var floorCount = TableManager.GetMapFloorCount(chapter);
            if (floorCount <= 0)
                return false;

            var floors = new List<MapNode>[floorCount + 1];
            for (var floor = 1; floor <= floorCount; floor++)
            {
                var template = TableManager.GetMapTemplate(chapter, floor);
                if (template == null)
                    return false;

                var allowed = TableManager.GetAllowedNodeTypes(chapter, floor);
                var weights = TableManager.GetNodeTypeWeights(chapter, floor);
                if (allowed.Count == 0 || allowed.Count != weights.Count)
                    return false;

                var slotCount = Mathf.Max(1, template.slot_count);
                var types = PickTypesForFloor(chapter, floor, slotCount, allowed, weights, rng);
                if (types == null)
                    return false;

                floors[floor] = new List<MapNode>(slotCount);
                for (var slot = 0; slot < slotCount; slot++)
                {
                    floors[floor].Add(new MapNode
                    {
                        Floor = floor,
                        Slot = slot,
                        NodeType = types[slot],
                        ContentId = ResolveContentId(chapter, floor, types[slot], rng),
                        NextSlots = Array.Empty<int>(),
                        State = MapNodeState.Locked,
                    });
                }
            }

            if (!BuildEdges(floors, floorCount, rng))
                return false;

            var list = new List<MapNode>(floorCount * 3);
            for (var floor = 1; floor <= floorCount; floor++)
                list.AddRange(floors[floor]);

            nodes = list.ToArray();
            return true;
        }

        private static MapNodeType[] PickTypesForFloor(
            int chapter,
            int floor,
            int slotCount,
            IReadOnlyList<MapNodeType> allowed,
            IReadOnlyList<int> weights,
            System.Random rng)
        {
            var result = new MapNodeType[slotCount];
            var used = new HashSet<MapNodeType>();

            var hasForced = TableManager.TryGetForcedNodeType(chapter, floor, out var forced);

            for (var i = 0; i < slotCount; i++)
            {
                MapNodeType picked;
                if (hasForced && i == 0)
                {
                    picked = forced;
                }
                else
                {
                    picked = WeightedPick(allowed, weights, used, floor, rng);
                }

                if (!IsTypeAllowedOnFloor(picked, floor))
                    return null;

                if (picked != MapNodeType.Battle && used.Contains(picked))
                    return null;

                result[i] = picked;
                used.Add(picked);
            }

            if (hasForced)
            {
                var found = false;
                for (var i = 0; i < result.Length; i++)
                {
                    if (result[i] == forced)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                    result[0] = forced;
            }

            Shuffle(result, rng);
            return result;
        }

        private static MapNodeType WeightedPick(
            IReadOnlyList<MapNodeType> allowed,
            IReadOnlyList<int> weights,
            HashSet<MapNodeType> used,
            int floor,
            System.Random rng)
        {
            var total = 0;
            for (var i = 0; i < allowed.Count; i++)
            {
                var type = allowed[i];
                if (!IsTypeAllowedOnFloor(type, floor))
                    continue;
                if (type != MapNodeType.Battle && used.Contains(type))
                    continue;
                total += Mathf.Max(0, weights[i]);
            }

            if (total <= 0)
                return MapNodeType.Battle;

            var roll = rng.Next(total);
            var acc = 0;
            for (var i = 0; i < allowed.Count; i++)
            {
                var type = allowed[i];
                if (!IsTypeAllowedOnFloor(type, floor))
                    continue;
                if (type != MapNodeType.Battle && used.Contains(type))
                    continue;

                acc += Mathf.Max(0, weights[i]);
                if (roll < acc)
                    return type;
            }

            return MapNodeType.Battle;
        }

        private static bool IsTypeAllowedOnFloor(MapNodeType type, int floor)
        {
            var data = TableManager.GetMapNodeType(type);
            if (data == null)
                return type == MapNodeType.Battle;

            return floor >= data.min_floor;
        }

        private static string ResolveContentId(int chapter, int floor, MapNodeType type, System.Random rng)
        {
            switch (type)
            {
                case MapNodeType.Battle:
                case MapNodeType.Elite:
                case MapNodeType.Boss:
                {
                    var pool = TableManager.GetMonsterPool(chapter, floor);
                    if (pool == null || pool.Count == 0)
                        return string.Empty;
                    return pool[rng.Next(pool.Count)];
                }
                case MapNodeType.Event:
                {
                    var events = TableManager.GetMapEventsForFloor(floor);
                    if (events == null || events.Count == 0)
                        return string.Empty;
                    return events[rng.Next(events.Count)].event_id;
                }
                default:
                    return string.Empty;
            }
        }

        private static bool BuildEdges(List<MapNode>[] floors, int floorCount, System.Random rng)
        {
            for (var floor = 1; floor < floorCount; floor++)
            {
                var current = floors[floor];
                var next = floors[floor + 1];
                var connections = new List<int>[current.Count];
                for (var i = 0; i < current.Count; i++)
                    connections[i] = new List<int>(2);

                var incoming = new HashSet<int>[next.Count];
                for (var i = 0; i < next.Count; i++)
                    incoming[i] = new HashSet<int>();

                // Primary adjacent links: each node connects to nearest next slot.
                for (var i = 0; i < current.Count; i++)
                {
                    var mapped = MapSlotIndex(i, current.Count, next.Count);
                    TryAddEdge(connections, incoming, i, mapped);
                }

                // Ensure every next node is reachable.
                for (var j = 0; j < next.Count; j++)
                {
                    if (incoming[j].Count > 0)
                        continue;

                    var best = 0;
                    var bestDist = int.MaxValue;
                    for (var i = 0; i < current.Count; i++)
                    {
                        var dist = Mathf.Abs(MapSlotIndex(i, current.Count, next.Count) - j);
                        if (dist < bestDist && connections[i].Count < 2)
                        {
                            bestDist = dist;
                            best = i;
                        }
                    }

                    TryAddEdge(connections, incoming, best, j);
                }

                // Optional second branch when slots allow (non-crossing).
                for (var i = 0; i < current.Count; i++)
                {
                    if (connections[i].Count >= 2 || rng.NextDouble() > 0.45d)
                        continue;

                    var primary = connections[i][0];
                    for (var delta = -1; delta <= 1; delta += 2)
                    {
                        var candidate = primary + delta;
                        if (candidate < 0 || candidate >= next.Count)
                            continue;
                        if (Mathf.Abs(MapSlotIndex(i, current.Count, next.Count) - candidate) > 1)
                            continue;
                        if (WouldCross(connections, i, candidate))
                            continue;
                        if (TryAddEdge(connections, incoming, i, candidate))
                            break;
                    }
                }

                for (var i = 0; i < current.Count; i++)
                {
                    if (connections[i].Count == 0)
                        return false;

                    var node = current[i];
                    node.NextSlots = connections[i].ToArray();
                    current[i] = node;
                }
            }

            // Boss floor has no outgoing edges.
            var bossFloor = floors[floorCount];
            for (var i = 0; i < bossFloor.Count; i++)
            {
                var node = bossFloor[i];
                node.NextSlots = Array.Empty<int>();
                bossFloor[i] = node;
            }

            return true;
        }

        private static bool TryAddEdge(List<int>[] connections, HashSet<int>[] incoming, int from, int to)
        {
            if (connections[from].Contains(to))
                return false;
            if (connections[from].Count >= 2)
                return false;
            if (WouldCross(connections, from, to))
                return false;

            connections[from].Add(to);
            connections[from].Sort();
            incoming[to].Add(from);
            return true;
        }

        private static bool WouldCross(List<int>[] connections, int from, int to)
        {
            for (var i = 0; i < connections.Length; i++)
            {
                if (i == from)
                    continue;

                foreach (var existingTo in connections[i])
                {
                    if ((i - from) * (existingTo - to) < 0)
                        return true;
                }
            }

            return false;
        }

        private static int MapSlotIndex(int slot, int fromCount, int toCount)
        {
            if (toCount <= 1)
                return 0;
            if (fromCount <= 1)
                return toCount / 2;

            return Mathf.Clamp(Mathf.RoundToInt(slot * (toCount - 1f) / (fromCount - 1f)), 0, toCount - 1);
        }

        private static bool Validate(MapNode[] nodes, int chapter)
        {
            if (nodes == null || nodes.Length == 0)
                return false;

            var byFloor = new Dictionary<int, List<MapNode>>();
            foreach (var node in nodes)
            {
                if (!byFloor.TryGetValue(node.Floor, out var list))
                {
                    list = new List<MapNode>();
                    byFloor[node.Floor] = list;
                }

                list.Add(node);
            }

            var floorCount = TableManager.GetMapFloorCount(chapter);
            if (floorCount <= 0 || byFloor.Count != floorCount)
                return false;

            if (!byFloor.TryGetValue(1, out var floor1) || floor1.Count != 1 || floor1[0].NodeType != MapNodeType.Battle)
                return false;

            if (!byFloor.TryGetValue(floorCount, out var bossFloor) ||
                bossFloor.Count != 1 ||
                bossFloor[0].NodeType != MapNodeType.Boss)
                return false;

            if (byFloor.TryGetValue(4, out var floor4))
            {
                var restCount = 0;
                foreach (var node in floor4)
                {
                    if (node.NodeType == MapNodeType.Rest)
                        restCount++;
                }

                if (restCount < 1)
                    return false;
            }

            var eliteTotal = 0;
            var hasCombatOnlyFloor = false;
            var prevHadRest = false;

            for (var floor = 1; floor <= floorCount; floor++)
            {
                if (!byFloor.TryGetValue(floor, out var list))
                    return false;

                if (floor >= 2 && floor <= floorCount - 1 && list.Count < 2)
                    return false;

                var typeSet = new HashSet<MapNodeType>();
                var eliteOnFloor = 0;
                var allCombat = true;
                var hasRest = false;

                foreach (var node in list)
                {
                    if (node.NodeType != MapNodeType.Battle && !typeSet.Add(node.NodeType))
                        return false;

                    if (node.NodeType == MapNodeType.Elite)
                    {
                        eliteOnFloor++;
                        eliteTotal++;
                        if (floor < 3)
                            return false;
                    }

                    if (node.NodeType == MapNodeType.Rest)
                        hasRest = true;

                    if (node.NodeType != MapNodeType.Battle && node.NodeType != MapNodeType.Elite)
                        allCombat = false;

                    if (floor < floorCount && (node.NextSlots == null || node.NextSlots.Length == 0))
                        return false;
                }

                if (eliteOnFloor > 1)
                    return false;

                if (hasRest && prevHadRest)
                    return false;
                prevHadRest = hasRest;

                if (floor >= 2 && floor <= floorCount - 1 && allCombat)
                    hasCombatOnlyFloor = true;
            }

            if (eliteTotal > 2)
                return false;

            if (floorCount >= 4 && !hasCombatOnlyFloor)
                return false;

            return true;
        }

        private static MapNode[] BuildFallbackMap(int chapter)
        {
            var floorCount = Mathf.Max(5, TableManager.GetMapFloorCount(chapter));
            var nodes = new List<MapNode>
            {
                Make(1, 0, MapNodeType.Battle, PickMonster(chapter, 1, 0), new[] { 0, 1 }),
                Make(2, 0, MapNodeType.Battle, PickMonster(chapter, 2, 0), new[] { 0, 1 }),
                Make(2, 1, MapNodeType.Event, PickEvent(2, 1), new[] { 1, 2 }),
                Make(3, 0, MapNodeType.Elite, PickMonster(chapter, 3, 0), new[] { 0 }),
                Make(3, 1, MapNodeType.Battle, PickMonster(chapter, 3, 1), new[] { 0, 1 }),
                Make(3, 2, MapNodeType.Treasure, string.Empty, new[] { 1, 2 }),
                Make(4, 0, MapNodeType.Rest, string.Empty, new[] { 0 }),
                Make(4, 1, MapNodeType.Battle, PickMonster(chapter, 4, 1), new[] { 0 }),
                Make(4, 2, MapNodeType.Treasure, string.Empty, new[] { 0 }),
                Make(5, 0, MapNodeType.Boss, PickMonster(chapter, 5, 0), Array.Empty<int>()),
            };

            // Ensure floor count matches template when smaller/larger.
            while (nodes.Count > 0 && nodes[^1].Floor > floorCount)
                nodes.RemoveAt(nodes.Count - 1);

            return nodes.ToArray();
        }

        private static MapNode Make(int floor, int slot, MapNodeType type, string contentId, int[] next)
        {
            return new MapNode
            {
                Floor = floor,
                Slot = slot,
                NodeType = type,
                ContentId = contentId ?? string.Empty,
                NextSlots = next ?? Array.Empty<int>(),
                State = MapNodeState.Locked,
            };
        }

        private static string PickMonster(int chapter, int floor, int index)
        {
            var pool = TableManager.GetMonsterPool(chapter, floor);
            if (pool == null || pool.Count == 0)
                return string.Empty;
            return pool[Mathf.Clamp(index, 0, pool.Count - 1)];
        }

        private static string PickEvent(int floor, int index)
        {
            var events = TableManager.GetMapEventsForFloor(floor);
            if (events == null || events.Count == 0)
                return string.Empty;
            return events[Mathf.Clamp(index, 0, events.Count - 1)].event_id;
        }

        private static void Shuffle(MapNodeType[] array, System.Random rng)
        {
            for (var i = array.Length - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                (array[i], array[j]) = (array[j], array[i]);
            }
        }
    }
}
