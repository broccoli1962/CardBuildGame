using Backend.AddressableKey;
using Backend.Object.Management;
using UnityEngine;
using UnityEngine.UI;

namespace Backend.Object.UI
{
    public class GamePanel : UIPanel<GamePanelPresenter>
    {
        [SerializeField] private RectTransform _playerCardsContainer;
        [SerializeField] private RectTransform _monsterCardContainer;
        [SerializeField] private RectTransform _battleVfxRoot;
        [SerializeField] private RectTransform _defendVfxAnchor;
        [SerializeField] private Image _playerHealthIcon;
        [SerializeField] private TMPro.TextMeshProUGUI _playerHealthText;
        [SerializeField] private Image _playerManaIcon;
        [SerializeField] private TMPro.TextMeshProUGUI _playerManaText;
        [SerializeField] private Image _playerShieldIcon;
        [SerializeField] private TMPro.TextMeshProUGUI _playerShieldText;
        [SerializeField] private CommonButton _endPlayerTurnButton;
        [SerializeField] private CommonButton _openMapPreviewButton;
        [SerializeField] private CommonButton _openDrawPileButton;
        [SerializeField] private CommonButton _openDiscardPileButton;
        [SerializeField] private CommonButton _openSoundSettingsButton;

        public override UILayer Layer => UILayer.HUD;
        public CommonButton EndPlayerTurnButton => _endPlayerTurnButton;
        public CommonButton OpenMapPreviewButton => _openMapPreviewButton;
        public CommonButton OpenDrawPileButton => _openDrawPileButton;
        public CommonButton OpenDiscardPileButton => _openDiscardPileButton;
        public CommonButton OpenSoundSettingsButton => _openSoundSettingsButton;

        /// <summary>
        /// Controller 가 손패를 배치할 컨테이너
        /// </summary>
        public RectTransform PlayerCardsContainer => _playerCardsContainer;

        /// <summary>
        /// Controller 가 몬스터 카드를 배치할 컨테이너
        /// </summary>
        public RectTransform MonsterCardContainer => _monsterCardContainer;

        /// <summary>
        /// 전투 VFX 루트. 미지정 시 패널 RectTransform을 사용합니다.
        /// </summary>
        public RectTransform BattleVfxRoot =>
            _battleVfxRoot != null ? _battleVfxRoot : CachedRectTransform;

        /// <summary>
        /// 공격 연출 앵커(몬스터 위치)
        /// </summary>
        public RectTransform AttackVfxAnchor => _monsterCardContainer;

        /// <summary>
        /// 방어 연출 앵커. 기본은 화면 중앙(BattleVfxRoot).
        /// </summary>
        public RectTransform DefendVfxAnchor =>
            _defendVfxAnchor != null ? _defendVfxAnchor : BattleVfxRoot;

        /// <summary>
        /// 드로우 연출 목표(남은 덱 버튼)
        /// </summary>
        public RectTransform DrawPileAnchor =>
            _openDrawPileButton != null ? _openDrawPileButton.CachedRectTransform : null;

        /// <summary>
        /// 버리기 연출 목표(무덤 버튼)
        /// </summary>
        public RectTransform DiscardPileAnchor =>
            _openDiscardPileButton != null ? _openDiscardPileButton.CachedRectTransform : null;

        /// <summary>
        /// PlayerInfo 고정 아이콘을 로드합니다.
        /// </summary>
        public void LoadStatIcons()
        {
            if (_playerHealthIcon != null)
            {
                _playerHealthIcon.sprite = ResourceManager.LoadResource<Sprite>(AddressableKeys.Icons.Get("Icon_Heart"));
                _playerHealthIcon.preserveAspect = true;
            }

            if (_playerManaIcon != null)
            {
                _playerManaIcon.sprite = ResourceManager.LoadResource<Sprite>(AddressableKeys.Icons.Get("Icon_Mana"));
                _playerManaIcon.preserveAspect = true;
            }

            if (_playerShieldIcon != null)
            {
                _playerShieldIcon.sprite = ResourceManager.LoadResource<Sprite>(AddressableKeys.Icons.Get("Icon_Shield"));
                _playerShieldIcon.preserveAspect = true;
            }
        }

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

        /// <summary>
        /// 방어도 표시 텍스트를 갱신합니다.
        /// </summary>
        public void SetShield(int shield)
        {
            if (_playerShieldText != null)
                _playerShieldText.text = shield.ToString();
        }
    }
}
