using System.Collections.Generic;
using Backend.AddressableKey;
using Backend.Object.GameSystems.Gameplay;
using Backend.Object.Management;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Backend.Object.UI
{
    public enum DeckInspectTab
    {
        DrawPile = 0,
        DiscardPile = 1,
    }

    /// <summary>
    /// 남은 덱(드로우 파일)과 무덤(버린 더미)을 확인하는 읽기 전용 팝업.
    /// </summary>
    public class DeckInspectPopup : UIPopup<DeckInspectPopupPresenter>
    {
        private static DeckInspectTab _pendingTab = DeckInspectTab.DrawPile;

        [SerializeField] private TextMeshProUGUI _headerText;
        [SerializeField] private TextMeshProUGUI _countText;
        [SerializeField] private TextMeshProUGUI _emptyText;
        [SerializeField] private CommonButton _drawTabButton;
        [SerializeField] private CommonButton _discardTabButton;
        [SerializeField] private CommonButton _closeButton;
        [SerializeField] private RectTransform _contentRoot;

        private readonly List<Card> _spawnedCards = new();
        private Card _cardPrefab;

        public CommonButton DrawTabButton => _drawTabButton;
        public CommonButton DiscardTabButton => _discardTabButton;
        public CommonButton CloseButton => _closeButton;

        public static DeckInspectTab PendingTab
        {
            get => _pendingTab;
            set => _pendingTab = value;
        }

        public void SetHeader(string text)
        {
            if (_headerText != null)
                _headerText.text = text;
        }

        public void SetCount(string text)
        {
            if (_countText != null)
                _countText.text = text;
        }

        public void SetTabVisual(DeckInspectTab tab)
        {
            SetTabActive(_drawTabButton, tab == DeckInspectTab.DrawPile);
            SetTabActive(_discardTabButton, tab == DeckInspectTab.DiscardPile);
        }

        public void ShowCards(IReadOnlyList<RuntimeCard> cards)
        {
            ShowCardsAsync(cards).Forget();
        }

        private async UniTaskVoid ShowCardsAsync(IReadOnlyList<RuntimeCard> cards)
        {
            ClearRows();

            var empty = cards == null || cards.Count == 0;
            if (_emptyText != null)
                _emptyText.gameObject.SetActive(empty);

            if (empty || _contentRoot == null)
                return;

            if (!await EnsureCardPrefabAsync())
                return;

            for (var i = 0; i < cards.Count; i++)
            {
                var runtimeCard = cards[i];
                if (runtimeCard == null)
                    continue;

                var card = Instantiate(_cardPrefab, _contentRoot);
                card.gameObject.SetActive(true);
                card.SetHoverEnabled(false);
                card.Bind(runtimeCard);
                _spawnedCards.Add(card);
            }
        }

        public void ClearRows()
        {
            for (var i = 0; i < _spawnedCards.Count; i++)
            {
                if (_spawnedCards[i] != null)
                    Destroy(_spawnedCards[i].gameObject);
            }

            _spawnedCards.Clear();
        }

        private async UniTask<bool> EnsureCardPrefabAsync()
        {
            if (_cardPrefab != null)
                return true;

            var address = AddressableKeys.UI.Get<Card>();
            if (string.IsNullOrEmpty(address))
            {
                Debug.LogError("[DeckInspectPopup] Card addressable key is missing.");
                return false;
            }

            _cardPrefab = await ResourceManager.LoadComponentAsync<Card>(address);
            if (_cardPrefab == null)
            {
                Debug.LogError("[DeckInspectPopup] Failed to load Card prefab.");
                return false;
            }

            return true;
        }

        private static void SetTabActive(CommonButton button, bool active)
        {
            if (button == null)
                return;

            button.interactable = !active;
            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = active
                    ? new Color(0.79f, 0.64f, 0.15f, 1f)
                    : new Color(0.25f, 0.25f, 0.32f, 1f);
            }
        }
    }
}
