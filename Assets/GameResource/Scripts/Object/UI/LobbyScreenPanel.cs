using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

namespace Backend.Object.UI
{
    public class LobbyScreenPanel : UIPanel<LobbyScreenPanelPresenter>
    {
        [SerializeField] private CommonButton _startButton;
        [SerializeField] private CommonButton _openSoundSettingsButton;
        [SerializeField] private float _pulseScale = 1.08f;
        [SerializeField] private float _pulseDuration = 0.8f;

        private MotionHandle _pulseHandle;

        public CommonButton StartButton => _startButton;
        public CommonButton OpenSoundSettingsButton => _openSoundSettingsButton;

        /// <summary>
        /// StartButton 커졌다 작아졌다 하는 루프 스케일 연출을 시작합니다.
        /// </summary>
        public void StartStartButtonPulse()
        {
            StopStartButtonPulse();

            if (_startButton == null)
                return;

            var tr = _startButton.transform;
            tr.localScale = Vector3.one;

            _pulseHandle = LMotion.Create(Vector3.one, Vector3.one * _pulseScale, _pulseDuration)
                .WithEase(Ease.InOutSine)
                .WithLoops(-1, LoopType.Yoyo)
                .BindToLocalScale(tr);
        }

        /// <summary>
        /// StartButton 펄스 연출을 중단하고 스케일을 복원합니다.
        /// </summary>
        public void StopStartButtonPulse()
        {
            if (_pulseHandle.IsActive())
                _pulseHandle.Cancel();

            if (_startButton != null)
                _startButton.transform.localScale = Vector3.one;
        }

        private void OnDisable()
        {
            StopStartButtonPulse();
        }
    }
}
