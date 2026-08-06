using TMPro;
using UnityEngine;

namespace Backend.Object.UI
{
    /// <summary>
    /// 맵 선택 메인 패널 (인터랙티브).
    /// </summary>
    public class MapPanel : UIPanel<MapPanelPresenter>
    {
        [SerializeField] private MapGraphView _graphView;
        [SerializeField] private TextMeshProUGUI _headerText;
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private TextMeshProUGUI _detailText;
        [SerializeField] private CommonButton _enterButton;
        [SerializeField] private CommonButton _clearSelectionButton;
        [SerializeField] private GameObject _detailRoot;

        public MapGraphView GraphView => _graphView;
        public CommonButton EnterButton => _enterButton;
        public CommonButton ClearSelectionButton => _clearSelectionButton;

        public void SetHeader(string text)
        {
            if (_headerText != null)
                _headerText.text = text;
        }

        public void SetStatus(string text)
        {
            if (_statusText != null)
                _statusText.text = text;
        }

        public void SetDetail(string text, bool visible)
        {
            if (_detailRoot != null)
                _detailRoot.SetActive(visible);

            if (_detailText != null)
                _detailText.text = text;
        }

        public void SetEnterInteractable(bool interactable, string label)
        {
            if (_enterButton == null)
                return;

            _enterButton.interactable = interactable;
            var labelText = _enterButton.GetComponentInChildren<TextMeshProUGUI>();
            if (labelText != null && !string.IsNullOrEmpty(label))
                labelText.text = label;
        }
    }
}
