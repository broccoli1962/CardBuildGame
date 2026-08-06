using System;
using System.Collections.Generic;
using Backend.Object.GameSystems.Llm;
using Backend.Object.Management;
using Newtonsoft.Json;
using UnityEngine;

namespace Backend.Object.GameSystems.Gameplay
{
    public readonly struct CardEffect
    {
        public CardEffectType Type { get; }
        public int Value { get; }
        public DamageTargetType Target { get; }

        public CardEffect(CardEffectType type, int value, DamageTargetType target = DamageTargetType.Single)
        {
            Type = type;
            Value = value;
            Target = type == CardEffectType.DEAL_DAMAGE ? target : DamageTargetType.Single;
        }
    }

    public sealed class RuntimeCard
    {
        private static int _nextUid;

        public int Uid { get; }
        public string CardId { get; }
        public string NameKey { get; }
        public string DescKey { get; }
        public string DisplayName { get; }
        public string DisplayDescription { get; }
        public CardType CardType { get; }
        public string IconKey { get; }
        public int ManaCost { get; private set; }
        public bool IsGenerated { get; }
        public float PowerScore { get; }
        public IReadOnlyList<CardEffect> Effects { get; }

        private RuntimeCard(
            int uid,
            string cardId,
            string nameKey,
            string descKey,
            string displayName,
            string displayDescription,
            CardType cardType,
            string iconKey,
            int manaCost,
            bool isGenerated,
            float powerScore,
            IReadOnlyList<CardEffect> effects)
        {
            Uid = uid;
            CardId = cardId;
            NameKey = nameKey;
            DescKey = descKey;
            DisplayName = displayName;
            DisplayDescription = displayDescription;
            CardType = cardType;
            IconKey = string.IsNullOrEmpty(iconKey) ? GetDefaultIconKey(cardType) : iconKey;
            ManaCost = manaCost;
            IsGenerated = isGenerated;
            PowerScore = powerScore;
            Effects = effects;
        }

        [Serializable]
        private sealed class EffectPayload
        {
            public string type;
            public int value;
            public string target;
        }

        /// <summary>
        /// BaseCardData 테이블 행으로부터 런타임 카드 인스턴스를 생성합니다.
        /// </summary>
        public static RuntimeCard CreateFromBaseCard(BaseCardData data)
        {
            if (data == null)
            {
                Debug.LogError("[RuntimeCard] BaseCardData is null.");
                return null;
            }

            var effects = ParseEffectJson(data.effect_json, data.card_id);

            return new RuntimeCard(
                ++_nextUid,
                data.card_id,
                data.name_key,
                data.desc_key,
                displayName: null,
                displayDescription: null,
                data.card_type,
                data.icon_key,
                data.mana_cost,
                isGenerated: false,
                powerScore: 0f,
                effects);
        }

        /// <summary>
        /// LLM/Fallback 생성 카드로부터 런타임 카드 인스턴스를 생성합니다.
        /// </summary>
        public static RuntimeCard CreateFromGenerated(GeneratedCardData data)
        {
            if (data == null)
            {
                Debug.LogError("[RuntimeCard] GeneratedCardData is null.");
                return null;
            }

            var effects = new List<CardEffect>();
            if (data.effects != null)
            {
                foreach (var effect in data.effects)
                {
                    if (effect == null || !TableManager.IsAllowedEffectType(effect.type))
                        continue;

                    var target = effect.type == CardEffectType.DEAL_DAMAGE
                        ? effect.target
                        : DamageTargetType.Single;

                    effects.Add(new CardEffect(
                        effect.type,
                        TableManager.ClampEffectValue(effect.type, effect.value),
                        target));
                }
            }

            if (effects.Count == 0)
            {
                Debug.LogError($"[RuntimeCard] Generated card has no valid effects: {data.card_id}");
                return null;
            }

            var power = data.power_score > 0f
                ? data.power_score
                : CardGenerationService.CalculatePowerScore(data);

            var description = CardDescriptionFormatter.EnsureDamageFormKeywords(data.description, data.effects);

            return new RuntimeCard(
                ++_nextUid,
                string.IsNullOrEmpty(data.card_id) ? $"gen_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}" : data.card_id,
                nameKey: string.Empty,
                descKey: string.Empty,
                displayName: data.name,
                displayDescription: description,
                data.card_type,
                GetDefaultIconKey(data.card_type),
                data.mana_cost,
                isGenerated: true,
                powerScore: power,
                effects);
        }

        /// <summary>
        /// 카드 타입에 대응하는 Addressables 아이콘 키를 반환합니다.
        /// </summary>
        public static string GetDefaultIconKey(CardType cardType)
        {
            return cardType switch
            {
                CardType.Defense => "Icon_Shield",
                CardType.Heal => "Icon_Heal",
                CardType.Special => "Icon_Special",
                _ => "Icon_Swords",
            };
        }

        /// <summary>
        /// 카드 타입에 대응하는 Addressables 프레임 키를 반환합니다.
        /// </summary>
        public static string GetFrameKey(CardType cardType)
        {
            return cardType switch
            {
                CardType.Defense => "CardFrame_Defense",
                CardType.Heal => "CardFrame_Heal",
                CardType.Special => "CardFrame_Special",
                _ => "CardFrame_Attack",
            };
        }

        /// <summary>
        /// 각인 등으로 마나 비용을 변경합니다.
        /// </summary>
        public void SetManaCost(int manaCost)
        {
            ManaCost = Mathf.Max(0, manaCost);
        }

        private static List<CardEffect> ParseEffectJson(string effectJson, string cardId)
        {
            var effects = new List<CardEffect>();
            if (string.IsNullOrWhiteSpace(effectJson))
                return effects;

            try
            {
                var payloads = JsonConvert.DeserializeObject<EffectPayload[]>(effectJson);
                if (payloads == null)
                    return effects;

                foreach (var payload in payloads)
                {
                    if (payload == null || string.IsNullOrWhiteSpace(payload.type))
                        continue;

                    if (!Enum.TryParse(payload.type, true, out CardEffectType effectType))
                        continue;

                    if (!TableManager.IsAllowedEffectType(effectType))
                        continue;

                    var target = effectType == CardEffectType.DEAL_DAMAGE
                        ? ParseDamageTarget(payload.target)
                        : DamageTargetType.Single;

                    effects.Add(new CardEffect(
                        effectType,
                        TableManager.ClampEffectValue(effectType, payload.value),
                        target));
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RuntimeCard] effect_json parse failed for '{cardId}': {ex.Message}");
            }

            return effects;
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
    }
}
