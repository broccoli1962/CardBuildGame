using System;
using System.Threading;
using Backend.AddressableKey;
using Backend.Object.FX;
using Backend.Object.GameSystems.Gameplay;
using Backend.Object.Management;
using Backend.Object.Management.Pool;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace Backend.Object.Controller
{
    /// <summary>
    /// 전투 중 플레이어 공격/방어/회복 연출을 재생합니다.
    /// </summary>
    public class BattleVfxController : MonoBehaviour
    {
        private RectTransform _vfxRoot;
        private RectTransform _attackAnchor;
        private RectTransform _defendAnchor;
        private Pooling<AttackSlashVfx> _attackPool;
        private Pooling<DefendShieldVfx> _defendPool;
        private CompositeDisposable _disposables;
        private CancellationTokenSource _lifetimeCts;

        /// <summary>
        /// VFX 풀을 생성하고 BattleSystem 연출 이벤트를 구독합니다.
        /// </summary>
        public async UniTask InitializeAsync(
            RectTransform vfxRoot,
            RectTransform attackAnchor,
            RectTransform defendAnchor)
        {
            if (vfxRoot == null)
            {
                Debug.LogError("[BattleVfxController] vfxRoot is null.");
                return;
            }

            _vfxRoot = vfxRoot;
            _attackAnchor = attackAnchor != null ? attackAnchor : vfxRoot;
            _defendAnchor = defendAnchor != null ? defendAnchor : vfxRoot;

            _lifetimeCts?.Cancel();
            _lifetimeCts?.Dispose();
            _lifetimeCts = new CancellationTokenSource();

            _attackPool = await ObjectPoolManager.GetOrCreatePoolAsync<AttackSlashVfx>(
                AddressableKeys.UI.Get<AttackSlashVfx>(),
                parent: _vfxRoot,
                defaultCapacity: 2,
                maxSize: 8);

            _defendPool = await ObjectPoolManager.GetOrCreatePoolAsync<DefendShieldVfx>(
                AddressableKeys.UI.Get<DefendShieldVfx>(),
                parent: _vfxRoot,
                defaultCapacity: 2,
                maxSize: 8);

            if (_attackPool == null || _defendPool == null)
            {
                Debug.LogError("[BattleVfxController] Failed to create VFX pools.");
                return;
            }

            _disposables?.Dispose();
            _disposables = new CompositeDisposable();

            BattleSystem.OnPlayerVfx
                .Subscribe(type => PlayVfx(type).Forget())
                .AddTo(_disposables);
        }

        private async UniTaskVoid PlayVfx(BattleVfxType type)
        {
            var token = _lifetimeCts != null ? _lifetimeCts.Token : CancellationToken.None;

            switch (type)
            {
                case BattleVfxType.Attack:
                    AudioManager.PlaySfx("Attack_Swing");
                    await PlayAttackAsync(_attackAnchor, flipHorizontal: false, token);
                    break;
                case BattleVfxType.EnemyAttack:
                    AudioManager.PlaySfx("Attack_Swing", pitch: 0.9f);
                    await PlayAttackAsync(_defendAnchor, flipHorizontal: true, token);
                    break;
                case BattleVfxType.Defend:
                    AudioManager.PlaySfx("Shield_Sound");
                    await PlayDefendAsync(_defendAnchor, token);
                    break;
                case BattleVfxType.EnemyDefend:
                    AudioManager.PlaySfx("Shield_Sound", pitch: 0.95f);
                    await PlayDefendAsync(_attackAnchor, token);
                    break;
                case BattleVfxType.Heal:
                    // 화면 연출 없음. 추후 SFX 훅 지점.
                    break;
            }
        }

        private async UniTask PlayAttackAsync(
            RectTransform anchor,
            bool flipHorizontal,
            CancellationToken token,
            Vector2 offset = default)
        {
            if (_attackPool == null)
                return;

            var vfx = _attackPool.Get();
            if (vfx == null)
                return;

            try
            {
                PlaceAtAnchor(vfx.transform as RectTransform, anchor, offset);
                await vfx.PlayAsync(token, flipHorizontal);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                if (vfx != null)
                    _attackPool.Release(vfx);
            }
        }

        private async UniTask PlayDefendAsync(RectTransform anchor, CancellationToken token)
        {
            if (_defendPool == null)
                return;

            var vfx = _defendPool.Get();
            if (vfx == null)
                return;

            try
            {
                PlaceAtAnchor(vfx.transform as RectTransform, anchor);
                await vfx.PlayAsync(token);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                if (vfx != null)
                    _defendPool.Release(vfx);
            }
        }

        private void PlaceAtAnchor(RectTransform vfxRect, RectTransform anchor, Vector2 offset = default)
        {
            if (vfxRect == null || _vfxRoot == null)
                return;

            vfxRect.SetParent(_vfxRoot, false);
            vfxRect.SetAsLastSibling();
            vfxRect.localScale = Vector3.one;
            vfxRect.localRotation = Quaternion.identity;

            if (anchor == null)
            {
                vfxRect.anchoredPosition = offset;
                return;
            }

            var worldCenter = anchor.TransformPoint(anchor.rect.center);
            var local = (Vector2)_vfxRoot.InverseTransformPoint(worldCenter);
            var rootRect = _vfxRoot.rect;
            var pivotOffset = new Vector2(
                Mathf.Lerp(rootRect.xMin, rootRect.xMax, _vfxRoot.pivot.x),
                Mathf.Lerp(rootRect.yMin, rootRect.yMax, _vfxRoot.pivot.y));

            vfxRect.anchoredPosition = local - pivotOffset + offset;
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

            ObjectPoolManager.ReleasePool<AttackSlashVfx>();
            ObjectPoolManager.ReleasePool<DefendShieldVfx>();
            _attackPool = null;
            _defendPool = null;
        }
    }
}
