using UnityEngine;

namespace Backend.Object.UI
{
    public class GamePanel : UIPanel<GamePanelPresenter>
    {
        [SerializeField] private RectTransform _playerCardsContainer;
        [SerializeField] private RectTransform _monsterCardContainer;
        [SerializeField] private TMPro.TextMeshProUGUI _playerHealthText;
        [SerializeField] private TMPro.TextMeshProUGUI _playerManaText;
        [SerializeField] private CommonButton _endPlayerTurnButton;
        [SerializeField] private CommonButton _openMapPreviewButton;
        [SerializeField] private CommonButton _openDrawPileButton;
        [SerializeField] private CommonButton _openDiscardPileButton;

        public override UILayer Layer => UILayer.HUD;
        public CommonButton EndPlayerTurnButton => _endPlayerTurnButton;
        public CommonButton OpenMapPreviewButton => _openMapPreviewButton;
        public CommonButton OpenDrawPileButton => _openDrawPileButton;
        public CommonButton OpenDiscardPileButton => _openDiscardPileButton;

        /// <summary>
        /// Controller 가 손패를 배치할 컨테이너
        /// </summary>
        public RectTransform PlayerCardsContainer => _playerCardsContainer;

        /// <summary>
        /// Controller 가 몬스터 카드를 배치할 컨테이너
        /// </summary>
        public RectTransform MonsterCardContainer => _monsterCardContainer;

        /// <summary>
        /// HP 표시 텍스트를 갱신합니다.
        /// </summary>
        public void SetHealth(int current, int max)
        {
            if (_playerHealthText != null)
                _playerHealthText.text = $"{current} / {max}";
        }

        /// <summary>
        /// 마나 표시 텍스트를 갱신합니다.
        /// </summary>
        public void SetMana(int current, int max)
        {
            if (_playerManaText != null)
                _playerManaText.text = $"{current} / {max}";
        }
    }
}
