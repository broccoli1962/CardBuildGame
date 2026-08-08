using Backend.AddressableKey;
using Backend.Object.GameSystems.Gameplay;
using Backend.Object.Management;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Backend.Object.UI
{
    /// <summary>
    /// 맵 Event 노드 선택지 UI.
    /// </summary>
    public class EventPanel : UIPanel<EventPanelPresenter>
    {
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _descriptionText;
        [SerializeField] private TextMeshProUGUI _resultText;
        [SerializeField] private CommonButton _optionAButton;
        [SerializeField] private CommonButton _optionBButton;
        [SerializeField] private CommonButton _continueButton;
        [SerializeField] private TextMeshProUGUI _optionALabel;
        [SerializeField] private TextMeshProUGUI _optionBLabel;
        [SerializeField] private TextMeshProUGUI _continueLabel;

        public CommonButton OptionAButton => _optionAButton;
        public CommonButton OptionBButton => _optionBButton;
        public CommonButton ContinueButton => _continueButton;

        /// <summary>
        /// 현재 EventSystem 상태에 맞춰 배경·문구·버튼 가시성을 갱신합니다.
        /// </summary>
        public void BindCurrentEvent()
        {
            var data = EventSystem.EventData;
            if (data == null)
                return;

            ApplyBackgroundAsync(data.event_id).Forget();

            if (_titleText != null)
                _titleText.text = data.name_key.GetLocalizeText();

            if (_descriptionText != null)
                _descriptionText.text = data.desc_key.GetLocalizeText();

            if (_optionALabel != null)
                _optionALabel.text = data.option_a_key.GetLocalizeText();

            if (_optionBLabel != null)
                _optionBLabel.text = data.option_b_key.GetLocalizeText();

            if (_continueLabel != null)
                _continueLabel.text = "event_continue".GetLocalizeText();

            RefreshChoiceVisibility();
        }

        /// <summary>
        /// 주사위 결과 등 대기 상태에 맞춰 버튼/결과 문구를 전환합니다.
        /// </summary>
        public void RefreshChoiceVisibility()
        {
            var awaiting = EventSystem.AwaitingResultContinue;

            if (_optionAButton != null)
                _optionAButton.gameObject.SetActive(!awaiting);

            if (_optionBButton != null)
                _optionBButton.gameObject.SetActive(!awaiting);

            if (_continueButton != null)
                _continueButton.gameObject.SetActive(awaiting);

            if (_descriptionText != null)
                _descriptionText.gameObject.SetActive(!awaiting);

            if (_resultText == null)
                return;

            if (!awaiting)
            {
                _resultText.gameObject.SetActive(false);
                return;
            }

            _resultText.gameObject.SetActive(true);
            var key = EventSystem.DiceSucceeded
                ? "event_dice_gambling_win"
                : "event_dice_gambling_lose";
            _resultText.text = key.GetLocalizeText();
        }

        private async UniTaskVoid ApplyBackgroundAsync(string eventId)
        {
            if (_backgroundImage == null)
                return;

            var iconKey = eventId switch
            {
                EventSystem.EventIdCaveLake => "Bg_Event_CaveLake",
                EventSystem.EventIdDiceGambling => "Bg_Event_DiceGambling",
                EventSystem.EventIdFortuneTeller => "Bg_Event_FortuneTeller",
                _ => "Bg_Event_CaveLake",
            };

            var sprite = await ResourceManager.LoadResourceAsync<Sprite>(AddressableKeys.Icons.Get(iconKey));
            if (sprite == null || _backgroundImage == null)
                return;

            _backgroundImage.sprite = sprite;
            _backgroundImage.preserveAspect = false;
        }
    }
}
