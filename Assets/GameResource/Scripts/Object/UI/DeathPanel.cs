using Backend.AddressableKey;
using Backend.Object.Management;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Backend.Object.UI
{
    /// <summary>
    /// 플레이어 사망 시 재시작/타이틀 복귀를 묻는 패널.
    /// </summary>
    public class DeathPanel : UIPanel<DeathPanelPresenter>
    {
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private CommonButton _restartButton;
        [SerializeField] private CommonButton _titleButton;

        public CommonButton RestartButton => _restartButton;
        public CommonButton TitleButton => _titleButton;

        /// <summary>
        /// 배경·문구를 로드하고 표시합니다.
        /// </summary>
        public void SetupVisuals()
        {
            if (_backgroundImage != null)
            {
                var sprite = ResourceManager.LoadResource<Sprite>(
                    AddressableKeys.Icons.Get("Bg_Death_FallenAdventurer"));
                if (sprite != null)
                {
                    _backgroundImage.sprite = sprite;
                    _backgroundImage.preserveAspect = false;
                }
            }

            if (_titleText != null)
                _titleText.text = "사망";

            if (_messageText != null)
                _messageText.text = "모험이 끝났습니다.\n다시 도전하시겠습니까?";
        }
    }
}
