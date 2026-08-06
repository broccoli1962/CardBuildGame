using Backend.Object.Management;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace Backend.Object.GameSystems.Gameplay
{
    public static class BattleSystem
    {
        #region Fields

        private static readonly ReactiveProperty<int> _enemyHp = new(0);
        private static readonly ReactiveProperty<int> _enemyMaxHp = new(0);
        private static readonly ReactiveProperty<int> _enemyShield = new(0);
        private static readonly ReactiveProperty<int> _enemyAttack = new(0);
        private static readonly ReactiveProperty<string> _enemyNameKey = new(string.Empty);
        private static readonly ReactiveProperty<EnemyActionPreview> _nextAction = new(default);
        private static readonly Subject<string> _onBattleLog = new();

        private static string _monsterId;
        private static int _enemyTurnIndex;
        private static int _threatTier;
        private static int _chapter;
        private static bool _isElite;
        private static bool _isBoss;
        private static bool _isBattleActive;
        private static bool _isEnemyTurnRunning;
        private static int _turnSequence;

        #endregion

        #region Properties

        public static ReadOnlyReactiveProperty<int> EnemyHp => _enemyHp;
        public static ReadOnlyReactiveProperty<int> EnemyMaxHp => _enemyMaxHp;
        public static ReadOnlyReactiveProperty<int> EnemyShield => _enemyShield;
        public static ReadOnlyReactiveProperty<int> EnemyAttack => _enemyAttack;
        public static ReadOnlyReactiveProperty<string> EnemyNameKey => _enemyNameKey;
        public static ReadOnlyReactiveProperty<EnemyActionPreview> NextAction => _nextAction;
        public static Observable<string> OnBattleLog => _onBattleLog;
        public static bool IsBattleActive => _isBattleActive;
        public static string MonsterId => _monsterId;

        #endregion

        #region Public Methods

        public static void Initialize()
        {
            _turnSequence = 0;
            _isEnemyTurnRunning = false;
        }

        /// <summary>
        /// 몬스터 데이터와 위협도 스케일링을 적용해 전투를 시작합니다.
        /// </summary>
        public static void StartBattle(
            string monsterId,
            int threatTier = 0,
            int chapter = 1,
            bool isElite = false,
            bool isBoss = false)
        {
            var monster = TableManager.GetMonster(monsterId);
            if (monster == null)
            {
                Debug.LogError($"[BattleSystem] Monster not found: {monsterId}");
                return;
            }

            _monsterId = monsterId;
            _threatTier = threatTier;
            _chapter = chapter;
            _isElite = isElite;
            _isBoss = isBoss;
            _enemyTurnIndex = 0;
            _isBattleActive = true;
            _isEnemyTurnRunning = false;
            _turnSequence++;

            PlayerStateSystem.ResetBattleState();
            DeckSystem.PrepareForBattle();
            ApplyMonsterScaling(monster);

            _enemyNameKey.Value = monster.name_key;
            _enemyShield.Value = 0;

            RefreshIntent();
            BeginPlayerTurn(isBattleStart: true);

            LogBattle($"전투 시작: {monster.name_key.GetLocalizeText()}");
        }

        /// <summary>
        /// 플레이어 턴 중 손패 카드 사용을 시도합니다.
        /// </summary>
        public static bool TryPlayCard(RuntimeCard card)
        {
            if (!_isBattleActive || _isEnemyTurnRunning)
                return false;

            if (GameManager.CurrentPhase != GamePhase.PlayerTurn)
                return false;

            if (card == null || !DeckSystem.ContainsInHand(card))
                return false;

            if (!CanPlayCard(card, out var blockReason))
            {
                if (!string.IsNullOrEmpty(blockReason))
                    LogBattle(blockReason);

                return false;
            }

            PayManaCost(card);
            ApplyCardEffects(card);
            DeckSystem.DiscardFromHand(card);

            if (_enemyHp.Value <= 0)
            {
                OnEnemyDefeated();
                return true;
            }

            return true;
        }

        /// <summary>
        /// 카드 사용 가능 여부를 반환합니다.
        /// </summary>
        public static bool CanPlayCard(RuntimeCard card)
        {
            return CanPlayCard(card, out _);
        }

        /// <summary>
        /// 플레이어 턴을 종료하고 적 턴을 시작합니다.
        /// </summary>
        public static void EndPlayerTurn()
        {
            if (!_isBattleActive || _isEnemyTurnRunning)
                return;

            if (GameManager.CurrentPhase != GamePhase.PlayerTurn)
                return;

            DeckSystem.DiscardHand();
            RunEnemyTurnAsync().Forget();
        }

        public static void Dispose()
        {
            _turnSequence++;
            _isBattleActive = false;
            _isEnemyTurnRunning = false;
            _monsterId = null;

            _enemyHp.Value = 0;
            _enemyMaxHp.Value = 0;
            _enemyShield.Value = 0;
            _enemyAttack.Value = 0;
            _enemyNameKey.Value = string.Empty;
            _nextAction.Value = default;
        }

        #endregion

        #region Turn Flow

        private static void BeginPlayerTurn(bool isBattleStart = false)
        {
            if (!_isBattleActive)
                return;

            GameManager.SetPhase(GamePhase.PlayerTurn);
            PlayerStateSystem.RefillMana();

            // 전투 시작 손패는 PrepareForBattle에서 이미 뽑는다.
            // 여기서 DrawInitialHand를 다시 호출하면 기존 손패가 유실되어 남은 덱이 비게 된다.
            if (!isBattleStart)
                DeckSystem.DrawToHandSize();

            RefreshIntent();
        }

        private static async UniTaskVoid RunEnemyTurnAsync()
        {
            _isEnemyTurnRunning = true;
            var sequence = ++_turnSequence;

            GameManager.SetPhase(GamePhase.EnemyTurn);

            var delayMs = Mathf.RoundToInt(
                TableManager.GetFloat(TableManager.BalanceKey.EnemyTurnDelay, 1.4f) * 1000f);

            if (delayMs > 0)
                await UniTask.Delay(delayMs);

            if (!_isBattleActive || sequence != _turnSequence)
                return;

            ExecuteEnemyAction();
            _enemyTurnIndex++;
            RefreshIntent();

            if (PlayerStateSystem.Hp.CurrentValue <= 0)
            {
                _isEnemyTurnRunning = false;
                _isBattleActive = false;
                GameManager.SetPhase(GamePhase.GameOver);
                GameManager.GameOver();
                LogBattle("패배...");
                return;
            }

            _isEnemyTurnRunning = false;
            BeginPlayerTurn();
        }

        private static void OnEnemyDefeated()
        {
            _isBattleActive = false;
            _turnSequence++;
            PlayerStateSystem.RefillMana();
            GameManager.SetPhase(GamePhase.NodeClear);
            GameManager.StageClear();
            LogBattle("적 처치!");
        }

        #endregion

        #region Intent

        private static void RefreshIntent()
        {
            if (string.IsNullOrEmpty(_monsterId))
            {
                _nextAction.Value = default;
                return;
            }

            var turnForPreview = _enemyTurnIndex + 1;
            var action = TableManager.GetNextMonsterAction(_monsterId, turnForPreview, _threatTier);
            _nextAction.Value = EnemyActionPreview.FromAction(action);
        }

        #endregion

        #region Card Effects

        private static bool CanPlayCard(RuntimeCard card, out string blockReason)
        {
            blockReason = null;

            if (PlayerStateSystem.NextCardFree.CurrentValue)
            {
                return ValidateEffectCosts(card, out blockReason);
            }

            if (PlayerStateSystem.Mana.CurrentValue < card.ManaCost)
            {
                blockReason = "마나가 부족합니다.";
                return false;
            }

            return ValidateEffectCosts(card, out blockReason);
        }

        private static bool ValidateEffectCosts(RuntimeCard card, out string blockReason)
        {
            blockReason = null;

            foreach (var effect in card.Effects)
            {
                switch (effect.Type)
                {
                    case CardEffectType.GAIN_MANA_FROM_HP:
                        if (PlayerStateSystem.Hp.CurrentValue < 1 || PlayerStateSystem.Mana.CurrentValue < 1)
                        {
                            blockReason = "체력 또는 마나가 부족합니다.";
                            return false;
                        }
                        break;

                    case CardEffectType.PLAYER_HP_CHANGE when effect.Value < 0 && effect.Value != -999:
                        if (PlayerStateSystem.Hp.CurrentValue + effect.Value < 1)
                        {
                            blockReason = "체력이 부족합니다.";
                            return false;
                        }
                        break;
                }
            }

            return true;
        }

        private static void PayManaCost(RuntimeCard card)
        {
            if (PlayerStateSystem.NextCardFree.CurrentValue)
            {
                PlayerStateSystem.SetNextCardFree(false);
                return;
            }

            PlayerStateSystem.SpendMana(card.ManaCost);
        }

        private static void ApplyCardEffects(RuntimeCard card)
        {
            foreach (var effect in card.Effects)
            {
                ApplyCardEffect(effect);
            }
        }

        private static void ApplyCardEffect(CardEffect effect)
        {
            switch (effect.Type)
            {
                case CardEffectType.DEAL_DAMAGE:
                    DealDamageToEnemy(effect.Value, effect.Target);
                    break;

                case CardEffectType.GAIN_SHIELD:
                    PlayerStateSystem.GainShield(effect.Value);
                    LogBattle($"방어도 +{effect.Value}");
                    break;

                case CardEffectType.HEAL_HP:
                    PlayerStateSystem.Heal(effect.Value);
                    LogBattle(effect.Value >= 99 ? "체력 완전 회복" : $"체력 +{effect.Value}");
                    break;

                case CardEffectType.PLAYER_HP_CHANGE:
                    PlayerStateSystem.ChangeHp(effect.Value);
                    LogBattle("체력 변화");
                    break;

                case CardEffectType.MANA_RECOVER:
                    PlayerStateSystem.RecoverMana(effect.Value);
                    LogBattle($"마나 +{effect.Value}");
                    break;

                case CardEffectType.MAX_MANA_CHANGE:
                    PlayerStateSystem.ChangeMaxMana(effect.Value);
                    LogBattle($"최대 마나 {FormatSigned(effect.Value)}");
                    break;

                case CardEffectType.SET_INVULNERABLE:
                    PlayerStateSystem.SetNextAttackNegate(true);
                    LogBattle("다음 적 공격 무효");
                    break;

                case CardEffectType.FREE_NEXT_CARD:
                    PlayerStateSystem.SetNextCardFree(true);
                    LogBattle("다음 카드 무료");
                    break;

                case CardEffectType.GAIN_MANA_FROM_HP:
                    PlayerStateSystem.ChangeHp(-1);
                    PlayerStateSystem.SpendMana(1);
                    PlayerStateSystem.RecoverMana(5);
                    LogBattle("체력 1·마나 1 소모 → 마나 +5");
                    break;
            }
        }

        /// <summary>
        /// 적에게 피해를 적용합니다. 현재는 단일 적만 존재하므로 Aoe도 동일 대상에 적용합니다.
        /// </summary>
        private static void DealDamageToEnemy(int amount, DamageTargetType target = DamageTargetType.Single)
        {
            if (amount <= 0)
                return;

            // TODO: 멀티 적 전투 시 Aoe는 모든 적, Single은 선택/전방 적에게 분기.
            var shield = _enemyShield.Value;
            var blocked = Mathf.Min(shield, amount);
            _enemyShield.Value = shield - blocked;

            var remaining = amount - blocked;
            if (remaining > 0)
                _enemyHp.Value = Mathf.Max(0, _enemyHp.Value - remaining);

            var formLabel = target == DamageTargetType.Aoe ? "광역" : "단일";
            LogBattle($"적에게 {amount} 피해 ({formLabel})");
        }

        #endregion

        #region Enemy Actions

        private static void ExecuteEnemyAction()
        {
            var preview = _nextAction.Value;
            if (!preview.IsValid)
            {
                LogBattle("적이 대기합니다.");
                return;
            }

            switch (preview.ActionType)
            {
                case EnemyActionType.Attack:
                case EnemyActionType.Breath:
                    PlayerStateSystem.TakeDamage(preview.Value);
                    LogBattle($"적 공격 {preview.Value}");
                    break;

                case EnemyActionType.Defend:
                    _enemyShield.Value += preview.Value;
                    LogBattle($"적 방어도 +{preview.Value}");
                    break;

                case EnemyActionType.DrainMana:
                    PlayerStateSystem.DrainMana(preview.Value);
                    LogBattle($"마나 -{preview.Value}");
                    break;

                case EnemyActionType.Idle:
                    LogBattle("적이 대기합니다.");
                    break;
            }
        }

        #endregion

        #region Monster Scaling

        private static void ApplyMonsterScaling(MonsterData monster)
        {
            var scaling = TableManager.GetThreatScalingByTier(_threatTier)
                          ?? TableManager.GetThreatScaling(0);

            var hpMult = scaling?.hp_mult ?? 1f;
            var atkMult = scaling?.atk_mult ?? 1f;

            if (_isElite)
            {
                var elite = TableManager.GetEliteScaling(_chapter);
                if (elite != null)
                {
                    hpMult *= elite.hp_mult;
                    atkMult *= elite.atk_mult;
                    hpMult = Mathf.Min(hpMult, TableManager.GetFloat(TableManager.BalanceKey.HpMultCap, 2.2f));
                    atkMult = Mathf.Min(atkMult, TableManager.GetFloat(TableManager.BalanceKey.AtkMultCap, 1.5f));
                }
            }

            if (_isBoss)
            {
                atkMult = Mathf.Min(
                    atkMult,
                    TableManager.GetFloat(TableManager.BalanceKey.BossAtkMultCap, 1.35f));
            }

            var maxHp = Mathf.CeilToInt(monster.max_hp * hpMult);
            if (_isElite)
            {
                var elite = TableManager.GetEliteScaling(_chapter);
                if (elite != null)
                    maxHp += elite.bonus_max_hp;
            }

            _enemyMaxHp.Value = maxHp;
            _enemyHp.Value = maxHp;
            _enemyAttack.Value = Mathf.CeilToInt(monster.base_attack * atkMult);
        }

        #endregion

        #region Helpers

        private static void LogBattle(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            _onBattleLog.OnNext(message);
            Debug.Log($"[BattleSystem] {message}");
        }

        private static string FormatSigned(int value) => value >= 0 ? $"+{value}" : value.ToString();

        #endregion
    }
}
