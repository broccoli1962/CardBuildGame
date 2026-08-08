using Backend.Object.Management;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine.SceneManagement;

namespace Backend.Object.UI
{
    public class DeathPanelPresenter : UIPresenter<DeathPanel>
    {
        private const string GameSceneName = "GameScene";
        private const string LobbySceneName = "LobbyScene";

        private CompositeDisposable _disposables;
        private bool _isNavigating;

        public override void OnOpen()
        {
            _disposables?.Dispose();
            _disposables = new CompositeDisposable();
            _isNavigating = false;

            if (View == null)
                return;

            View.SetupVisuals();

            if (View.RestartButton != null)
            {
                View.RestartButton.OnClickAsObservable()
                    .Subscribe(_ => RestartRun())
                    .AddTo(_disposables);
            }

            if (View.TitleButton != null)
            {
                View.TitleButton.OnClickAsObservable()
                    .Subscribe(_ => ReturnToTitle())
                    .AddTo(_disposables);
            }
        }

        public override void OnClose()
        {
            _disposables?.Dispose();
            _disposables = null;
        }

        private void RestartRun()
        {
            if (_isNavigating)
                return;

            LoadSceneAsync(GameSceneName).Forget();
        }

        private void ReturnToTitle()
        {
            if (_isNavigating)
                return;

            LoadSceneAsync(LobbySceneName).Forget();
        }

        private async UniTaskVoid LoadSceneAsync(string sceneName)
        {
            _isNavigating = true;

            await UIManager.BlockUI();
            await UIManager.CloseAllUIAsync();
            await SceneManager.LoadSceneAsync(sceneName).ToUniTask();
        }
    }
}
