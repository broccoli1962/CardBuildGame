using System.Threading;
using Backend.AddressableKey;
using Backend.Object.GameSystems.Gameplay;
using Backend.Object.Management;
using Backend.Util;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace Backend.Object.UI
{
    public class Monster : CachedMonobehaviour
    {
        [SerializeField] private Image _monsterImage;
        [SerializeField] private SingleGaugeBar _healthGauge;
        [SerializeField] private GameObject _shieldLayout;
        [SerializeField] private Image _shieldIcon;
        [SerializeField] private TMPro.TextMeshProUGUI _shieldText;
        [SerializeField] private GameObject _actionLayout;
        [SerializeField] private Image _actionImage;
        [SerializeField] private TMPro.TextMeshProUGUI _actionValueText;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _deathFadeDuration = 0.75f;

        private string _loadedMonsterIconKey;
        private string _loadedActionIconKey;
        private string _loadedShieldIconKey;
        private CancellationTokenSource _deathCts;

        /// <summary>
        /// 몬스터 테이블 데이터를 UI에 바인딩합니다.
        /// </summary>
        public void Bind(MonsterData monster)
        {
            if (monster == null)
            {
                Debug.LogError("[Monster] MonsterData is null.");
                return;
            }

            CancelDeathFade();
            ResetVisualState();
            LoadMonsterIconAsync(monster.icon_key).Forget();
        }

        /// <summary>
        /// HP 게이지, 방어도, 다음 행동(Intent) 표시를 갱신합니다.
        /// </summary>
        public void SetStats(int hp, int maxHp, int shield, EnemyActionPreview intent)
        {
            if (_healthGauge != null)
                _healthGauge.SetValue(hp, maxHp);

            UpdateShield(shield);
            UpdateActionPreview(intent);
        }

        /// <summary>
        /// 몬스터가 서서히 사라지는 사망 연출을 재생합니다.
        /// </summary>
        public async UniTask PlayDeathFadeAsync(CancellationToken externalToken = default)
        {
            EnsureCanvasGroup();
            CancelDeathFade();
            _deathCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
            var token = _deathCts.Token;

            if (_actionLayout != null)
                _actionLayout.SetActive(false);

            if (_shieldLayout != null)
                _shieldLayout.SetActive(false);

            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            try
            {
                await LMotion.Create(1f, 0f, _deathFadeDuration)
                    .WithEase(Ease.OutQuad)
                    .BindToAlpha(_canvasGroup)
                    .ToUniTask(token);
            }
            finally
            {
                if (_canvasGroup != null)
                    _canvasGroup.alpha = 0f;
            }
        }

        /// <summary>
        /// 풀 재사용을 위해 시각 상태를 초기화합니다.
        /// </summary>
        public void ResetVisualState()
        {
            CancelDeathFade();
            EnsureCanvasGroup();

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.blocksRaycasts = true;
                _canvasGroup.interactable = true;
            }
        }

        private void UpdateShield(int shield)
        {
            var showShield = shield > 0;
            if (_shieldLayout != null)
                _shieldLayout.SetActive(showShield);

            if (!showShield)
                return;

            LoadShieldIconAsync().Forget();

            if (_shieldText != null)
                _shieldText.text = shield.ToString();
        }

        private void UpdateActionPreview(EnemyActionPreview intent)
        {
            var iconKey = intent.IsValid ? GetActionIconKey(intent.ActionType) : string.Empty;
            var showAction = !string.IsNullOrEmpty(iconKey);

            if (_actionLayout != null)
                _actionLayout.SetActive(showAction);

            if (!showAction)
                return;

            LoadActionIconAsync(iconKey).Forget();

            if (_actionValueText != null)
            {
                var showValue = intent.Value > 0;
                _actionValueText.gameObject.SetActive(showValue);
                if (showValue)
                    _actionValueText.text = intent.Value.ToString();
            }
        }

        private static string GetActionIconKey(EnemyActionType actionType)
        {
            return actionType switch
            {
                EnemyActionType.Attack => "Icon_Swords",
                EnemyActionType.Breath => "Icon_Swords",
                EnemyActionType.Defend => "Icon_Shield",
                EnemyActionType.DrainMana => "Icon_Mana",
                _ => string.Empty
            };
        }

        private async UniTaskVoid LoadMonsterIconAsync(string iconKey)
        {
            if (await TryLoadIconAsync(_monsterImage, iconKey, _loadedMonsterIconKey))
                _loadedMonsterIconKey = iconKey;

            if (_monsterImage != null && _monsterImage.sprite != null)
                _monsterImage.SetNativeSize();
        }

        private async UniTaskVoid LoadActionIconAsync(string iconKey)
        {
            if (await TryLoadIconAsync(_actionImage, iconKey, _loadedActionIconKey))
                _loadedActionIconKey = iconKey;
        }

        private async UniTaskVoid LoadShieldIconAsync()
        {
            const string iconKey = "Icon_Shield";
            if (await TryLoadIconAsync(_shieldIcon, iconKey, _loadedShieldIconKey))
                _loadedShieldIconKey = iconKey;

            if (_shieldIcon != null)
                _shieldIcon.preserveAspect = true;
        }

        private static async UniTask<bool> TryLoadIconAsync(Image target, string iconKey, string loadedKey)
        {
            if (target == null || string.IsNullOrEmpty(iconKey))
                return false;

            if (loadedKey == iconKey && target.sprite != null)
                return false;

            var address = AddressableKeys.Icons.Get(iconKey);
            if (string.IsNullOrEmpty(address))
            {
                Debug.LogWarning($"[Monster] Icon key not registered: {iconKey}");
                return false;
            }

            var sprite = await ResourceManager.LoadResourceAsync<Sprite>(address);
            if (sprite == null)
                return false;

            target.sprite = sprite;
            target.enabled = true;
            return true;
        }

        private void EnsureCanvasGroup()
        {
            if (_canvasGroup != null)
                return;

            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        private void CancelDeathFade()
        {
            if (_deathCts == null)
                return;

            _deathCts.Cancel();
            _deathCts.Dispose();
            _deathCts = null;
        }

        private void OnDisable()
        {
            CancelDeathFade();
        }

        private void OnDestroy()
        {
            CancelDeathFade();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();
        }
#endif
    }
}
