using Backend.AddressableKey;
using Backend.Object.Management;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Backend.Object.UI
{
    /// <summary>
    /// 맵 Rest(야영) 노드 UI.
    /// </summary>
    public class RestPanel : UIPanel<RestPanelPresenter>
    {
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _descriptionText;
        [SerializeField] private CommonButton _healButton;
        [SerializeField] private CommonButton _passButton;
        [SerializeField] private TextMeshProUGUI _healLabel;
        [SerializeField] private TextMeshProUGUI _passLabel;

        public CommonButton HealButton => _healButton;
        public CommonButton PassButton => _passButton;

        /// <summary>
        /// 야영 배경과 문구를 바인딩합니다.
        /// </summary>
        public void BindVisuals()
        {
            ApplyBackground();

            if (_titleText != null)
                _titleText.text = "node_rest_name".GetLocalizeText();

            if (_descriptionText != null)
                _descriptionText.text = "rest_heal_desc".GetLocalizeText();

            if (_healLabel != null)
                _healLabel.text = "rest_heal_name".GetLocalizeText();

            if (_passLabel != null)
                _passLabel.text = "event_pass".GetLocalizeText();
        }

        private void ApplyBackground()
        {
            if (_backgroundImage == null)
                return;

            var sprite = ResourceManager.LoadResource<Sprite>(
                AddressableKeys.Icons.Get("Bg_Event_Camping"));
            if (sprite == null)
                return;

            _backgroundImage.sprite = sprite;
            _backgroundImage.preserveAspect = false;
        }
    }
}
