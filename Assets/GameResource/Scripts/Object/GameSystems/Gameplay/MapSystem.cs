using System;
using System.Collections.Generic;
using Backend.Object.Management;
using R3;
using UnityEngine;

namespace Backend.Object.GameSystems.Gameplay
{
    /// <summary>
    /// 맵 런타임 상태·노드 선택/진입/완료를 관리한다.
    /// </summary>
    public static class MapSystem
    {
        #region Fields

        private static readonly ReactiveProperty<IReadOnlyList<MapNode>> _nodes = new(Array.Empty<MapNode>());
        private static readonly ReactiveProperty<int> _chapter = new(1);
        private static readonly ReactiveProperty<int> _currentFloor = new(0);
        private static readonly ReactiveProperty<int> _currentSlot = new(-1);
        private static readonly ReactiveProperty<(int floor, int slot)?> _selectedNode = new(null);
        private static readonly ReactiveProperty<int> _mapSeed = new(0);
        private static readonly ReactiveProperty<float> _threatScore = new(0f);
        private static readonly Subject<MapNode> _onNodeEntered = new();
        private static readonly Subject<Unit> _onReturnedToMap = new();
        private static readonly Subject<Unit> _onMapChanged = new();

        private static MapNode[] _mutableNodes = Array.Empty<MapNode>();
        private static readonly List<(int floor, int slot)> _pathHistory = new();
        private static readonly List<int> _selectableSlots = new();
        private static bool _isInitialized;
        private static bool _awaitingNodeCompletion;

        #endregion

        #region Properties

        public static ReadOnlyReactiveProperty<IReadOnlyList<MapNode>> Nodes => _nodes;
        public static ReadOnlyReactiveProperty<int> Chapter => _chapter;
        public static ReadOnlyReactiveProperty<int> CurrentFloor => _currentFloor;
        public static ReadOnlyReactiveProperty<int> CurrentSlot => _currentSlot;
        public static ReadOnlyReactiveProperty<(int floor, int slot)?> SelectedNode => _selectedNode;
        public static ReadOnlyReactiveProperty<int> MapSeed => _mapSeed;
        public static ReadOnlyReactiveProperty<float> ThreatScore => _threatScore;
        public static Observable<MapNode> OnNodeEntered => _onNodeEntered;
        public static Observable<Unit> OnReturnedToMap => _onReturnedToMap;
        public static Observable<Unit> OnMapChanged => _onMapChanged;
        public static IReadOnlyList<(int floor, int slot)> PathHistory => _pathHistory;
        public static IReadOnlyList<int> SelectableSlots => _selectableSlots;
        public static bool IsRunActive => _isInitialized && _mutableNodes.Length > 0;
        public static bool AwaitingNodeCompletion => _awaitingNodeCompletion;

        public static int ThreatTier => TableManager.GetThreatTier(_threatScore.CurrentValue);

        #endregion

        #region Lifecycle

        public static void Initialize()
        {
            _isInitialized = true;
            _awaitingNodeCompletion = false;
            _pathHistory.Clear();
            _selectableSlots.Clear();
            _selectedNode.Value = null;
            _threatScore.Value = 0f;
            _currentFloor.Value = 0;
            _currentSlot.Value = -1;
            _mutableNodes = Array.Empty<MapNode>();
            _nodes.Value = _mutableNodes;
        }

        public static void Dispose()
        {
            _isInitialized = false;
            _awaitingNodeCompletion = false;
            _pathHistory.Clear();
            _selectableSlots.Clear();
            _selectedNode.Value = null;
            _mutableNodes = Array.Empty<MapNode>();
            _nodes.Value = _mutableNodes;
        }

        /// <summary>
        /// 챕터 런을 시작하고 맵을 생성한다. Floor 1은 자동 진입한다.
        /// </summary>
        public static void StartRun(int chapter = 1, int? seed = null)
        {
            if (!_isInitialized)
                Initialize();

            _chapter.Value = Mathf.Max(1, chapter);
            _mapSeed.Value = seed ?? UnityEngine.Random.Range(1, int.MaxValue);
            _pathHistory.Clear();
            _selectedNode.Value = null;
            _awaitingNodeCompletion = false;

            _mutableNodes = MapGenerator.Generate(_chapter.CurrentValue, _mapSeed.CurrentValue);
            for (var i = 0; i < _mutableNodes.Length; i++)
            {
                var node = _mutableNodes[i];
                node.State = MapNodeState.Locked;
                _mutableNodes[i] = node;
            }

            PublishNodes();

            // Floor 1 tutorial node: no choice, enter immediately.
            if (TryGetNode(1, 0, out _))
            {
                SetNodeState(1, 0, MapNodeState.Selectable);
                RefreshSelectableFrom(0, -1, bootstrapFloor1: true);
                ConfirmEnter(1, 0);
            }
            else
            {
                GameManager.SetPhase(GamePhase.MapSelect);
                _onReturnedToMap.OnNext(Unit.Default);
            }
        }

