using Backend.Object.UI;
using Cysharp.Threading.Tasks;

namespace Backend.Object.Management
{
    public class LobbySceneContext : SceneContext
    {
        protected override async UniTask OnEnterAsync()
        {
            // Scene-embedded UIRoot/LobbyScreenPanel is for editor preview only.
            // After returning from GameScene it loads on top of the DDOL UIManager root
            // and steals clicks with no Presenter OnOpen bindings.
            UIManager.DisableActiveSceneUiRoot();

            var loading = await UIManager.OpenAsync<LoadingPanel>();

            await loading.AnimateProgressAsync(0f, 0.3f, duration: 0.5f);

            await Boot.WaitUntilReadyAsync();

            await loading.AnimateProgressAsync(0.3f, 1f, duration: 0.5f);

            GameBgm.PlayLobby();
            await UIManager.OpenAsync<LobbyScreenPanel>();
            UIManager.Close(loading);
            UIManager.UnblockUI();
        }
    }
}
