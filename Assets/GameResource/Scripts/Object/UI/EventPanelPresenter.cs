using Backend.Object.GameSystems.Gameplay;
using R3;

namespace Backend.Object.UI
{
    public class EventPanelPresenter : UIPresenter<EventPanel>
    {
        private CompositeDisposable _disposables;

        public override void OnOpen()
        {
            _disposables?.Dispose();
            _disposables = new CompositeDisposable();

            if (View == null)
                return;

            View.BindCurrentEvent();

            EventSystem.OnStateChanged
                .Subscribe(_ => View.RefreshChoiceVisibility())
                .AddTo(_disposables);

            if (View.OptionAButton != null)
            {
                View.OptionAButton.OnClickAsObservable()
                    .Subscribe(_ => EventSystem.ChooseOptionA())
                    .AddTo(_disposables);
            }

            if (View.OptionBButton != null)
            {
                View.OptionBButton.OnClickAsObservable()
                    .Subscribe(_ => EventSystem.ChooseOptionB())
                    .AddTo(_disposables);
            }

            if (View.ContinueButton != null)
            {
                View.ContinueButton.OnClickAsObservable()
                    .Subscribe(_ => EventSystem.ConfirmResultAndLeave())
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