        #endregion

        #region Selection / Enter

        public static bool TrySelectNode(int floor, int slot)
        {
            if (_awaitingNodeCompletion)
                return false;

            if (!TryGetNode(floor, slot, out var node))
                return false;

            if (node.State != MapNodeState.Selectable)
                return false;

            _selectedNode.Value = (floor, slot);
            _onMapChanged.OnNext(Unit.Default);
            return true;
        }

        public static void ClearSelection()
        {
            if (_selectedNode.CurrentValue == null)
                return;

            _selectedNode.Value = null;
            _onMapChanged.OnNext(Unit.Default);
        }

        public static bool ConfirmEnter()
        {
            if (_selectedNode.CurrentValue == null)
                return false;

            var (floor, slot) = _selectedNode.CurrentValue.Value;
            return ConfirmEnter(floor, slot);
        }

        public static bool ConfirmEnter(int floor, int slot)
        {
            if (_awaitingNodeCompletion)
                return false;

            if (!TryGetNode(floor, slot, out var node))
                return false;

            if (node.State != MapNodeState.Selectable && !(floor == 1 && slot == 0 && _pathHistory.Count == 0))
                return false;

            _selectedNode.Value = null;
            _currentFloor.Value = floor;
            _currentSlot.Value = slot;
            SetNodeState(floor, slot, MapNodeState.Current);
            LockNonPathNodes();
            _awaitingNodeCompletion = true;

            switch (node.NodeType)
            {
                case MapNodeType.Battle:
                case MapNodeType.Elite:
                case MapNodeType.Boss:
                    GameManager.SetPhase(GamePhase.PlayerTurn);
                    BattleSystem.StartBattle(
                        node.ContentId,
                        ThreatTier,
                        _chapter.CurrentValue,
                        isElite: node.NodeType == MapNodeType.Elite,
                        isBoss: node.NodeType == MapNodeType.Boss);
                    break;
                case MapNodeType.Rest:
                    GameManager.SetPhase(GamePhase.RestNode);
                    break;
                case MapNodeType.Event:
                    GameManager.SetPhase(GamePhase.EventNode);
                    break;
                case MapNodeType.Treasure:
                    GameManager.SetPhase(GamePhase.TreasureNode);
                    break;
            }

            _onNodeEntered.OnNext(node);
            _onMapChanged.OnNext(Unit.Default);
            return true;
        }

        /// <summary>
        /// 현재 노드를 클리어하고 맵 선택 화면으로 복귀한다.
        /// </summary>
        public static void CompleteCurrentNode()
        {
            if (!_awaitingNodeCompletion)
                return;

            var floor = _currentFloor.CurrentValue;
            var slot = _currentSlot.CurrentValue;
            if (!TryGetNode(floor, slot, out var node))
                return;

            SetNodeState(floor, slot, MapNodeState.Cleared);
            _pathHistory.Add((floor, slot));
            _awaitingNodeCompletion = false;

            if (node.NodeType == MapNodeType.Boss)
            {
                GameManager.SetPhase(GamePhase.Victory);
                _onMapChanged.OnNext(Unit.Default);
                return;
            }

            RefreshSelectableFrom(floor, slot, bootstrapFloor1: false);
            GameManager.SetPhase(GamePhase.MapSelect);
            _onReturnedToMap.OnNext(Unit.Default);
            _onMapChanged.OnNext(Unit.Default);
        }

        /// <summary>
        /// 비전투 노드 스텁용: 진입 즉시 완료 처리.
        /// </summary>
        public static void CompleteNonBattleStub()
        {
            if (!_awaitingNodeCompletion)
                return;

            if (!TryGetCurrentNode(out var node))
                return;

            if (node.NodeType is MapNodeType.Battle or MapNodeType.Elite or MapNodeType.Boss)
                return;

            CompleteCurrentNode();
        }

        public static void AddThreat(float delta)
        {
            _threatScore.Value = Mathf.Max(0f, _threatScore.CurrentValue + delta);
            _onMapChanged.OnNext(Unit.Default);
        }

        #endregion

        #region Queries

        public static bool TryGetNode(int floor, int slot, out MapNode node)
        {
            for (var i = 0; i < _mutableNodes.Length; i++)
            {
                if (_mutableNodes[i].Floor == floor && _mutableNodes[i].Slot == slot)
                {
                    node = _mutableNodes[i];
                    return true;
                }
            }

            node = default;
            return false;
        }

