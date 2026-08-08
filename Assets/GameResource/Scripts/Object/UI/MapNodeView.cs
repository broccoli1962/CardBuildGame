using System;
using Backend.AddressableKey;
using Backend.Object.Management;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Backend.Object.UI
{
    /// <summary>
    /// 맵 그래프의 개별 노드 뷰. TableManager icon_key 스프라이트를 표시한다.
    /// </summary>
    public class MapNodeView : MonoBehaviour
    {
        [SerializeField] private RectTransform _rectTransform;
        [SerializeField] private Image _background;
        [SerializeField] private Image _border;
        [SerializeField] private Image _iconImage;
        [SerializeField] private TextMeshProUGUI _iconText;
        [SerializeField] private TextMeshProUGUI _labelText;
        [SerializeField] private TextMeshProUGUI _clearedMark;
        [SerializeField] private CommonButton _button;
        [SerializeField] private CanvasGroup _canvasGroup;

        private int _floor;
        private int _slot;
        private Action<int, int> _onClicked;
        private string _loadedIconKey;
        private int _bindVersion;

        public int Floor => _floor;
        public int Slot => _slot;
        public RectTransform RectTransform => _rectTransform != null ? _rectTransform : (RectTransform)transform;

        private void Awake()
        {
            EnsureRefs();
        }

        /// <summary>
        /// 프리팹 없이 런타임 생성할 때 참조를 주입합니다.
        /// </summary>
        public void WireRuntime(
            RectTransform rectTransform,
            Image background,
            Image border,
            Image iconImage,
            TextMeshProUGUI iconText,
            TextMeshProUGUI labelText,
            CommonButton button,
            CanvasGroup canvasGroup,
            TextMeshProUGUI clearedMark = null)
        {
            _rectTransform = rectTransform;
            _background = background;
            _border = border;
            _iconImage = iconImage;
            _iconText = iconText;
            _labelText = labelText;
            _button = button;
            _canvasGroup = canvasGroup;
            _clearedMark = clearedMark;
        }

        public void Bind(int floor, int slot, MapNodeType nodeType, MapNodeState state, Action<int, int> onClicked)
        {
            EnsureRefs();
            _floor = floor;
            _slot = slot;
            _onClicked = onClicked;
            _bindVersion++;

            if (_labelText != null)
                _labelText.text = $"F{floor}";

            if (_clearedMark != null)
            {
                _clearedMark.gameObject.SetActive(state == MapNodeState.Cleared);
                _clearedMark.text = "OK";
            }

            ApplyState(nodeType, state);
            LoadIconAsync(nodeType, _bindVersion).Forget();

            if (_button != null)
            {
                if (_button.OnClick == null)
                    _button.OnClick = new UnityEngine.Events.UnityEvent();

                _button.OnClick.RemoveListener(HandleClick);
                _button.OnClick.AddListener(HandleClick);
                _button.interactable = state is MapNodeState.Selectable or MapNodeState.Cleared or MapNodeState.Current;
                // Runtime-spawned buttons have no Addressable SFX clip yet.
                _button.useSound = false;
            }
        }

        public void SetAnchoredPosition(Vector2 position)
        {
            RectTransform.anchoredPosition = position;
        }

        private async UniTaskVoid LoadIconAsync(MapNodeType nodeType, int version)
        {
            var typeData = TableManager.GetMapNodeType(nodeType);
            var iconKey = typeData != null && !string.IsNullOrEmpty(typeData.icon_key)
                ? typeData.icon_key
                : GetFallbackIconKey(nodeType);

            if (_loadedIconKey == iconKey && _iconImage != null && _iconImage.sprite != null)
            {
                if (_iconText != null)
                    _iconText.enabled = false;
                return;
            }

            var address = AddressableKeys.Icons.Get(iconKey);
            if (string.IsNullOrEmpty(address))
            {
                ShowTextFallback(nodeType);
                return;
            }

            var sprite = await ResourceManager.LoadResourceAsync<Sprite>(address);
            if (version != _bindVersion)
                return;

            if (sprite == null)
            {
                ShowTextFallback(nodeType);
                return;
            }

            _loadedIconKey = iconKey;
            if (_iconImage != null)
            {
                _iconImage.sprite = sprite;
                _iconImage.enabled = true;
                _iconImage.preserveAspect = true;
                _iconImage.color = Color.white;
            }

            if (_iconText != null)
                _iconText.enabled = false;
        }

        private void ShowTextFallback(MapNodeType nodeType)
        {
            if (_iconImage != null)
                _iconImage.enabled = false;

            if (_iconText != null)
            {
                _iconText.enabled = true;
                _iconText.text = GetEmojiFallback(nodeType);
            }
        }

        private void HandleClick()
        {
            _onClicked?.Invoke(_floor, _slot);
        }

        private void EnsureRefs()
        {
            if (_rectTransform == null)
                _rectTransform = transform as RectTransform;
            if (_canvasGroup == null)
                TryGetComponent(out _canvasGroup);
            if (_button == null)
                TryGetComponent(out _button);
        }

        private void ApplyState(MapNodeType nodeType, MapNodeState state)
        {
            var typeColor = GetTypeColor(nodeType, TableManager.GetMapNodeType(nodeType)?.color_hex);

            if (_background != null)
            {
                // Icon art already includes circular fill; keep plate subtle.
                _background.color = state switch
                {
                    MapNodeState.Cleared => new Color(0.09f, 0.4f, 0.2f, 0.55f),
                    MapNodeState.Current => new Color(0.79f, 0.64f, 0.15f, 0.2f),
                    _ => new Color(0.03f, 0.03f, 0.06f, 0.35f),
                };
            }

            // Icon sprites already include circular borders; keep an outer ring for selection only.
            if (_border != null)
            {
                var showRing = state is MapNodeState.Selectable or MapNodeState.Current or MapNodeState.Cleared;
                _border.enabled = showRing;
                _border.color = state switch
                {
                    MapNodeState.Cleared => new Color(0.13f, 0.77f, 0.37f, 0.9f),
                    MapNodeState.Current => new Color(0.79f, 0.64f, 0.15f, 1f),
                    MapNodeState.Selectable => new Color(typeColor.r, typeColor.g, typeColor.b, 0.85f),
                    _ => Color.clear,
                };
            }

            if (_iconImage != null)
            {
                _iconImage.color = state == MapNodeState.Locked
                    ? new Color(0.65f, 0.65f, 0.65f, 1f)
                    : Color.white;
            }

            if (_canvasGroup != null)
                _canvasGroup.alpha = state == MapNodeState.Locked ? 0.35f : 1f;

            var scale = state == MapNodeState.Current ? 1.12f : state == MapNodeState.Selectable ? 1.06f : 1f;
            if (nodeType == MapNodeType.Boss)
                scale *= 1.15f;
            RectTransform.localScale = Vector3.one * scale;
        }

        private static string GetFallbackIconKey(MapNodeType type) => type switch
        {
            MapNodeType.Battle => "Node_Battle",
            MapNodeType.Elite => "Node_Elite",
            MapNodeType.Rest => "Node_Rest",
            MapNodeType.Event => "Node_Event",
            MapNodeType.Treasure => "Node_Treasure",
            MapNodeType.Boss => "Node_Boss",
            _ => "Node_Battle",
        };

        private static string GetEmojiFallback(MapNodeType type) => type switch
        {
            MapNodeType.Battle => "전투",
            MapNodeType.Elite => "정예",
            MapNodeType.Rest => "휴식",
            MapNodeType.Event => "사건",
            MapNodeType.Treasure => "보물",
            MapNodeType.Boss => "보스",
            _ => "-",
        };

        private static Color GetTypeColor(MapNodeType type, string colorHex)
        {
            if (!string.IsNullOrEmpty(colorHex) && ColorUtility.TryParseHtmlString(colorHex, out var parsed))
                return parsed;

            return type switch
            {
                MapNodeType.Battle => new Color(0.90f, 0.24f, 0.24f),
                MapNodeType.Elite => new Color(0.73f, 0.11f, 0.11f),
                MapNodeType.Rest => new Color(0.98f, 0.45f, 0.09f),
                MapNodeType.Event => new Color(0.62f, 0.48f, 0.92f),
                MapNodeType.Treasure => new Color(0.79f, 0.64f, 0.15f),
                MapNodeType.Boss => new Color(0.90f, 0.24f, 0.24f),
                _ => Color.white,
            };
        }

        private void OnDestroy()
        {
            if (_button != null && _button.OnClick != null)
                _button.OnClick.RemoveListener(HandleClick);
        }
    }
}
