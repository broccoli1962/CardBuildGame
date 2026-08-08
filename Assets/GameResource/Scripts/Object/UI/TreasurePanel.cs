using Backend.AddressableKey;
using Backend.Object.Management;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Backend.Object.UI
{
    /// <summary>
    /// 맵 Treasure 노드 — 체력/마나 영구 강화 1택 UI.
    /// </summary>
    public class TreasurePanel : UIPanel<TreasurePanelPresenter>
    {
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _descriptionText;
        [SerializeField] private CommonButton _lifeButton;
        [SerializeField] private CommonButton _manaButton;
        [SerializeField] private CommonButton _passButton;
        [SerializeField] private TextMeshProUGUI _lifeLabel;
        [SerializeField] private TextMeshProUGUI _manaLabel;
        [SerializeField] private TextMeshProUGUI _passLabel;

        public CommonButton LifeButton => _lifeButton;
        public CommonButton ManaButton => _manaButton;
        public CommonButton PassButton => _passButton;

        /// <summary>
        /// 보물 배경과 선택지 문구를 바인딩합니다.
        /// </summary>
        public void BindVisuals()
        {
            ApplyBackgroundAsync().Forget();

            if (_titleText != null)
                _titleText.text = "node_treasure_name".GetLocalizeText();

            if (_descriptionText != null)
                _descriptionText.text = "node_treasure_desc".GetLocalizeText();

            if (_lifeLabel != null)
                _lifeLabel.text = FormatOptionLabel("treasure_life_name", "treasure_life_desc");

            if (_manaLabel != null)
                _manaLabel.text = FormatOptionLabel("treasure_mana_name", "treasure_mana_desc");

            if (_passLabel != null)
                _passLabel.text = "event_pass".GetLocalizeText();
        }

        private static string FormatOptionLabel(string nameKey, string descKey)
        {
            return $"{nameKey.GetLocalizeText()}\n{descKey.GetLocalizeText()}";
        }

        private async UniTaskVoid ApplyBackgroundAsync()
        {
            if (_backgroundImage == null)
                return;

            var sprite = await ResourceManager.LoadResourceAsync<Sprite>(
                AddressableKeys.Icons.Get("Bg_Event_Treasure"));
            if (sprite == null || _backgroundImage == null)
                return;

            _backgroundImage.sprite = sprite;
            _backgroundImage.preserveAspect = false;
        }
    }
}
