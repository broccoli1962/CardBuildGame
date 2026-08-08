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
        private BattleVfxController _battleVfxController;
        private MapPanel _mapPanel;
        private GamePanel _gamePanel;
        private CardCreationPanel _cardCreationPanel;
        private EventPanel _eventPanel;
        private RestPanel _restPanel;
        private TreasurePanel _treasurePanel;
        private DeathPanel _deathPanel;
        private CompositeDisposable _disposables;

        protected override async UniTask OnEnterAsync()
        {
            await Boot.WaitUntilReadyAsync();

            // Scene-embedded UIRoot is editor preview only; disable so it cannot steal raycasts.
            UIManager.DisableActiveSceneUiRoot();

            _disposables?.Dispose();
            _disposables = new CompositeDisposable();

            _gamePanel = await UIManager.OpenAsync<GamePanel>();
            UIManager.UnblockUI();
            if (_gamePanel == null)
            {
                Debug.LogError("[GameSceneContext] Failed to open GamePanel.");
                return;
            }

            if (!await TryInitializeControllerAsync<CardController>(
                    _gamePanel.PlayerCardsContainer,
                    controller => controller.InitializeAsync(
                        _gamePanel.PlayerCardsContainer,
                        _gamePanel.DrawPileAnchor,
                        _gamePanel.DiscardPileAnchor),
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

            if (!await TryInitializeControllerAsync<BattleVfxController>(
                    _gamePanel.BattleVfxRoot,
                    controller => controller.InitializeAsync(
                        _gamePanel.BattleVfxRoot,
                        _gamePanel.AttackVfxAnchor,
                        _gamePanel.DefendVfxAnchor),
                    controller => _battleVfxController = controller))
            {
                return;
            }

            MapSystem.OnNodeEntered.Subscribe(node => HandleNodeEntered(node).Forget()).AddTo(_disposables);
            MapSystem.OnReturnedToMap.Subscribe(_ => HandleReturnedToMap().Forget()).AddTo(_disposables);
            EventSystem.OnEventStarted.Subscribe(_ => OpenEventPanelAsync().Forget()).AddTo(_disposables);
            EventSystem.OnEventEnded.Subscribe(_ => CloseEventPanel()).AddTo(_disposables);
            RestSystem.OnRestStarted.Subscribe(_ => OpenRestPanelAsync().Forget()).AddTo(_disposables);
            RestSystem.OnRestEnded.Subscribe(_ => CloseRestPanel()).AddTo(_disposables);
            TreasureSystem.OnTreasureStarted.Subscribe(_ => OpenTreasurePanelAsync().Forget()).AddTo(_disposables);
            TreasureSystem.OnTreasureEnded.Subscribe(_ => CloseTreasurePanel()).AddTo(_disposables);
            CardCreationSystem.OnSessionStarted.Subscribe(_ => OpenCardCreationPanelAsync().Forget()).AddTo(_disposables);
            CardCreationSystem.OnSessionEnded.Subscribe(_ => CloseCardCreationPanel()).AddTo(_disposables);
            GameManager.OnGameOver.Subscribe(_ => OpenDeathPanelAsync().Forget()).AddTo(_disposables);

            GameManager.StartGameplay();

            if (MapSystem.AwaitingNodeCompletion && MapSystem.TryGetCurrentNode(out var current) &&
                current.NodeType is MapNodeType.Battle or MapNodeType.Elite or MapNodeType.Boss)
            {
                GameBgm.PlayForNode(current);
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
            DestroyController(ref _battleVfxController);
            _mapPanel = null;
            _gamePanel = null;
            _cardCreationPanel = null;
            _eventPanel = null;
            _restPanel = null;
            _treasurePanel = null;
            _deathPanel = null;
        }

        private async UniTask HandleNodeEntered(MapNode node)
        {
            switch (node.NodeType)
            {
                case MapNodeType.Battle:
                case MapNodeType.Elite:
                case MapNodeType.Boss:
                    CloseMapPanel();
                    GameBgm.PlayForNode(node);
                    break;
                case MapNodeType.Event:
                    CloseMapPanel();
                    if (!EventSystem.BeginEvent(node))
                        MapSystem.CompleteNonBattleStub();
                    break;
                case MapNodeType.Rest:
                    CloseMapPanel();
                    if (!RestSystem.BeginRest(node))
                        MapSystem.CompleteNonBattleStub();
                    break;
                case MapNodeType.Treasure:
                    CloseMapPanel();
                    if (!TreasureSystem.BeginTreasure(node))
                        MapSystem.CompleteNonBattleStub();
                    break;
            }

            await UniTask.CompletedTask;
        }

        private async UniTask HandleReturnedToMap()
        {
            CloseEventPanel();
            CloseRestPanel();
            CloseTreasurePanel();
            CloseCardCreationPanel();
            await OpenMapPanelAsync();
        }

        private async UniTask OpenEventPanelAsync()
        {
            CloseMapPanel();

            if (_eventPanel != null)
                return;

            _eventPanel = await UIManager.OpenAsync<EventPanel>();
            if (_eventPanel == null)
            {
                Debug.LogError("[GameSceneContext] Failed to open EventPanel.");
                return;
            }

            GameBgm.PlayForEvent(EventSystem.EventId);
        }

        private void CloseEventPanel()
        {
            if (_eventPanel == null)
                return;

            UIManager.Close(_eventPanel);
            _eventPanel = null;
        }

        private async UniTask OpenRestPanelAsync()
        {
            CloseMapPanel();

            if (_restPanel != null)
                return;

            _restPanel = await UIManager.OpenAsync<RestPanel>();
            if (_restPanel == null)
            {
                Debug.LogError("[GameSceneContext] Failed to open RestPanel.");
                return;
            }

            GameBgm.PlayRest();
        }

        private void CloseRestPanel()
        {
            if (_restPanel == null)
                return;

            UIManager.Close(_restPanel);
            _restPanel = null;
        }

        private async UniTask OpenTreasurePanelAsync()
        {
            CloseMapPanel();

            if (_treasurePanel != null)
                return;

            _treasurePanel = await UIManager.OpenAsync<TreasurePanel>();
            if (_treasurePanel == null)
            {
                Debug.LogError("[GameSceneContext] Failed to open TreasurePanel.");
                return;
            }

            GameBgm.PlayTreasure();
        }

        private void CloseTreasurePanel()
        {
            if (_treasurePanel == null)
                return;

            UIManager.Close(_treasurePanel);
            _treasurePanel = null;
        }

        private async UniTask OpenCardCreationPanelAsync()
        {
            CloseMapPanel();
            CloseEventPanel();
            CloseRestPanel();
            CloseTreasurePanel();

            if (_cardCreationPanel != null)
                return;

            _cardCreationPanel = await UIManager.OpenAsync<CardCreationPanel>();
            if (_cardCreationPanel == null)
            {
                Debug.LogError("[GameSceneContext] Failed to open CardCreationPanel.");
                return;
            }

            GameBgm.PlayCardCreation();
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
            {
                Debug.LogError("[GameSceneContext] Failed to open MapPanel.");
                return;
            }

            GameBgm.PlayMap();
        }

        private void CloseMapPanel()
        {
            if (_mapPanel == null)
                return;

            UIManager.Close(_mapPanel);
            _mapPanel = null;
        }

        private async UniTask OpenDeathPanelAsync()
        {
            if (_deathPanel != null)
                return;

            CloseMapPanel();
            CloseEventPanel();
            CloseRestPanel();
            CloseTreasurePanel();
            CloseCardCreationPanel();

            _deathPanel = await UIManager.OpenAsync<DeathPanel>();
            if (_deathPanel == null)
            {
                Debug.LogError("[GameSceneContext] Failed to open DeathPanel.");
                return;
            }

            GameBgm.PlayDeath();
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
