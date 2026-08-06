using Backend.Object.GameSystems.Gameplay;
using Backend.Object.Management;
using R3;

namespace Backend.Object.UI
{
    public class MapPreviewPopupPresenter : UIPresenter<MapPreviewPopup>
    {
        private CompositeDisposable _disposables;

        public override void OnOpen()
        {
            _disposables?.Dispose();
            _disposables = new CompositeDisposable();

            if (View == null)
                return;

            View.GraphView?.SetInteractable(false);
            View.GraphView?.Bind(MapSystem.Nodes.CurrentValue, null);

            View.SetHeader($"MAP PREVIEW · CHAPTER {MapSystem.Chapter.CurrentValue}");
            var tier = MapSystem.ThreatTier;
            View.SetStatus(
                $"현재 F{MapSystem.CurrentFloor.CurrentValue}  ·  경로 {MapSystem.PathHistory.Count}  ·  {(tier > 0 ? $"☠ T{tier}" : "T0")}");

            if (View.CloseButton != null)
            {
                View.CloseButton.OnClickAsObservable()
                    .Subscribe(_ => UIManager.Close(View))
                    .AddTo(_disposables);
            }
        }

        public override void OnClose()
        {
            _disposables?.Dispose();
            _disposables = null;
        }
    }
}
