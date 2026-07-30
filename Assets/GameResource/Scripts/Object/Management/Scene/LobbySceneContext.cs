using Backend.Object.UI;
using Cysharp.Threading.Tasks;
using R3;

namespace Backend.Object.Management
{
    public class LobbySceneContext : SceneContext
    {
        protected override async UniTask OnEnterAsync()
        {
            var loading = await UIManager.OpenAsync<LoadingPanel>();

            await loading.AnimateProgressAsync(0f, 0.3f, duration: 0.5f);

            await Boot.WaitUntilReadyAsync();

            await loading.AnimateProgressAsync(0.3f, 1f, duration: 0.5f);

            await UIManager.OpenAsync<LobbyScreenPanel>();
            UIManager.Close(loading);
        }
    }
}