using System;
using System.Collections.Generic;
using Backend.Object.GameSystems.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Backend.Object.UI
{
    /// <summary>
    /// MapPanel / MapPreviewPopup 이 공유하는 맵 그래프 렌더러.
    /// </summary>
    public class MapGraphView : MonoBehaviour
    {
        [SerializeField] private RectTransform _contentRoot;
        [SerializeField] private RectTransform _edgeRoot;
        [SerializeField] private RectTransform _nodeRoot;
        [SerializeField] private MapNodeView _nodePrefab;
        [SerializeField] private float _floorSpacing = 140f;
        [SerializeField] private float _slotSpacing = 100f;
        [SerializeField] private Vector2 _origin = new(0f, -240f);

        private readonly List<MapNodeView> _spawnedNodes = new();
        private readonly List<Image> _spawnedEdges = new();
        private readonly Dictionary<(int floor, int slot), Vector2> _positions = new();
        private Action<int, int> _onNodeClicked;
        private bool _interactable = true;

        public void SetInteractable(bool interactable)
        {
            _interactable = interactable;
        }

        public void Bind(IReadOnlyList<MapNode> nodes, Action<int, int> onNodeClicked)
        {
            _onNodeClicked = onNodeClicked;
            EnsureRoots();
            ClearVisuals();

            if (nodes == null || nodes.Count == 0)
                return;

            var maxFloor = 1;
            var maxSlotsByFloor = new Dictionary<int, int>();
            foreach (var node in nodes)
            {
                maxFloor = Mathf.Max(maxFloor, node.Floor);
                if (!maxSlotsByFloor.TryGetValue(node.Floor, out var count) || node.Slot + 1 > count)
                    maxSlotsByFloor[node.Floor] = node.Slot + 1;
            }

            foreach (var node in nodes)
            {
                var slotCount = maxSlotsByFloor[node.Floor];
                var x = (node.Slot - (slotCount - 1) * 0.5f) * _slotSpacing;
                var y = _origin.y + (node.Floor - 1) * _floorSpacing;
                _positions[(node.Floor, node.Slot)] = new Vector2(x, y);
            }

            foreach (var node in nodes)
            {
                if (node.NextSlots == null)
                    continue;

                foreach (var nextSlot in node.NextSlots)
                {
                    if (!_positions.TryGetValue((node.Floor, node.Slot), out var from))
                        continue;
                    if (!_positions.TryGetValue((node.Floor + 1, nextSlot), out var to))
                        continue;

                    var traversed = node.State == MapNodeState.Cleared;
                    var selectablePath = node.State == MapNodeState.Current;
                    SpawnEdge(from, to, traversed, selectablePath);
                }
            }

            foreach (var node in nodes)
            {
                var view = SpawnNode();
                if (view == null)
                    continue;

                view.SetAnchoredPosition(_positions[(node.Floor, node.Slot)]);
                view.Bind(node.Floor, node.Slot, node.NodeType, node.State, HandleNodeClicked);
                _spawnedNodes.Add(view);
            }

            if (_contentRoot != null)
            {
                var height = Mathf.Max(640f, (maxFloor + 1) * _floorSpacing + 160f);
                _contentRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            }
        }

        private void HandleNodeClicked(int floor, int slot)
        {
            if (!_interactable)
                return;

            _onNodeClicked?.Invoke(floor, slot);
        }

        private MapNodeView SpawnNode()
        {
            if (_nodePrefab == null)
            {
                Debug.LogError("[MapGraphView] MapNodeView prefab is not assigned.");
                return null;
            }

            return Instantiate(_nodePrefab, _nodeRoot);
        }

        private void SpawnEdge(Vector2 from, Vector2 to, bool traversed, bool selectablePath)
        {
            var go = new GameObject("Edge", typeof(RectTransform), typeof(Image));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(_edgeRoot, false);

            var delta = to - from;
            var distance = delta.magnitude;
            rect.sizeDelta = new Vector2(Mathf.Max(2f, distance), traversed ? 3f : 2f);
            rect.anchoredPosition = (from + to) * 0.5f;
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.color = traversed
                ? new Color(0.79f, 0.64f, 0.15f, 1f)
                : selectablePath
                    ? new Color(0.79f, 0.64f, 0.15f, 0.55f)
                    : new Color(1f, 1f, 1f, 0.08f);

            _spawnedEdges.Add(image);
        }

        private void EnsureRoots()
        {
            if (_contentRoot == null)
                _contentRoot = transform as RectTransform;

            if (_edgeRoot == null)
            {
                var edgeGo = new GameObject("Edges", typeof(RectTransform));
                _edgeRoot = edgeGo.GetComponent<RectTransform>();
                _edgeRoot.SetParent(_contentRoot, false);
                Stretch(_edgeRoot);
            }

            if (_nodeRoot == null)
            {
                var nodeGo = new GameObject("Nodes", typeof(RectTransform));
                _nodeRoot = nodeGo.GetComponent<RectTransform>();
                _nodeRoot.SetParent(_contentRoot, false);
                Stretch(_nodeRoot);
            }
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void ClearVisuals()
        {
            foreach (var node in _spawnedNodes)
            {
                if (node != null)
                    Destroy(node.gameObject);
            }

            _spawnedNodes.Clear();

            foreach (var edge in _spawnedEdges)
            {
                if (edge != null)
                    Destroy(edge.gameObject);
            }

            _spawnedEdges.Clear();
            _positions.Clear();
        }
    }
}
