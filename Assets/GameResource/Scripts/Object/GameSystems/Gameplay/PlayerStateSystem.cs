using Backend.Object.Management;
using R3;
using UnityEngine;

namespace Backend.Object.GameSystems.Gameplay
{
    public static class PlayerStateSystem
    {
        #region Fields

        private static readonly ReactiveProperty<int> _hp = new(0);
        private static readonly ReactiveProperty<int> _maxHp = new(0);
        private static readonly ReactiveProperty<int> _mana = new(0);
        private static readonly ReactiveProperty<int> _maxMana = new(0);
        private static readonly ReactiveProperty<int> _shield = new(0);
        private static readonly ReactiveProperty<bool> _nextAttackNegate = new(false);
        private static readonly ReactiveProperty<bool> _nextCardFree = new(false);

        #endregion

        #region Properties

        public static ReadOnlyReactiveProperty<int> Hp => _hp;
        public static ReadOnlyReactiveProperty<int> MaxHp => _maxHp;
        public static ReadOnlyReactiveProperty<int> Mana => _mana;
        public static ReadOnlyReactiveProperty<int> MaxMana => _maxMana;
        public static ReadOnlyReactiveProperty<int> Shield => _shield;
        public static ReadOnlyReactiveProperty<bool> NextAttackNegate => _nextAttackNegate;
        public static ReadOnlyReactiveProperty<bool> NextCardFree => _nextCardFree;

        #endregion

        #region Public Methods

        public static void Initialize()
        {
            var maxHp = TableManager.GetInt(TableManager.BalanceKey.PlayerStartMaxHp, 10);
            var startHp = TableManager.GetInt(TableManager.BalanceKey.PlayerStartHp, 10);
            var maxMana = TableManager.GetInt(TableManager.BalanceKey.PlayerStartMaxMana, 5);

            _maxHp.Value = maxHp;
            _hp.Value = Mathf.Min(startHp, maxHp);
            _maxMana.Value = maxMana;
            _mana.Value = maxMana;
            ResetBattleState();
        }

        /// <summary>
        /// 노드 전환·전투 시작 시 방어도와 일회성 플래그를 초기화합니다.
        /// </summary>
        public static void ResetBattleState()
        {
            _shield.Value = 0;
            _nextAttackNegate.Value = false;
            _nextCardFree.Value = false;
        }

        public static void RefillMana()
        {
            _mana.Value = _maxMana.Value;
        }

        public static bool SpendMana(int amount)
        {
            if (amount <= 0)
                return true;

            if (_mana.Value < amount)
                return false;

            _mana.Value -= amount;
            return true;
        }

        public static void RecoverMana(int amount)
        {
            if (amount <= 0)
                return;

            _mana.Value = Mathf.Min(_maxMana.Value, _mana.Value + amount);
        }

        public static void DrainMana(int amount)
        {
            if (amount <= 0)
                return;

            _mana.Value = Mathf.Max(0, _mana.Value - amount);
        }

        public static void GainShield(int amount)
        {
            if (amount <= 0)
                return;

            _shield.Value += amount;
        }

        public static void Heal(int amount)
        {
            if (amount >= 99)
            {
                _hp.Value = _maxHp.Value;
                return;
            }

            if (amount <= 0)
                return;

            _hp.Value = Mathf.Min(_maxHp.Value, _hp.Value + amount);
        }

        public static void ChangeHp(int delta)
        {
            if (delta == -999)
            {
                _hp.Value = 1;
                return;
            }

            if (delta == 0)
                return;

            _hp.Value = Mathf.Clamp(_hp.Value + delta, 0, _maxHp.Value);
        }

        public static void ChangeMaxMana(int delta)
        {
            if (delta == 0)
                return;

            _maxMana.Value = Mathf.Max(0, _maxMana.Value + delta);
            _mana.Value = Mathf.Min(_mana.Value, _maxMana.Value);
        }

        /// <summary>
        /// 최대 체력을 변경합니다. 증가분은 현재 체력에도 더합니다.
        /// </summary>
        public static void ChangeMaxHp(int delta)
        {
            if (delta == 0)
                return;

            _maxHp.Value = Mathf.Max(1, _maxHp.Value + delta);
            if (delta > 0)
                _hp.Value = Mathf.Min(_maxHp.Value, _hp.Value + delta);
            else
                _hp.Value = Mathf.Min(_hp.Value, _maxHp.Value);
        }

        public static void SetNextAttackNegate(bool value)
        {
            _nextAttackNegate.Value = value;
        }

        public static void SetNextCardFree(bool value)
        {
            _nextCardFree.Value = value;
        }

        /// <summary>
        /// 적 공격 피해를 방어도·무효화 플래그 규칙에 따라 적용합니다.
        /// </summary>
        public static void TakeDamage(int amount)
        {
            if (amount <= 0)
                return;

            if (_nextAttackNegate.Value)
            {
                _nextAttackNegate.Value = false;
                return;
            }

            var shield = _shield.Value;
            var blocked = Mathf.Min(shield, amount);
            _shield.Value = shield - blocked;

            var remaining = amount - blocked;
            if (remaining > 0)
                _hp.Value = Mathf.Max(0, _hp.Value - remaining);
        }

        #endregion
    }
}
