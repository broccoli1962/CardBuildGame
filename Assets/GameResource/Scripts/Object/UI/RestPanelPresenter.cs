using Backend.Object.GameSystems.Gameplay;
using R3;

namespace Backend.Object.UI
{
    public class RestPanelPresenter : UIPresenter<RestPanel>
    {
        private CompositeDisposable _disposables;

        public override void OnOpen()
        {
            _disposables?.Dispose();
            _disposables = new CompositeDisposable();

            if (View == null)
                return;

            View.BindVisuals();

            if (View.HealButton != null)
            {
                View.HealButton.OnClickAsObservable()
                    .Subscribe(_ => RestSystem.ChooseHeal())
                    .AddTo(_disposables);
            }

            if (View.PassButton != null)
            {
                View.PassButton.OnClickAsObservable()
                    .Subscribe(_ => RestSystem.ChoosePass())
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
