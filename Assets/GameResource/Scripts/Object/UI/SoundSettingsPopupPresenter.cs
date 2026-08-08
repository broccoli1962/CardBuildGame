using System;
using Backend.Object.Management;
using R3;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Backend.Object.UI
{
    public class SoundSettingsPopupPresenter : UIPresenter<SoundSettingsPopup>
    {
        private CompositeDisposable _disposables;

        public override void OnOpen()
        {
            _disposables?.Dispose();
            _disposables = new CompositeDisposable();

            if (View == null)
                return;

            View.SetHeader("사운드 설정");
            View.SetBgmLabel("BGM");
            View.SetSfxLabel("SFX");
            View.SetBgmVolume(AudioManager.GetBgmVolume());
            View.SetSfxVolume(AudioManager.GetSfxVolume());

            BindSlider(View.BgmSlider, AudioManager.SetBgmVolume);
            BindSlider(View.SfxSlider, AudioManager.SetSfxVolume);

            if (View.CloseButton != null)
            {
                View.CloseButton.OnClickAsObservable()
                    .Subscribe(_ => UIManager.Close(View))
                    .AddTo(_disposables);
            }
        }

        public override void OnClose()
        {
            _disposables?.Dispose();
            _disposables = null;
        }

        private void BindSlider(Slider slider, Action<float> onChanged)
        {
            if (slider == null || onChanged == null)
                return;

            UnityAction<float> handler = value => onChanged(value);
            slider.onValueChanged.AddListener(handler);
            Disposable.Create(() =>
            {
                if (slider != null)
                    slider.onValueChanged.RemoveListener(handler);
            }).AddTo(_disposables);
        }
    }
}
