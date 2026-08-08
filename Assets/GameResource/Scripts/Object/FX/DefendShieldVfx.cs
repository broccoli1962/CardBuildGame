using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

namespace Backend.Object.FX
{
    /// <summary>
    /// 방패가 나타나 한 바퀴 돌고 사라지는 방어 연출.
    /// </summary>
    public class DefendShieldVfx : MonoBehaviour
    {
        [SerializeField] private RectTransform _shield;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _duration = 0.7f;

        private CancellationTokenSource _playCts;

        /// <summary>
        /// 방패 스핀 연출을 재생합니다.
        /// </summary>
        public async UniTask PlayAsync(CancellationToken externalToken = default)
        {
            CancelPlay();
            _playCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
            var token = _playCts.Token;

            try
            {
                PrepareVisuals();

                var appear = _duration * 0.25f;
                var spin = _duration * 0.5f;
                var fade = _duration * 0.25f;

                var scaleIn = LMotion.Create(Vector3.one * 0.2f, Vector3.one, appear)
                    .WithEase(Ease.OutBack)
                    .BindToLocalScale(_shield);

                var fadeIn = LMotion.Create(0f, 1f, appear)
                    .WithEase(Ease.OutQuad)
                    .BindToAlpha(_canvasGroup);

                await UniTask.WhenAll(scaleIn.ToUniTask(token), fadeIn.ToUniTask(token));
                token.ThrowIfCancellationRequested();

                // 화면 평면(Z) 스핀이 아니라 Y축 회전으로 좌우 뒤집히는 입체 연출.
                await LMotion.Create(0f, 360f, spin)
                    .WithEase(Ease.InOutSine)
                    .BindToLocalEulerAnglesY(_shield)
                    .ToUniTask(token);

                token.ThrowIfCancellationRequested();

                var scaleOut = LMotion.Create(Vector3.one, Vector3.one * 0.6f, fade)
                    .WithEase(Ease.InQuad)
                    .BindToLocalScale(_shield);

                var fadeOut = LMotion.Create(1f, 0f, fade)
                    .WithEase(Ease.InQuad)
                    .BindToAlpha(_canvasGroup);

                await UniTask.WhenAll(scaleOut.ToUniTask(token), fadeOut.ToUniTask(token));
            }
            finally
            {
                ResetVisuals();
            }
        }

        public void CancelPlay()
        {
            if (_playCts == null)
                return;

            _playCts.Cancel();
            _playCts.Dispose();
            _playCts = null;
        }

        private void PrepareVisuals()
        {
            if (_shield != null)
            {
                _shield.anchoredPosition = Vector2.zero;
                _shield.localEulerAngles = Vector3.zero;
                _shield.localScale = Vector3.one * 0.2f;
            }

            if (_canvasGroup != null)
                _canvasGroup.alpha = 0f;
        }

        private void ResetVisuals()
        {
            PrepareVisuals();
        }

        private void OnDisable()
        {
            CancelPlay();
            ResetVisuals();
        }

        private void OnDestroy()
        {
            CancelPlay();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_shield == null)
            {
                var shield = transform.Find("Shield");
                if (shield != null)
                    _shield = shield as RectTransform;
            }

            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();
        }
#endif
    }
}
