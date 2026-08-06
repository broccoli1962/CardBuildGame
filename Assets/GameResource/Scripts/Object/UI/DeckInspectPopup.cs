using System.Collections.Generic;
using Backend.Object.GameSystems.Gameplay;
using Backend.Object.GameSystems.Llm;
using Backend.Util;
using TMPro;
using UnityEngine;

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
        [SerializeField] private GameObject _rowPrefab;

        private readonly List<GameObject> _spawnedRows = new();

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
            ClearRows();

            var empty = cards == null || cards.Count == 0;
            if (_emptyText != null)
                _emptyText.gameObject.SetActive(empty);

            if (empty || _contentRoot == null || _rowPrefab == null)
                return;

            for (var i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                if (card == null)
                    continue;

                var row = Instantiate(_rowPrefab, _contentRoot);
                row.SetActive(true);
                BindRow(row, card);
                _spawnedRows.Add(row);
            }
        }

        public void ClearRows()
        {
            for (var i = 0; i < _spawnedRows.Count; i++)
            {
                if (_spawnedRows[i] != null)
                    Destroy(_spawnedRows[i]);
            }

            _spawnedRows.Clear();
        }

        private static void SetTabActive(CommonButton button, bool active)
        {
            if (button == null)
                return;

            button.interactable = !active;
            var image = button.GetComponent<UnityEngine.UI.Image>();
            if (image != null)
            {
                image.color = active
                    ? new Color(0.79f, 0.64f, 0.15f, 1f)
                    : new Color(0.25f, 0.25f, 0.32f, 1f);
            }
        }

        private static void BindRow(GameObject row, RuntimeCard card)
        {
            var nameText = FindText(row.transform, "Name");
            var costText = FindText(row.transform, "Cost");
            var metaText = FindText(row.transform, "Meta");
            var descText = FindText(row.transform, "Description");

            var displayName = card.IsGenerated
                ? card.DisplayName
                : card.NameKey.GetLocalizeText();
            var description = card.IsGenerated
                ? CardDescriptionFormatter.ColorizeDamageFormKeywords(card.DisplayDescription)
                : card.DescKey.GetLocalizeText();

            if (nameText != null)
                nameText.text = displayName;
            if (costText != null)
                costText.text = card.ManaCost.ToString();
            if (metaText != null)
                metaText.text = card.CardType.ToString();
            if (descText != null)
            {
                descText.richText = true;
                descText.text = description;
            }
        }

        private static TextMeshProUGUI FindText(Transform root, string childName)
        {
            var child = root.Find(childName);
            return child != null ? child.GetComponent<TextMeshProUGUI>() : null;
        }
    }
}
