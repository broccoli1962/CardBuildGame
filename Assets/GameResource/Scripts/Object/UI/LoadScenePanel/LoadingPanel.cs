using Backend.Object.UI;
using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;
using UnityEngine.UI;

namespace Backend.Object.UI
{
    public class LoadingPanel : UIPanel
    {
        [SerializeField] private SpinIcon _spinIcon;
        [SerializeField] private Image _progressBar;

        private MotionHandle _progressHandle;
        private float _progress;

        protected override void OnOpen()
        {
            base.OnOpen();

            //단순 연출용 스크립트
            _spinIcon.Init();

            //비동기 작업 시작
            SetProgress(0f);
        }

        protected override void OnClose()
        {
            _spinIcon.StopIconSpin();
            if (_progressHandle.IsActive())
            {
                _progressHandle.Cancel();
            }
            base.OnClose();
        }

        public void SetProgress(float value)
        {
            _progress = Mathf.Clamp01(value);
            if (_progressBar != null)
            {
                _progressBar.fillAmount = _progress;
            }
        }

        public async UniTask AnimateProgressAsync(float f, float t, float duration){
            if(_progressHandle.IsActive()){
                _progressHandle.Cancel();
            }

            SetProgress(f);

            _progressHandle = LMotion.Create(f, t, duration)
                .WithEase(Ease.OutQuint)
                .Bind(SetProgress);

            await _progressHandle.ToUniTask();
        }
    }
}