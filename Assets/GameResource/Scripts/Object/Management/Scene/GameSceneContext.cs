using Backend.AddressableKey;
using Backend.Object.Controller;
using Backend.Object.GameSystems.Gameplay;
using Backend.Object.UI;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace Backend.Object.Management
{
    public class GameSceneContext : SceneContext
    {
        private CardController _cardController;
        private MonsterController _monsterController;
        private MapPanel _mapPanel;
        private GamePanel _gamePanel;
        private CardCreationPanel _cardCreationPanel;
        private CompositeDisposable _disposables;

        protected override async UniTask OnEnterAsync()
        {
            await Boot.WaitUntilReadyAsync();

            _disposables?.Dispose();
            _disposables = new CompositeDisposable();

            _gamePanel = await UIManager.OpenAsync<GamePanel>();
            if (_gamePanel == null)
            {
                Debug.LogError("[GameSceneContext] Failed to open GamePanel.");
                return;
            }

            if (!await TryInitializeControllerAsync<CardController>(
                    _gamePanel.PlayerCardsContainer,
                    controller => controller.InitializeAsync(_gamePanel.PlayerCardsContainer),
                    controller => _cardController = controller))
            {
                return;
            }

            if (!await TryInitializeControllerAsync<MonsterController>(
                    _gamePanel.MonsterCardContainer,
                    controller => controller.InitializeAsync(_gamePanel.MonsterCardContainer),
                    controller => _monsterController = controller))
            {
                return;
            }

            MapSystem.OnNodeEntered.Subscribe(node => HandleNodeEntered(node).Forget()).AddTo(_disposables);
            MapSystem.OnReturnedToMap.Subscribe(_ => HandleReturnedToMap().Forget()).AddTo(_disposables);
            CardCreationSystem.OnSessionStarted.Subscribe(_ => OpenCardCreationPanelAsync().Forget()).AddTo(_disposables);
            CardCreationSystem.OnSessionEnded.Subscribe(_ => CloseCardCreationPanel()).AddTo(_disposables);

            GameManager.StartGameplay();

            if (MapSystem.AwaitingNodeCompletion && MapSystem.TryGetCurrentNode(out var current) &&
                current.NodeType is MapNodeType.Battle or MapNodeType.Elite or MapNodeType.Boss)
            {
                return;
            }

            await OpenMapPanelAsync();
        }

        protected override void OnExit()
        {
            _disposables?.Dispose();
            _disposables = null;

            GameManager.EndGameplay();

            DestroyController(ref _cardController);
            DestroyController(ref _monsterController);
            _mapPanel = null;
            _gamePanel = null;
            _cardCreationPanel = null;
        }

        private async UniTask HandleNodeEntered(MapNode node)
        {
            switch (node.NodeType)
            {
                case MapNodeType.Battle:
                case MapNodeType.Elite:
                case MapNodeType.Boss:
                    CloseMapPanel();
                    break;
                case MapNodeType.Rest:
                case MapNodeType.Event:
                case MapNodeType.Treasure:
                    MapSystem.CompleteNonBattleStub();
                    break;
            }

            await UniTask.CompletedTask;
        }

        private async UniTask HandleReturnedToMap()
        {
            CloseCardCreationPanel();
            await OpenMapPanelAsync();
        }

        private async UniTask OpenCardCreationPanelAsync()
        {
            CloseMapPanel();

            if (_cardCreationPanel != null)
                return;

            _cardCreationPanel = await UIManager.OpenAsync<CardCreationPanel>();
            if (_cardCreationPanel == null)
                Debug.LogError("[GameSceneContext] Failed to open CardCreationPanel.");
        }

        private void CloseCardCreationPanel()
        {
            if (_cardCreationPanel == null)
                return;

            UIManager.Close(_cardCreationPanel);
            _cardCreationPanel = null;
        }

        private async UniTask OpenMapPanelAsync()
        {
            if (_mapPanel != null)
                return;

            _mapPanel = await UIManager.OpenAsync<MapPanel>();
            if (_mapPanel == null)
                Debug.LogError("[GameSceneContext] Failed to open MapPanel.");
        }

        private void CloseMapPanel()
        {
            if (_mapPanel == null)
                return;

            UIManager.Close(_mapPanel);
            _mapPanel = null;
        }

        private static async UniTask<bool> TryInitializeControllerAsync<TController>(
            RectTransform container,
            System.Func<TController, UniTask> initializeAsync,
            System.Action<TController> assignController)
            where TController : Component
        {
            if (container == null)
            {
                Debug.LogError($"[GameSceneContext] Container for {typeof(TController).Name} is null.");
                return false;
            }

            var address = AddressableKeys.InGame.Get<TController>();
            var prefab = await ResourceManager.LoadResourceAsync<GameObject>(address);
            if (prefab == null)
            {
                Debug.LogError($"[GameSceneContext] Failed to load {typeof(TController).Name} prefab.");
                return false;
            }

            var instance = UnityEngine.Object.Instantiate(prefab);
            if (!instance.TryGetComponent(out TController controller))
            {
                Debug.LogError($"[GameSceneContext] {typeof(TController).Name} component not found on prefab instance.");
                UnityEngine.Object.Destroy(instance);
                return false;
            }

            assignController(controller);
            await initializeAsync(controller);
            return true;
        }

        private static void DestroyController<TController>(ref TController controller)
            where TController : Component
        {
            if (controller == null)
                return;

            UnityEngine.Object.Destroy(controller.gameObject);
            controller = null;
        }
    }
}
