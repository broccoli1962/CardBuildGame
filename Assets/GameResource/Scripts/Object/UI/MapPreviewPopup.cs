using TMPro;
using UnityEngine;

namespace Backend.Object.UI
{
    /// <summary>
    /// 전투/노드 진행 중 맵 진행도를 확인하는 읽기 전용 팝업.
    /// </summary>
    public class MapPreviewPopup : UIPopup<MapPreviewPopupPresenter>
    {
        [SerializeField] private MapGraphView _graphView;
        [SerializeField] private TextMeshProUGUI _headerText;
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private CommonButton _closeButton;

        public MapGraphView GraphView => _graphView;
        public CommonButton CloseButton => _closeButton;

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
    }
}