        public static bool TryGetCurrentNode(out MapNode node)
        {
            return TryGetNode(_currentFloor.CurrentValue, _currentSlot.CurrentValue, out node);
        }

        public static bool TryGetSelectedNode(out MapNode node)
        {
            node = default;
            if (_selectedNode.CurrentValue == null)
                return false;

            var (floor, slot) = _selectedNode.CurrentValue.Value;
            return TryGetNode(floor, slot, out node);
        }

        public static bool IsSelectable(int floor, int slot)
        {
            return TryGetNode(floor, slot, out var node) && node.State == MapNodeState.Selectable;
        }

        /// <summary>
        /// 상세 시트용 몬스터 실수치 프리뷰.
        /// </summary>
        public static bool TryGetMonsterPreview(MapNode node, out int hp, out int atk, out string nameKey)
        {
            hp = 0;
            atk = 0;
            nameKey = string.Empty;

            if (node.NodeType is not (MapNodeType.Battle or MapNodeType.Elite or MapNodeType.Boss))
                return false;

            var monster = TableManager.GetMonster(node.ContentId);
            if (monster == null)
                return false;

            nameKey = monster.name_key;
            var scaling = TableManager.GetThreatScalingByTier(ThreatTier) ?? TableManager.GetThreatScaling(0);
            var hpMult = scaling?.hp_mult ?? 1f;
            var atkMult = scaling?.atk_mult ?? 1f;

            if (node.NodeType == MapNodeType.Elite)
            {
                var elite = TableManager.GetEliteScaling(_chapter.CurrentValue);
                if (elite != null)
                {
                    hpMult *= elite.hp_mult;
                    atkMult *= elite.atk_mult;
                    hpMult = Mathf.Min(hpMult, TableManager.GetFloat(TableManager.BalanceKey.HpMultCap, 2.2f));
                    atkMult = Mathf.Min(atkMult, TableManager.GetFloat(TableManager.BalanceKey.AtkMultCap, 1.5f));
                    hp = Mathf.CeilToInt(monster.max_hp * hpMult) + elite.bonus_max_hp;
                    atk = Mathf.CeilToInt(monster.base_attack * atkMult);
                    return true;
                }
            }

            if (node.NodeType == MapNodeType.Boss)
            {
                atkMult = Mathf.Min(atkMult, TableManager.GetFloat(TableManager.BalanceKey.BossAtkMultCap, 1.35f));
            }

            hp = Mathf.CeilToInt(monster.max_hp * hpMult);
            atk = Mathf.CeilToInt(monster.base_attack * atkMult);
            return true;
        }

        #endregion

        #region Internal

        private static void RefreshSelectableFrom(int fromFloor, int fromSlot, bool bootstrapFloor1)
        {
            _selectableSlots.Clear();

            for (var i = 0; i < _mutableNodes.Length; i++)
            {
                var node = _mutableNodes[i];
                if (node.State == MapNodeState.Selectable)
                {
                    node.State = MapNodeState.Locked;
                    _mutableNodes[i] = node;
                }
            }

            if (bootstrapFloor1)
            {
                SetNodeState(1, 0, MapNodeState.Selectable);
                _selectableSlots.Add(0);
                PublishNodes();
                return;
            }

            if (!TryGetNode(fromFloor, fromSlot, out var current))
            {
                PublishNodes();
                return;
            }

            var nextFloor = fromFloor + 1;
            if (current.NextSlots == null || current.NextSlots.Length == 0)
            {
                PublishNodes();
                return;
            }

            foreach (var nextSlot in current.NextSlots)
            {
                if (!TryGetNode(nextFloor, nextSlot, out var next))
                    continue;

                if (next.State == MapNodeState.Cleared)
                    continue;

                SetNodeState(nextFloor, nextSlot, MapNodeState.Selectable);
                _selectableSlots.Add(nextSlot);
            }

            PublishNodes();
        }

        private static void LockNonPathNodes()
        {
            for (var i = 0; i < _mutableNodes.Length; i++)
            {
                var node = _mutableNodes[i];
                if (node.State == MapNodeState.Selectable)
                {
                    node.State = MapNodeState.Locked;
                    _mutableNodes[i] = node;
                }
            }

            _selectableSlots.Clear();
            PublishNodes();
        }

        private static void SetNodeState(int floor, int slot, MapNodeState state)
        {
            for (var i = 0; i < _mutableNodes.Length; i++)
            {
                if (_mutableNodes[i].Floor != floor || _mutableNodes[i].Slot != slot)
                    continue;

                var node = _mutableNodes[i];
                node.State = state;
                _mutableNodes[i] = node;
                break;
            }

            PublishNodes();
        }

        private static void PublishNodes()
        {
            _nodes.Value = _mutableNodes;
        }

        #endregion
    }
}
