using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Backend.Object.UI
{
    /// <summary>
    /// HP/Mana 등 개별 Pip 스프라이트로 채워지는 게이지.
    /// </summary>
    public class PipGaugeBar : CommonGaugeBar
    {
        [SerializeField] private RectTransform _pipContainer;
        [SerializeField] private Image _pipTemplate;
        [SerializeField] private Sprite _activeSprite;
        [SerializeField] private Sprite _inactiveSprite;
        [SerializeField] private float _pipSize = 18f;

        private readonly List<Image> _pips = new();

        protected override void Awake()
        {
            base.Awake();
            if (_pipTemplate != null)
                _pipTemplate.gameObject.SetActive(false);
        }

        public void SetValue(int current, int max)
        {
            max = Mathf.Max(0, max);
            current = Mathf.Clamp(current, 0, max);
            EnsurePipCount(max);

            for (int i = 0; i < _pips.Count; i++)
            {
                var pip = _pips[i];
                var active = i < current;
                pip.sprite = active ? _activeSprite : _inactiveSprite;
                pip.color = Color.white;
                pip.gameObject.SetActive(i < max);
            }
        }

        private void EnsurePipCount(int count)
        {
            while (_pips.Count < count)
            {
                var pipGo = Instantiate(_pipTemplate.gameObject, _pipContainer);
                pipGo.SetActive(true);
                var pip = pipGo.GetComponent<Image>();
                var rt = pip.rectTransform;
                rt.sizeDelta = new Vector2(_pipSize, _pipSize);
                _pips.Add(pip);
            }
        }
    }
}
