using Backend.AddressableKey;
using Backend.Object.GameSystems.Gameplay;
using Backend.Object.Management;
using Backend.Util;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Backend.Object.UI
{
    public class Monster : CachedMonobehaviour
    {
        [SerializeField] private Image _monsterImage;
        [SerializeField] private SingleGaugeBar _healthGauge;
        [SerializeField] private GameObject _actionLayout;
        [SerializeField] private Image _actionImage;
        [SerializeField] private TMPro.TextMeshProUGUI _actionValueText;

        private string _loadedMonsterIconKey;
        private string _loadedActionIconKey;

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

            LoadMonsterIconAsync(monster.icon_key).Forget();
        }

        /// <summary>
        /// HP 게이지와 다음 행동(Intent) 표시를 갱신합니다.
        /// </summary>
        public void SetStats(int hp, int maxHp, int shield, EnemyActionPreview intent)
        {
            if (_healthGauge != null)
                _healthGauge.SetValue(hp, maxHp);

            UpdateActionPreview(intent);
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
    }
}
