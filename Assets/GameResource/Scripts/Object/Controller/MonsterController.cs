using System;
using System.Threading;
using Backend.AddressableKey;
using Backend.Object.GameSystems.Gameplay;
using Backend.Object.Management;
using Backend.Object.Management.Pool;
using Backend.Object.UI;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace Backend.Object.Controller
{
    public class MonsterController : MonoBehaviour
    {
        private RectTransform _container;
        private Pooling<Monster> _pool;
        private Monster _activeMonsterCard;
        private CompositeDisposable _disposables;
        private CancellationTokenSource _lifetimeCts;
        private bool _isPlayingDeath;

        /// <summary>
        /// 몬스터 카드 풀을 생성하고 전투 상태 변경 구독을 시작합니다.
        /// </summary>
        public async UniTask InitializeAsync(RectTransform container)
        {
            if (container == null)
            {
                Debug.LogError("[MonsterController] container is null.");
                return;
            }

            _container = container;

            _lifetimeCts?.Cancel();
            _lifetimeCts?.Dispose();
            _lifetimeCts = new CancellationTokenSource();

            _pool = await ObjectPoolManager.GetOrCreatePoolAsync<Monster>(
                AddressableKeys.UI.Get<Monster>(),
                parent: container);

            if (_pool == null)
            {
                Debug.LogError("[MonsterController] Failed to create monster card pool.");
                return;
            }

            _disposables?.Dispose();
            _disposables = new CompositeDisposable();

            BattleSystem.EnemyNameKey
                .Subscribe(_ => RefreshMonster())
                .AddTo(_disposables);

            BattleSystem.EnemyHp
                .Subscribe(_ => RefreshStats())
                .AddTo(_disposables);

            BattleSystem.EnemyMaxHp
                .Subscribe(_ => RefreshStats())
                .AddTo(_disposables);

            BattleSystem.EnemyShield
                .Subscribe(_ => RefreshStats())
                .AddTo(_disposables);

            BattleSystem.NextAction
                .Subscribe(_ => RefreshStats())
                .AddTo(_disposables);

            BattleSystem.OnEnemyDeath
                .Subscribe(_ => PlayDeathPresentationAsync().Forget())
                .AddTo(_disposables);

            RefreshMonster();
        }

        private void RefreshMonster()
        {
            if (_pool == null || _isPlayingDeath)
                return;

            ReleaseActiveMonster();

            if (!BattleSystem.IsBattleActive || string.IsNullOrEmpty(BattleSystem.MonsterId))
                return;

            var monster = TableManager.GetMonster(BattleSystem.MonsterId);
            if (monster == null)
            {
                Debug.LogError($"[MonsterController] Monster not found: {BattleSystem.MonsterId}");
                return;
            }

            _activeMonsterCard = _pool.Get();
            if (_activeMonsterCard == null)
                return;

            _activeMonsterCard.Bind(monster);
            _activeMonsterCard.CachedTransform.SetParent(_container, false);
            _activeMonsterCard.CachedRectTransform.anchoredPosition = Vector2.zero;

            RefreshStats();
        }

        private void RefreshStats()
        {
            if (_activeMonsterCard == null || _isPlayingDeath)
                return;

            _activeMonsterCard.SetStats(
                BattleSystem.EnemyHp.CurrentValue,
                BattleSystem.EnemyMaxHp.CurrentValue,
                BattleSystem.EnemyShield.CurrentValue,
                BattleSystem.NextAction.CurrentValue);
        }

        private async UniTaskVoid PlayDeathPresentationAsync()
        {
            if (_isPlayingDeath)
            {
                BattleSystem.CompleteEnemyDeathPresentation();
                return;
            }

            _isPlayingDeath = true;
            var token = _lifetimeCts != null ? _lifetimeCts.Token : CancellationToken.None;

            try
            {
                if (_activeMonsterCard != null)
                    await _activeMonsterCard.PlayDeathFadeAsync(token);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                ReleaseActiveMonster();
                _isPlayingDeath = false;
                BattleSystem.CompleteEnemyDeathPresentation();
            }
        }

        private void ReleaseActiveMonster()
        {
            if (_pool == null || _activeMonsterCard == null)
                return;

            _activeMonsterCard.ResetVisualState();
            _pool.Release(_activeMonsterCard);
            _activeMonsterCard = null;
        }

        private void OnDestroy()
        {
            if (GameStateUtil.IsQuitting)
                return;

            _lifetimeCts?.Cancel();
            _lifetimeCts?.Dispose();
            _lifetimeCts = null;

            _disposables?.Dispose();
            _disposables = null;

            ReleaseActiveMonster();
            ObjectPoolManager.ReleasePool<Monster>();
            BattleSystem.CompleteEnemyDeathPresentation();
        }
    }
}
