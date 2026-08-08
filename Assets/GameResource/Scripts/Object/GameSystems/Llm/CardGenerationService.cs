using System;
using System.Collections.Generic;
using System.Threading;
using Backend.Object.GameSystems.Gameplay;
using Backend.Object.Management;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace Backend.Object.GameSystems.Llm
{
    public readonly struct CardGenerationResult
    {
        public GeneratedCardData Card { get; }
        public bool UsedFallback { get; }

        public CardGenerationResult(GeneratedCardData card, bool usedFallback)
        {
            Card = card;
            UsedFallback = usedFallback;
        }
    }

    /// <summary>
    /// 로컬 LLM 카드 생성 오케스트레이터. 실패 시 Fallback 으로 게임 진행을 보장합니다.
    /// </summary>
    public static class CardGenerationService
    {
        /// <summary>
        /// 플레이어 콘셉트로 카드를 생성합니다. LLM 실패 시 Fallback 카드를 반환합니다.
        /// </summary>
        public static async UniTask<CardGenerationResult> GenerateAsync(
            string userConcept,
            float? temperatureOverride = null,
            CancellationToken cancellationToken = default)
        {
            var constraints = ConceptConstraintHelper.Parse(userConcept);
            var prompt = BuildPrompt(userConcept, constraints);
            var llmText = await LocalLlmClient.GenerateAsync(prompt, temperatureOverride, cancellationToken);

            if (TryParseCard(llmText, out var parsed))
            {
                FinalizeCard(parsed, constraints);
                return new CardGenerationResult(parsed, usedFallback: false);
            }

            if (!string.IsNullOrWhiteSpace(llmText))
                Debug.LogWarning($"[CardGenerationService] LLM response invalid. Using fallback.\n--- RAW ---\n{TrimForLog(llmText)}");
            else
                Debug.LogWarning("[CardGenerationService] LLM response empty. Using fallback generator.");

            var fallback = FallbackCardGenerator.Generate(userConcept);
            FinalizeCard(fallback, constraints);
            return new CardGenerationResult(fallback, usedFallback: true);
        }

        /// <summary>
        /// 생성 카드의 CPS(Card Power Score)를 계산합니다.
        /// </summary>
        public static float CalculatePowerScore(GeneratedCardData card)
        {
            if (card?.effects == null || card.effects.Count == 0)
                return 0f;

            var raw = 0f;
            foreach (var effect in card.effects)
            {
                if (effect == null || !TableManager.IsAllowedEffectType(effect.type))
                    continue;

                if (effect.type == CardEffectType.PLAYER_HP_CHANGE && effect.value < 0)
                {
                    var hpCost = effect.value == -999
                        ? TableManager.GetInt(TableManager.BalanceKey.PlayerStartMaxHp, 10) - 1
                        : Mathf.Abs(effect.value);
                    raw -= hpCost * TableManager.GetFloat(TableManager.BalanceKey.UsabilityFactorNormal, 1f);
                    continue;
                }

                raw += TableManager.GetEffectPowerScore(effect.type, effect.value);
            }

            var manaMultiplier = TableManager.GetFloat(TableManager.BalanceKey.ManaCostMultiplier, 1.5f);
            raw -= card.mana_cost * manaMultiplier;

            var maxMana = TableManager.GetInt(TableManager.BalanceKey.PlayerStartMaxMana, 5);
            var usability = card.mana_cost <= maxMana
                ? TableManager.GetFloat(TableManager.BalanceKey.UsabilityFactorNormal, 1f)
                : TableManager.GetFloat(TableManager.BalanceKey.UsabilityFactorManaOver, 0.5f);

            return Mathf.Max(0f, raw * usability);
        }

        private static string BuildPrompt(string userConcept, ConceptConstraintHelper.Constraints constraints)
        {
            var concept = string.IsNullOrWhiteSpace(userConcept) ? "균형 잡힌 카드" : userConcept.Trim();
            var requiredHint = ConceptConstraintHelper.BuildPromptHint(constraints);
            return
                "You are a card game designer. Return ONLY one JSON object with no markdown, no explanation, and no extra objects.\n" +
                "Schema:\n" +
                "{\n" +
                "  \"name\": \"8 chars max Korean name\",\n" +
                "  \"description\": \"100 chars max Korean description; include 단일 and/or 광역 when dealing damage\",\n" +
                "  \"card_type\": \"attack|defense|heal|special\",\n" +
                "  \"mana_cost\": 0-15 integer,\n" +
                "  \"effects\": [{\"type\":\"DEAL_DAMAGE|GAIN_SHIELD|HEAL_HP|PLAYER_HP_CHANGE|MANA_RECOVER|MAX_MANA_CHANGE|SET_INVULNERABLE|FREE_NEXT_CARD|GAIN_MANA_FROM_HP\",\"value\":number,\"target\":\"single|aoe\"}]\n" +
                "}\n" +
                "Rules:\n" +
                "- Use only allowed effect types.\n" +
                "- If the player specifies a number for damage/mana/shield/heal, use EXACTLY that number.\n" +
                "- card_type defense MUST use GAIN_SHIELD (armor/block). Never use HEAL_HP for defense.\n" +
                "- card_type heal MUST use HEAL_HP. Never use GAIN_SHIELD for heal.\n" +
                "- 방어도/실드/shield = GAIN_SHIELD. 회복/힐/heal = HEAL_HP. Do not swap them.\n" +
                "- DEAL_DAMAGE must set target: single (one enemy) or aoe (all enemies). Default single if omitted.\n" +
                "- target is only meaningful for DEAL_DAMAGE; omit it for other types.\n" +
                "- If request implies area/splash/all enemies, use aoe and write 광역 in description.\n" +
                "- If request implies one enemy/focus, use single and write 단일 in description.\n" +
                "- A card may mix single and aoe DEAL_DAMAGE effects.\n" +
                "- DEAL_DAMAGE over 15 requires PLAYER_HP_CHANGE penalty.\n" +
                "- image_path is always assets/cards/joker.png.\n" +
                "- Stop immediately after the closing brace of the single JSON object.\n" +
                requiredHint +
                $"Player request: {concept}";
        }

        private static void FinalizeCard(GeneratedCardData card, ConceptConstraintHelper.Constraints constraints)
        {
            ConceptConstraintHelper.Apply(card, constraints);
            ConceptConstraintHelper.AlignCardTypeAndEffects(card);
            ApplyDamagePenaltyRule(card);

            var maxDesc = TableManager.GetInt(TableManager.BalanceKey.CardDescMaxLength, 100);
            card.description = Trim(
                CardDescriptionFormatter.EnsureDamageFormKeywords(card.description, card.effects),
                maxDesc,
                "AI가 생성한 카드");
            card.power_score = CalculatePowerScore(card);
        }

        private static bool TryParseCard(string llmText, out GeneratedCardData card)
        {
            card = null;
            if (string.IsNullOrWhiteSpace(llmText))
                return false;

            if (!TryExtractFirstJsonObject(llmText, out var json))
                return false;

            LlmCardPayload payload;
            try
            {
                payload = JsonConvert.DeserializeObject<LlmCardPayload>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CardGenerationService] JSON parse failed: {e.Message}");
                return false;
            }

            if (payload == null || payload.effects == null || payload.effects.Count == 0)
                return false;

            card = Sanitize(payload);
            return card.effects.Count > 0;
        }

        private static GeneratedCardData Sanitize(LlmCardPayload payload)
        {
            var maxName = TableManager.GetInt(TableManager.BalanceKey.CardNameMaxLength, 8);
            var maxDesc = TableManager.GetInt(TableManager.BalanceKey.CardDescMaxLength, 100);
            var maxMana = TableManager.GetInt(TableManager.BalanceKey.CardManaCostMax, 15);

            var card = new GeneratedCardData
            {
                card_id = $"gen_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                name = Trim(payload.name, maxName, "생성카드"),
                description = Trim(payload.description, maxDesc, "AI가 생성한 카드"),
                image_path = "assets/cards/joker.png",
                card_type = ParseCardType(payload.card_type),
                mana_cost = Mathf.Clamp(payload.mana_cost, 0, maxMana),
                is_generated = true,
                effects = new List<GeneratedCardEffect>()
            };

            foreach (var effect in payload.effects)
            {
                if (effect == null || string.IsNullOrWhiteSpace(effect.type))
                    continue;

                if (!Enum.TryParse(effect.type, true, out CardEffectType effectType))
                    continue;

                if (!TableManager.IsAllowedEffectType(effectType))
                    continue;

                var target = effectType == CardEffectType.DEAL_DAMAGE
                    ? ParseDamageTarget(effect.target)
                    : DamageTargetType.Single;

                card.effects.Add(new GeneratedCardEffect
                {
                    type = effectType,
                    value = TableManager.ClampEffectValue(effectType, effect.value),
                    target = target
                });
            }

            ApplyDamagePenaltyRule(card);
            card.description = Trim(
                CardDescriptionFormatter.EnsureDamageFormKeywords(card.description, card.effects),
                maxDesc,
                "AI가 생성한 카드");
            return card;
        }

        private static void ApplyDamagePenaltyRule(GeneratedCardData card)
        {
            var hasHighDamage = false;
            foreach (var effect in card.effects)
            {
                if (effect.type == CardEffectType.DEAL_DAMAGE && effect.value > 15)
                {
                    hasHighDamage = true;
                    break;
                }
            }

            if (!hasHighDamage)
                return;

            foreach (var effect in card.effects)
            {
                if (effect.type == CardEffectType.PLAYER_HP_CHANGE)
                    return;
            }

            card.effects.Add(new GeneratedCardEffect
            {
                type = CardEffectType.PLAYER_HP_CHANGE,
                value = TableManager.ClampEffectValue(CardEffectType.PLAYER_HP_CHANGE, -3)
            });
        }

        private static DamageTargetType ParseDamageTarget(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return DamageTargetType.Single;

            return raw.Trim().ToLowerInvariant() switch
            {
                "aoe" or "area" or "all" or "광역" => DamageTargetType.Aoe,
                _ => DamageTargetType.Single,
            };
        }

        private static CardType ParseCardType(string cardType)
        {
            return cardType?.ToLowerInvariant() switch
            {
                "defense" => CardType.Defense,
                "heal" => CardType.Heal,
                "special" => CardType.Special,
                _ => CardType.Attack
            };
        }

        private static string Trim(string value, int maxLength, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
                return fallback;

            value = value.Trim();
            return value.Length <= maxLength ? value : value[..maxLength];
        }

        /// <summary>
        /// 모델이 JSON을 여러 번 반복해도 첫 번째 완전한 객체만 추출한다.
        /// </summary>
        private static bool TryExtractFirstJsonObject(string text, out string json)
        {
            json = null;
            var start = text.IndexOf('{');
            if (start < 0)
                return false;

            var depth = 0;
            var inString = false;
            var escape = false;
            for (var i = start; i < text.Length; i++)
            {
                var c = text[i];
                if (inString)
                {
                    if (escape)
                    {
                        escape = false;
                        continue;
                    }

                    if (c == '\\')
                    {
                        escape = true;
                        continue;
                    }

                    if (c == '"')
                        inString = false;
                    continue;
                }

                switch (c)
                {
                    case '"':
                        inString = true;
                        break;
                    case '{':
                        depth++;
                        break;
                    case '}':
                        depth--;
                        if (depth == 0)
                        {
                            json = text.Substring(start, i - start + 1);
                            return true;
                        }

                        break;
                }
            }

            return false;
        }

        private static string TrimForLog(string value, int maxLength = 500)
        {
            value = value.Trim();
            return value.Length <= maxLength ? value : value[..maxLength] + "...";
        }

        [Serializable]
        private sealed class LlmCardPayload
        {
            public string name;
            public string description;
            public string card_type;
            public int mana_cost;
            public List<LlmEffectPayload> effects;
        }

        [Serializable]
        private sealed class LlmEffectPayload
        {
            public string type;
            public int value;
            public string target;
        }
    }
}
