using Backend.Object.GameSystems.Gameplay;
using R3;

namespace Backend.Object.UI
{
    public class TreasurePanelPresenter : UIPresenter<TreasurePanel>
    {
        private CompositeDisposable _disposables;

        public override void OnOpen()
        {
            _disposables?.Dispose();
            _disposables = new CompositeDisposable();

            if (View == null)
                return;

            View.BindVisuals();

            if (View.LifeButton != null)
            {
                View.LifeButton.OnClickAsObservable()
                    .Subscribe(_ => TreasureSystem.ChooseLife())
                    .AddTo(_disposables);
            }

            if (View.ManaButton != null)
            {
                View.ManaButton.OnClickAsObservable()
                    .Subscribe(_ => TreasureSystem.ChooseMana())
                    .AddTo(_disposables);
            }

            if (View.PassButton != null)
            {
                View.PassButton.OnClickAsObservable()
                    .Subscribe(_ => TreasureSystem.ChoosePass())
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
