using Backend.Object.GameSystems.Gameplay;
using Backend.Object.GameSystems.Llm;
using Backend.Util.Management;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace Backend.Object.Management
{
    public class GameManager : SingletonGameObject<GameManager>
    {
        private static GamePhase _currentPhase;
        private static readonly Subject<Unit> _onGameOver = new();

        /// <summary>
        /// 플레이어 사망으로 GameOver 페이즈에 진입했을 때 발행됩니다.
        /// </summary>
        public static Observable<Unit> OnGameOver => _onGameOver;

        protected override void OnAwake()
        {
            base.OnAwake();

            Application.targetFrameRate = 60;
        }

        private async UniTask InitializeCore_Internal()
        {
            await AudioManager.InitMixer();
            TableManager.Init();
            LocalLlmManager.EnsureInitialized();
        }

        private void StartGameplay_Internal()
        {
            PlayerStateSystem.Initialize();
            DeckSystem.Initialize();
            BattleSystem.Initialize();
            CardCreationSystem.Initialize();
            EventSystem.Initialize();
            RestSystem.Initialize();
            TreasureSystem.Initialize();
            MapSystem.Initialize();
            MapSystem.StartRun(chapter: 1);
        }

        private void EndGameplay_Internal()
        {
            TreasureSystem.Dispose();
            RestSystem.Dispose();
            EventSystem.Dispose();
            CardCreationSystem.Dispose();
            MapSystem.Dispose();
            BattleSystem.Dispose();
            DeckSystem.Dispose();
        }

        private void GameOver_Internal()
        {
            SetPhase_Internal(GamePhase.GameOver);
            _onGameOver.OnNext(Unit.Default);
        }

        private void StageClear_Internal()
        {
            if (!MapSystem.TryGetCurrentNode(out var node))
            {
                MapSystem.CompleteCurrentNode();
                return;
            }

            if (CardCreationSystem.ShouldOfferCreation(node))
            {
                CardCreationSystem.BeginSession(node);
                return;
            }

            MapSystem.CompleteCurrentNode();
        }

        private void SetPhase_Internal(GamePhase phase)
        {
            _currentPhase = phase;
        }

#region Static Public Methods
        public static void EndGameplay() => Instance?.EndGameplay_Internal();
        public static void GameOver() => Instance.GameOver_Internal();
        public static void StageClear() => Instance.StageClear_Internal();
        public static void StartGameplay() => Instance.StartGameplay_Internal();
        public static void SetPhase(GamePhase phase) => Instance.SetPhase_Internal(phase);

        /// <summary>
        /// 현재 게임 페이즈를 반환합니다.
        /// </summary>
        public static GamePhase CurrentPhase => _currentPhase;

        public static UniTask InitializeCore() => Instance.InitializeCore_Internal();
#endregion
    }
}
