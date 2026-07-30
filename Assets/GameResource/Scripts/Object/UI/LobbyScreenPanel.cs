using Backend.Object.Management;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Backend.Object.UI
{
    public class LobbyScreenPanel : UIPanel
    {
        private const string GameSceneName = "GameScene";

        [SerializeField] private CommonButton _startButton;

        private CompositeDisposable _disposables;

        protected override void OnOpen()
        {
            base.OnOpen();

            _disposables?.Dispose();
            _disposables = new CompositeDisposable();

            if (_startButton == null)
            {
                Debug.LogWarning("[LobbyScreenPanel] _startButton is not assigned.");
                return;
            }

            _startButton.OnClickAsObservable()
                .Subscribe(_ => StartGame())
                .AddTo(_disposables);
        }

        protected override void OnClose()
        {
            _disposables?.Dispose();
            _disposables = null;
            base.OnClose();
        }

        private void StartGame()
        {
            LoadGameSceneAsync().Forget();
        }

        private async UniTaskVoid LoadGameSceneAsync()
        {
            await UIManager.BlockUI();
            UIManager.CloseAllUI();
            await SceneManager.LoadSceneAsync(GameSceneName).ToUniTask();
        }
    }
}
