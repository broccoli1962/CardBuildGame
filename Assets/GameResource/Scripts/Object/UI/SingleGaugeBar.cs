using UnityEngine;
using UnityEngine.UI;

namespace Backend.Object.UI
{
    /// <summary>
    /// 단일 Fill 이미지로 표시하는 게이지 (적 HP 등).
    /// </summary>
    public class SingleGaugeBar : CommonGaugeBar
    {
        [SerializeField] private Image _fill;

        public void SetValue(int current, int max)
        {
            if (_fill == null) return;
            max = Mathf.Max(1, max);
            _fill.fillAmount = Mathf.Clamp01((float)current / max);
        }
    }
}
