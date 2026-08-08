using Backend.Object.Management;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine.SceneManagement;

namespace Backend.Object.UI
{
    public class LobbyScreenPanelPresenter : UIPresenter<LobbyScreenPanel>
    {
        private const string GameSceneName = "GameScene";

        private CompositeDisposable _disposables;

        public override void OnOpen()
        {
            _disposables?.Dispose();
            _disposables = new CompositeDisposable();

            if (View == null)
                return;

            View.StartStartButtonPulse();

            if (View.OpenSoundSettingsButton != null)
            {
                View.OpenSoundSettingsButton.OnClickAsObservable()
                    .Subscribe(_ => UIManager.OpenAsync<SoundSettingsPopup>().Forget())
                    .AddTo(_disposables);
            }

            if (View.StartButton == null)
                return;

            View.StartButton.OnClickAsObservable()
                .Subscribe(_ => StartGame())
                .AddTo(_disposables);
        }

        public override void OnClose()
        {
            View?.StopStartButtonPulse();

            _disposables?.Dispose();
            _disposables = null;
        }

        private void StartGame()
        {
            LoadGameSceneAsync().Forget();
        }

        private async UniTaskVoid LoadGameSceneAsync()
        {
            View?.StopStartButtonPulse();

            await UIManager.BlockUI();
            await UIManager.CloseAllUIAsync();
            await SceneManager.LoadSceneAsync(GameSceneName).ToUniTask();
        }
    }
}
