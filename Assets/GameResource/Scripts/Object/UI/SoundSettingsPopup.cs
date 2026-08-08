using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Backend.Object.UI
{
    /// <summary>
    /// BGM/SFX 볼륨을 게이지(슬라이더)로 조절하는 설정 팝업.
    /// </summary>
    public class SoundSettingsPopup : UIPopup<SoundSettingsPopupPresenter>
    {
        [SerializeField] private TextMeshProUGUI _headerText;
        [SerializeField] private TextMeshProUGUI _bgmLabelText;
        [SerializeField] private TextMeshProUGUI _sfxLabelText;
        [SerializeField] private Slider _bgmSlider;
        [SerializeField] private Slider _sfxSlider;
        [SerializeField] private CommonButton _closeButton;

        public Slider BgmSlider => _bgmSlider;
        public Slider SfxSlider => _sfxSlider;
        public CommonButton CloseButton => _closeButton;

        public void SetHeader(string text)
        {
            if (_headerText != null)
                _headerText.text = text;
        }

        public void SetBgmLabel(string text)
        {
            if (_bgmLabelText != null)
                _bgmLabelText.text = text;
        }

        public void SetSfxLabel(string text)
        {
            if (_sfxLabelText != null)
                _sfxLabelText.text = text;
        }

        public void SetBgmVolume(float value)
        {
            if (_bgmSlider != null)
                _bgmSlider.SetValueWithoutNotify(Mathf.Clamp01(value));
        }

        public void SetSfxVolume(float value)
        {
            if (_sfxSlider != null)
                _sfxSlider.SetValueWithoutNotify(Mathf.Clamp01(value));
        }
    }
}
