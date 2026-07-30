using System;
using System.Collections.Generic;
using Backend.Object.Management;
using UnityEngine;

namespace Backend.Object.GameSystems.Llm
{
    /// <summary>
    /// LLM 실패 시 규칙 기반으로 카드를 생성합니다 (기획서 3-1 Fallback).
    /// </summary>
    public static class FallbackCardGenerator
    {
        private static readonly string[] ExtremeKeywords =
        {
            "100", "즉사", "원킬", "최강", "무한", "신"
        };

        private static readonly string[] StrongKeywords =
        {
            "강력", "폭발", "엄청", "거대"
        };

        /// <summary>
        /// 플레이어 콘셉트를 분석해 규칙 기반 카드를 생성합니다.
        /// </summary>
        public static GeneratedCardData Generate(string userConcept)
        {
            var concept = userConcept ?? string.Empty;
            var tier = EvaluateRequestTier(concept);

            return tier switch
            {
                RequestTier.Extreme => CreateExtremeCard(concept),
                RequestTier.Strong => CreateStrongCard(concept),
                _ => CreateNormalCard(concept)
            };
        }

        private enum RequestTier
        {
            Normal,
            Strong,
            Extreme
        }

        private static RequestTier EvaluateRequestTier(string concept)
        {
            foreach (var keyword in ExtremeKeywords)
            {
                if (concept.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    return RequestTier.Extreme;
            }

            foreach (var keyword in StrongKeywords)
            {
                if (concept.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    return RequestTier.Strong;
            }

            return RequestTier.Normal;
        }

        private static GeneratedCardData CreateNormalCard(string concept)
        {
            var damage = TableManager.ClampEffectValue(CardEffectType.DEAL_DAMAGE, 6);
            return BuildCard(
                name: "기본 일격",
                description: TruncateDescription(string.IsNullOrWhiteSpace(concept) ? "적에게 피해를 입힌다." : concept),
                cardType: CardType.Attack,
                manaCost: 3,
                effects: new List<GeneratedCardEffect>
                {
                    new() { type = CardEffectType.DEAL_DAMAGE, value = damage }
                });
        }

        private static GeneratedCardData CreateStrongCard(string concept)
        {
            var damage = TableManager.ClampEffectValue(CardEffectType.DEAL_DAMAGE, 12);
            return BuildCard(
                name: "강화 타격",
                description: TruncateDescription(concept),
                cardType: CardType.Attack,
                manaCost: 5,
                effects: new List<GeneratedCardEffect>
                {
                    new() { type = CardEffectType.DEAL_DAMAGE, value = damage }
                });
        }

        private static GeneratedCardData CreateExtremeCard(string concept)
        {
            var damage = TableManager.ClampEffectValue(CardEffectType.DEAL_DAMAGE, 40);
            var hpCost = TableManager.ClampEffectValue(CardEffectType.PLAYER_HP_CHANGE, -999);

            return BuildCard(
                name: "극한 일격",
                description: TruncateDescription(concept),
                cardType: CardType.Attack,
                manaCost: TableManager.GetInt(TableManager.BalanceKey.CardManaCostMax, 15),
                effects: new List<GeneratedCardEffect>
                {
                    new() { type = CardEffectType.PLAYER_HP_CHANGE, value = hpCost },
                    new() { type = CardEffectType.DEAL_DAMAGE, value = damage }
                });
        }

        private static GeneratedCardData BuildCard(
            string name,
            string description,
            CardType cardType,
            int manaCost,
            List<GeneratedCardEffect> effects)
        {
            var maxMana = TableManager.GetInt(TableManager.BalanceKey.CardManaCostMax, 15);
            manaCost = Mathf.Clamp(manaCost, 0, maxMana);

            var card = new GeneratedCardData
            {
                card_id = $"gen_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                name = TruncateName(name),
                description = description,
                card_type = cardType,
                mana_cost = manaCost,
                is_generated = true,
                effects = effects
            };

            card.power_score = CardGenerationService.CalculatePowerScore(card);
            return card;
        }

        private static string TruncateName(string name)
        {
            var maxLen = TableManager.GetInt(TableManager.BalanceKey.CardNameMaxLength, 8);
            if (string.IsNullOrEmpty(name) || name.Length <= maxLen)
                return name;

            return name[..maxLen];
        }

        private static string TruncateDescription(string description)
        {
            var maxLen = TableManager.GetInt(TableManager.BalanceKey.CardDescMaxLength, 100);
            if (string.IsNullOrEmpty(description) || description.Length <= maxLen)
                return description;

            return description[..maxLen];
        }
    }
}
