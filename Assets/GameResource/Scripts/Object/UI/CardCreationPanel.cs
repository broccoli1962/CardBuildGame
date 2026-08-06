using TMPro;
using UnityEngine;

namespace Backend.Object.UI
{
    /// <summary>
    /// AI 카드 생성 패널.
    /// </summary>
    public class CardCreationPanel : UIPanel<CardCreationPanelPresenter>
    {
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _subtitleText;
        [SerializeField] private TMP_InputField _conceptInput;
        [SerializeField] private CommonButton _generateButton;
        [SerializeField] private CommonButton _skipButton;
        [SerializeField] private TextMeshProUGUI _loadingText;
        [SerializeField] private GameObject _resultRoot;
        [SerializeField] private TextMeshProUGUI _resultNameText;
        [SerializeField] private TextMeshProUGUI _resultDescText;
        [SerializeField] private TextMeshProUGUI _resultMetaText;
        [SerializeField] private TextMeshProUGUI _threatPreviewText;
        [SerializeField] private CommonButton _addButton;
        [SerializeField] private CommonButton _discardButton;
        [SerializeField] private GameObject _draftRoot;
        [SerializeField] private CommonButton _candidateAButton;
        [SerializeField] private CommonButton _candidateBButton;
        [SerializeField] private TextMeshProUGUI _candidateALabel;
        [SerializeField] private TextMeshProUGUI _candidateBLabel;

        public TMP_InputField ConceptInput => _conceptInput;
        public CommonButton GenerateButton => _generateButton;
        public CommonButton SkipButton => _skipButton;
        public CommonButton AddButton => _addButton;
        public CommonButton DiscardButton => _discardButton;
        public CommonButton CandidateAButton => _candidateAButton;
        public CommonButton CandidateBButton => _candidateBButton;

        public void SetHeader(string title, string subtitle)
        {
            if (_titleText != null)
                _titleText.text = title;
            if (_subtitleText != null)
                _subtitleText.text = subtitle;
        }

        public void SetInputInteractable(bool interactable)
        {
            if (_conceptInput != null)
                _conceptInput.interactable = interactable;
            if (_generateButton != null)
                _generateButton.interactable = interactable;
        }

        public void SetLoading(bool visible, string text = null)
        {
            if (_loadingText == null)
                return;

            _loadingText.gameObject.SetActive(visible);
            if (visible && text != null)
                _loadingText.text = text;
        }

        public void SetResultVisible(bool visible)
        {
            if (_resultRoot != null)
                _resultRoot.SetActive(visible);
        }

        public void SetDraftVisible(bool visible)
        {
            if (_draftRoot != null)
                _draftRoot.SetActive(visible);
        }

        public void SetResult(
            string name,
            string description,
            string meta,
            string threatPreview)
        {
            if (_resultNameText != null)
                _resultNameText.text = name;
            if (_resultDescText != null)
            {
                _resultDescText.richText = true;
                _resultDescText.text = description;
            }
            if (_resultMetaText != null)
                _resultMetaText.text = meta;
            if (_threatPreviewText != null)
                _threatPreviewText.text = threatPreview;
        }

        public void SetCandidateLabels(string a, string b)
        {
            if (_candidateALabel != null)
                _candidateALabel.text = a;
            if (_candidateBLabel != null)
                _candidateBLabel.text = b;
        }

        public void SetSkipVisible(bool visible)
        {
            if (_skipButton != null)
                _skipButton.gameObject.SetActive(visible);
        }
    }
}
