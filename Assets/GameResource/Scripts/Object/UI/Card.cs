using Backend.AddressableKey;
using Backend.Object.GameSystems.Gameplay;
using Backend.Object.GameSystems.Llm;
using Backend.Object.Management;
using Backend.Util;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Backend.Object.UI
{
    public class Card : CachedMonobehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image _frameImage;
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMPro.TextMeshProUGUI _nameText;
        [SerializeField] private TMPro.TextMeshProUGUI _descriptionText;
        [SerializeField] private TMPro.TextMeshProUGUI _costText;
        [SerializeField] private TMPro.TextMeshProUGUI _valueText;

        [SerializeField] private float _hoverScale = 1.08f;
        [SerializeField] private float _hoverLiftY = 30f;
        [SerializeField] private float _hoverDuration = 0.15f;

        private MotionHandle _scaleHandle;
        private MotionHandle _positionHandle;
        private Vector2 _restAnchoredPosition;
        private bool _isHovered;

        private RuntimeCard _runtimeCard;
        private string _loadedFrameKey;
        private string _loadedIconKey;
        private int _bindVersion;
        private bool _hoverEnabled = true;
        private bool _interactable = true;

        /// <summary>
        /// 현재 바인딩된 런타임 카드입니다.
        /// </summary>
        public RuntimeCard BoundCard => _runtimeCard;

        /// <summary>
        /// 런타임 카드 데이터를 UI 텍스트·프레임·아이콘에 바인딩합니다.
        /// </summary>
        public void Bind(RuntimeCard card)
        {
            if (card == null)
            {
                Debug.LogError("[Card] RuntimeCard is null.");
                return;
            }

            _runtimeCard = card;
            _bindVersion++;
            ResetTravelVisual();

            if (_nameText != null)
            {
                _nameText.text = card.IsGenerated
                    ? card.DisplayName
                    : card.NameKey.GetLocalizeText();
            }

            if (_descriptionText != null)
            {
                _descriptionText.richText = true;
                _descriptionText.text = card.IsGenerated
                    ? CardDescriptionFormatter.ColorizeDamageFormKeywords(card.DisplayDescription)
                    : card.DescKey.GetLocalizeText();
            }

            if (_costText != null)
                _costText.text = card.ManaCost.ToString();

            if (_valueText != null)
                _valueText.text = card.Effects.Count > 0 ? card.Effects[0].Value.ToString() : string.Empty;

            LoadVisualsAsync(card, _bindVersion).Forget();
        }

        /// <summary>
        /// 호버 연출 사용 여부를 설정합니다. 덱 조회처럼 그리드 배치에서는 끄는 것이 안전합니다.
        /// </summary>
        public void SetHoverEnabled(bool enabled)
        {
            _hoverEnabled = enabled;
            if (!enabled)
                ResetHoverState();
        }

        /// <summary>
        /// 클릭 사용 가능 여부를 설정합니다. 이동 연출 중에는 끄는 것이 안전합니다.
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            _interactable = interactable;
            SetHoverEnabled(interactable);
        }

        /// <summary>
        /// 이동 연출 전후 스케일·호버 상태를 초기화합니다.
        /// </summary>
        public void ResetTravelVisual()
        {
            ResetHoverState();
            CachedTransform.localScale = Vector3.one;
        }

        public void TryUse()
        {
            if (!_interactable || _runtimeCard == null)
                return;
            DeckSystem.TryPlayCard(_runtimeCard);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_hoverEnabled || _isHovered)
                return;

            _isHovered = true;

            var rect = CachedRectTransform;
            _restAnchoredPosition = rect.anchoredPosition;
            rect.SetAsLastSibling();

            PlayHoverMotion(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_isHovered)
                return;

            _isHovered = false;
            PlayHoverMotion(false);
        }

        private async UniTaskVoid LoadVisualsAsync(RuntimeCard card, int version)
        {
            var frameKey = RuntimeCard.GetFrameKey(card.CardType);
            var iconKey = string.IsNullOrEmpty(card.IconKey)
                ? RuntimeCard.GetDefaultIconKey(card.CardType)
                : card.IconKey;

            if (await TryLoadSpriteAsync(_frameImage, frameKey, _loadedFrameKey, version))
                _loadedFrameKey = frameKey;

            if (await TryLoadSpriteAsync(_iconImage, iconKey, _loadedIconKey, version))
                _loadedIconKey = iconKey;
        }

        private async UniTask<bool> TryLoadSpriteAsync(Image target, string iconKey, string loadedKey, int version)
        {
            if (target == null || string.IsNullOrEmpty(iconKey))
                return false;

            if (loadedKey == iconKey && target.sprite != null)
                return false;

            var address = AddressableKeys.Icons.Get(iconKey);
            if (string.IsNullOrEmpty(address))
            {
                Debug.LogWarning($"[Card] Icon key not registered: {iconKey}");
                return false;
            }

            var sprite = await ResourceManager.LoadResourceAsync<Sprite>(address);
            if (version != _bindVersion || sprite == null)
                return false;

            target.sprite = sprite;
            target.enabled = true;
            target.preserveAspect = true;
            return true;
        }

        private void PlayHoverMotion(bool hover)
        {
            CancelHoverMotion();

            var rect = CachedRectTransform;
            var tr = CachedTransform;
            var targetScale = hover ? Vector3.one * _hoverScale : Vector3.one;
            var targetPos = hover
                ? _restAnchoredPosition + new Vector2(0f, _hoverLiftY)
                : _restAnchoredPosition;

            _scaleHandle = LMotion.Create(tr.localScale, targetScale, _hoverDuration)
                .WithEase(Ease.OutCubic)
                .BindToLocalScale(tr);

            _positionHandle = LMotion.Create(rect.anchoredPosition, targetPos, _hoverDuration)
                .WithEase(Ease.OutCubic)
                .Bind(v => rect.anchoredPosition = v);
        }

        private void CancelHoverMotion()
        {
            if (_scaleHandle.IsActive())
                _scaleHandle.Cancel();

            if (_positionHandle.IsActive())
                _positionHandle.Cancel();
        }

        private void ResetHoverState()
        {
            CancelHoverMotion();

            // 호버 중이 아닐 때 스케일을 강제 복원하면 드로우/버리기 이동 연출 스케일이 깨진다.
            if (!_isHovered)
                return;

            _isHovered = false;
            CachedTransform.localScale = Vector3.one;
        }

        private void OnDisable()
        {
            if (GameStateUtil.IsQuitting)
                return;

            ResetHoverState();
        }
    }
}
