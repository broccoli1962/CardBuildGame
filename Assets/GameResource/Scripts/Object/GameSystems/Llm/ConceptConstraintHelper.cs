using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Backend.Object.GameSystems.Gameplay;
using Backend.Object.Management;
using UnityEngine;

namespace Backend.Object.GameSystems.Llm
{
    /// <summary>
    /// 플레이어 콘셉트에 명시된 수치(데미지/마나 등)를 파싱하고 생성 카드에 강제 반영한다.
    /// 소형 로컬 LLM이 숫자를 무시하는 경우를 보정한다.
    /// </summary>
    public static class ConceptConstraintHelper
    {
        // 한국어 조사(의/을/를 등)가 끼어도 파싱되도록 허용한다.
        private const string KoParticle = @"(?:의|을|를|은|는|이|가|로|으로|만큼)?";

        private static readonly Regex DamageRegex = new(
            $@"(?:(?:데미지|피해|damage|dmg)\s*{KoParticle}\s*[:=]?\s*(\d+))|(?:(\d+)\s*{KoParticle}\s*(?:데미지|피해|damage|dmg))",
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex ManaRegex = new(
            $@"(?:(?:마나|코스트|mana|cost)\s*{KoParticle}\s*[:=]?\s*(\d+))|(?:(\d+)\s*{KoParticle}\s*(?:마나|코스트|mana|cost))",
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex ShieldRegex = new(
            $@"(?:(?:실드|방어도|shield)\s*{KoParticle}\s*[:=]?\s*(\d+))|(?:(\d+)\s*{KoParticle}\s*(?:실드|방어도|shield))",
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex HealRegex = new(
            $@"(?:(?:회복|힐|heal)\s*{KoParticle}\s*[:=]?\s*(\d+))|(?:(\d+)\s*{KoParticle}\s*(?:회복|힐|heal))",
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public readonly struct Constraints
        {
            public int? Damage { get; }
            public int? ManaCost { get; }
            public int? Shield { get; }
            public int? Heal { get; }

            public bool HasAny => Damage.HasValue || ManaCost.HasValue || Shield.HasValue || Heal.HasValue;

            public Constraints(int? damage, int? manaCost, int? shield, int? heal)
            {
                Damage = damage;
                ManaCost = manaCost;
                Shield = shield;
                Heal = heal;
            }
        }

        public static Constraints Parse(string userConcept)
        {
            if (string.IsNullOrWhiteSpace(userConcept))
                return default;

            return new Constraints(
                TryReadInt(DamageRegex, userConcept),
                TryReadInt(ManaRegex, userConcept),
                TryReadInt(ShieldRegex, userConcept),
                TryReadInt(HealRegex, userConcept));
        }

        /// <summary>
        /// 프롬프트에 넣을 필수 수치 힌트. 없으면 빈 문자열.
        /// </summary>
        public static string BuildPromptHint(Constraints constraints)
        {
            if (!constraints.HasAny)
                return string.Empty;

            var sb = new StringBuilder();
            sb.AppendLine("Required numeric values (use EXACTLY these):");
            if (constraints.Damage.HasValue)
                sb.AppendLine($"- DEAL_DAMAGE value = {constraints.Damage.Value}");
            if (constraints.ManaCost.HasValue)
                sb.AppendLine($"- mana_cost = {constraints.ManaCost.Value}");
            if (constraints.Shield.HasValue)
                sb.AppendLine($"- GAIN_SHIELD value = {constraints.Shield.Value}");
            if (constraints.Heal.HasValue)
                sb.AppendLine($"- HEAL_HP value = {constraints.Heal.Value}");
            return sb.ToString();
        }

        /// <summary>
        /// 파싱된 수치를 카드에 덮어쓴다. 테이블 min/max로 클램프한다.
        /// </summary>
        public static void Apply(GeneratedCardData card, Constraints constraints)
        {
            if (card?.effects == null || !constraints.HasAny)
                return;

            var maxMana = TableManager.GetInt(TableManager.BalanceKey.CardManaCostMax, 15);

            if (constraints.ManaCost.HasValue)
            {
                card.mana_cost = Mathf.Clamp(constraints.ManaCost.Value, 0, maxMana);
            }
            else if (constraints.Damage.HasValue)
            {
                // 데미만 명시한 경우, 모델이 임의로 붙인 높은 마나를 데미지에 맞게 낮춘다.
                card.mana_cost = Mathf.Clamp(Mathf.CeilToInt(constraints.Damage.Value * 0.5f), 0, maxMana);
            }

            if (constraints.Damage.HasValue)
                ApplyOrInsertEffect(card, CardEffectType.DEAL_DAMAGE, constraints.Damage.Value);

            if (constraints.Shield.HasValue)
            {
                ApplyOrInsertEffect(card, CardEffectType.GAIN_SHIELD, constraints.Shield.Value);
                // 소형 모델이 방어도 요청을 HEAL_HP로 넣는 경우가 많아, 회복 제약이 없으면 제거한다.
                if (!constraints.Heal.HasValue)
                    RemoveEffectType(card, CardEffectType.HEAL_HP);
                card.card_type = CardType.Defense;
            }

            if (constraints.Heal.HasValue)
            {
                ApplyOrInsertEffect(card, CardEffectType.HEAL_HP, constraints.Heal.Value);
                if (!constraints.Shield.HasValue)
                    RemoveEffectType(card, CardEffectType.GAIN_SHIELD);
                card.card_type = CardType.Heal;
            }
        }

        /// <summary>
        /// LLM이 card_type과 effect type을 어긋나게 낸 경우를 보정한다.
        /// 예: Defense + HEAL_HP → GAIN_SHIELD
        /// </summary>
        public static void AlignCardTypeAndEffects(GeneratedCardData card)
        {
            if (card?.effects == null || card.effects.Count == 0)
                return;

            var hasDamage = HasEffect(card, CardEffectType.DEAL_DAMAGE);
            var hasShield = HasEffect(card, CardEffectType.GAIN_SHIELD);
            var hasHeal = HasEffect(card, CardEffectType.HEAL_HP);

            // 소형 LLM이 defense/heal 타입과 효과를 자주 뒤바꾼다.
            if (card.card_type == CardType.Defense && hasHeal && !hasShield)
                ReplaceEffectType(card, CardEffectType.HEAL_HP, CardEffectType.GAIN_SHIELD);
            else if (card.card_type == CardType.Heal && hasShield && !hasHeal)
                ReplaceEffectType(card, CardEffectType.GAIN_SHIELD, CardEffectType.HEAL_HP);

            hasShield = HasEffect(card, CardEffectType.GAIN_SHIELD);
            hasHeal = HasEffect(card, CardEffectType.HEAL_HP);

            if (hasDamage || card.card_type == CardType.Special)
                return;

            if (hasShield && !hasHeal)
                card.card_type = CardType.Defense;
            else if (hasHeal && !hasShield)
                card.card_type = CardType.Heal;
        }

        private static void ApplyOrInsertEffect(GeneratedCardData card, CardEffectType type, int rawValue)
        {
            var value = TableManager.ClampEffectValue(type, rawValue);
            var replaced = false;

            foreach (var effect in card.effects)
            {
                if (effect == null || effect.type != type)
                    continue;

                effect.value = value;
                replaced = true;
            }

            if (replaced)
                return;

            card.effects.Add(new GeneratedCardEffect
            {
                type = type,
                value = value,
                target = DamageTargetType.Single
            });
        }

        private static bool HasEffect(GeneratedCardData card, CardEffectType type)
        {
            foreach (var effect in card.effects)
            {
                if (effect != null && effect.type == type)
                    return true;
            }

            return false;
        }

        private static void RemoveEffectType(GeneratedCardData card, CardEffectType type)
        {
            for (var i = card.effects.Count - 1; i >= 0; i--)
            {
                if (card.effects[i] != null && card.effects[i].type == type)
                    card.effects.RemoveAt(i);
            }
        }

        private static void ReplaceEffectType(GeneratedCardData card, CardEffectType from, CardEffectType to)
        {
            foreach (var effect in card.effects)
            {
                if (effect == null || effect.type != from)
                    continue;

                effect.type = to;
                effect.value = TableManager.ClampEffectValue(to, effect.value);
            }
        }

        private static int? TryReadInt(Regex regex, string text)
        {
            var match = regex.Match(text);
            if (!match.Success)
                return null;

            for (var i = 1; i < match.Groups.Count; i++)
            {
                if (!match.Groups[i].Success)
                    continue;

                if (int.TryParse(match.Groups[i].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                    return value;
            }

            return null;
        }
    }
}
