using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
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
        private static readonly Regex JsonBlockRegex = new(@"\{[\s\S]*\}", RegexOptions.Compiled);

        /// <summary>
        /// 플레이어 콘셉트로 카드를 생성합니다. LLM 실패 시 Fallback 카드를 반환합니다.
        /// </summary>
        public static async UniTask<CardGenerationResult> GenerateAsync(
            string userConcept,
            float? temperatureOverride = null,
            CancellationToken cancellationToken = default)
        {
            var prompt = BuildPrompt(userConcept);
            var llmText = await LocalLlmClient.GenerateAsync(prompt, temperatureOverride, cancellationToken);

            if (TryParseCard(llmText, out var parsed))
            {
                parsed.power_score = CalculatePowerScore(parsed);
                return new CardGenerationResult(parsed, usedFallback: false);
            }

            Debug.LogWarning("[CardGenerationService] LLM response invalid. Using fallback generator.");
            return new CardGenerationResult(FallbackCardGenerator.Generate(userConcept), usedFallback: true);
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

            var maxMana = TableManager.GetInt(TableManager.BalanceKey.PlayerStartMaxMana, 10);
            var usability = card.mana_cost <= maxMana
                ? TableManager.GetFloat(TableManager.BalanceKey.UsabilityFactorNormal, 1f)
                : TableManager.GetFloat(TableManager.BalanceKey.UsabilityFactorManaOver, 0.5f);

            return Mathf.Max(0f, raw * usability);
        }

        private static string BuildPrompt(string userConcept)
        {
            var concept = string.IsNullOrWhiteSpace(userConcept) ? "균형 잡힌 카드" : userConcept.Trim();
            return
                "You are a card game designer. Return ONLY one JSON object with no markdown.\n" +
                "Schema:\n" +
                "{\n" +
                "  \"name\": \"8 chars max Korean name\",\n" +
                "  \"description\": \"100 chars max Korean description\",\n" +
                "  \"card_type\": \"attack|defense|heal|special\",\n" +
                "  \"mana_cost\": 0-15 integer,\n" +
                "  \"effects\": [{\"type\":\"DEAL_DAMAGE|GAIN_SHIELD|HEAL_HP|PLAYER_HP_CHANGE|MANA_RECOVER|MAX_MANA_CHANGE|SET_INVULNERABLE|FREE_NEXT_CARD|GAIN_MANA_FROM_HP\",\"value\":number}]\n" +
                "}\n" +
                "Rules:\n" +
                "- Use only allowed effect types.\n" +
                "- DEAL_DAMAGE over 15 requires PLAYER_HP_CHANGE penalty.\n" +
                "- image_path is always assets/cards/joker.png.\n" +
                $"Player request: {concept}";
        }

        private static bool TryParseCard(string llmText, out GeneratedCardData card)
        {
            card = null;
            if (string.IsNullOrWhiteSpace(llmText))
                return false;

            var match = JsonBlockRegex.Match(llmText);
            if (!match.Success)
                return false;

            LlmCardPayload payload;
            try
            {
                payload = JsonConvert.DeserializeObject<LlmCardPayload>(match.Value);
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

                card.effects.Add(new GeneratedCardEffect
                {
                    type = effectType,
                    value = TableManager.ClampEffectValue(effectType, effect.value)
                });
            }

            ApplyDamagePenaltyRule(card);
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
        }
    }
}
