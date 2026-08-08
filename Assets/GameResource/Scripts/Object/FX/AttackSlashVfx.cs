using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

namespace Backend.Object.FX
{
    /// <summary>
    /// 검이 나타나 몬스터를 베는 공격 연출.
    /// </summary>
    public class AttackSlashVfx : MonoBehaviour
    {
        [SerializeField] private RectTransform _sword;
        [SerializeField] private CanvasGroup _swordGroup;
        [SerializeField] private RectTransform _slashArc;
        [SerializeField] private CanvasGroup _slashGroup;
        [SerializeField] private float _duration = 0.45f;
        [SerializeField] private Vector2 _startOffset = new(-120f, 90f);
        [SerializeField] private Vector2 _endOffset = new(120f, -90f);
        [SerializeField] private float _startAngle = 40f;
        [SerializeField] private float _endAngle = -50f;

        private CancellationTokenSource _playCts;

        /// <summary>
        /// 앵커 중앙에서 베기 연출을 재생합니다.
        /// </summary>
        /// <param name="flipHorizontal">true면 X축을 뒤집어 반대 방향 베기를 재생합니다.</param>
        public async UniTask PlayAsync(CancellationToken externalToken = default, bool flipHorizontal = false)
        {
            CancelPlay();
            _playCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
            var token = _playCts.Token;

            var startOffset = flipHorizontal
                ? new Vector2(-_startOffset.x, _startOffset.y)
                : _startOffset;
            var endOffset = flipHorizontal
                ? new Vector2(-_endOffset.x, _endOffset.y)
                : _endOffset;
            var startAngle = flipHorizontal ? -_startAngle : _startAngle;
            var endAngle = flipHorizontal ? -_endAngle : _endAngle;

            try
            {
                PrepareVisuals(startOffset, startAngle);

                var appear = _duration * 0.2f;
                var slash = _duration * 0.45f;
                var fade = _duration * 0.35f;

                var swordScale = LMotion.Create(Vector3.one * 0.4f, Vector3.one, appear)
                    .WithEase(Ease.OutBack)
                    .BindToLocalScale(_sword);

                var swordFadeIn = LMotion.Create(0f, 1f, appear)
                    .WithEase(Ease.OutQuad)
                    .BindToAlpha(_swordGroup);

                await UniTask.WhenAll(swordScale.ToUniTask(token), swordFadeIn.ToUniTask(token));
                token.ThrowIfCancellationRequested();

                if (_slashGroup != null)
                    _slashGroup.alpha = 1f;
                if (_slashArc != null)
                {
                    _slashArc.localScale = Vector3.one * 0.6f;
                    _slashArc.localEulerAngles = new Vector3(0f, 0f, flipHorizontal ? 25f : -25f);
                }

                var slashTasks = new System.Collections.Generic.List<UniTask>(4)
                {
                    LMotion.Create(startOffset, endOffset, slash)
                        .WithEase(Ease.InCubic)
                        .BindToAnchoredPosition(_sword)
                        .ToUniTask(token),
                    LMotion.Create(startAngle, endAngle, slash)
                        .WithEase(Ease.InCubic)
                        .BindToLocalEulerAnglesZ(_sword)
                        .ToUniTask(token),
                };

                if (_slashArc != null)
                {
                    slashTasks.Add(LMotion.Create(Vector3.one * 0.6f, Vector3.one * 1.2f, slash)
                        .WithEase(Ease.OutCubic)
                        .BindToLocalScale(_slashArc)
                        .ToUniTask(token));
                }

                if (_slashGroup != null)
                {
                    slashTasks.Add(LMotion.Create(1f, 0f, slash)
                        .WithEase(Ease.InQuad)
                        .BindToAlpha(_slashGroup)
                        .ToUniTask(token));
                }

                await UniTask.WhenAll(slashTasks);

                token.ThrowIfCancellationRequested();

                var swordFadeOut = LMotion.Create(1f, 0f, fade)
                    .WithEase(Ease.InQuad)
                    .BindToAlpha(_swordGroup);

                var swordShrink = LMotion.Create(Vector3.one, Vector3.one * 0.7f, fade)
                    .WithEase(Ease.InQuad)
                    .BindToLocalScale(_sword);

                await UniTask.WhenAll(swordFadeOut.ToUniTask(token), swordShrink.ToUniTask(token));
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
            PrepareVisuals(_startOffset, _startAngle);
        }

        private void PrepareVisuals(Vector2 startOffset, float startAngle)
        {
            if (_sword != null)
            {
                _sword.anchoredPosition = startOffset;
                _sword.localEulerAngles = new Vector3(0f, 0f, startAngle);
                _sword.localScale = Vector3.one * 0.4f;
            }

            if (_swordGroup != null)
                _swordGroup.alpha = 0f;

            if (_slashArc != null)
            {
                _slashArc.anchoredPosition = Vector2.zero;
                _slashArc.localEulerAngles = new Vector3(0f, 0f, -25f);
                _slashArc.localScale = Vector3.one * 0.6f;
            }

            if (_slashGroup != null)
                _slashGroup.alpha = 0f;
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
            if (_sword == null)
            {
                var sword = transform.Find("Sword");
                if (sword != null)
                {
                    _sword = sword as RectTransform;
                    _swordGroup = sword.GetComponent<CanvasGroup>();
                }
            }

            if (_slashArc == null)
            {
                var slash = transform.Find("SlashArc");
                if (slash != null)
                {
                    _slashArc = slash as RectTransform;
                    _slashGroup = slash.GetComponent<CanvasGroup>();
                }
            }
        }
#endif
    }
}
