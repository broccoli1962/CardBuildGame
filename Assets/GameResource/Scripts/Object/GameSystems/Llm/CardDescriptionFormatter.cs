using System.Text;
using Backend.Object.GameSystems.Gameplay;

namespace Backend.Object.GameSystems.Llm
{
    /// <summary>
    /// 생성 카드 설명의 피해 형태 키워드를 TMP 리치텍스트로 강조합니다.
    /// </summary>
    public static class CardDescriptionFormatter
    {
        public const string SingleKeyword = "단일";
        public const string AoeKeyword = "광역";

        private const string SingleColor = "#2EC4B6";
        private const string AoeColor = "#F4A261";

        /// <summary>
        /// 설명문 안의 단일/광역 키워드에 색상을 입힙니다.
        /// </summary>
        public static string ColorizeDamageFormKeywords(string description)
        {
            if (string.IsNullOrEmpty(description))
                return description;

            return description
                .Replace(AoeKeyword, $"<color={AoeColor}>{AoeKeyword}</color>")
                .Replace(SingleKeyword, $"<color={SingleColor}>{SingleKeyword}</color>");
        }

        /// <summary>
        /// DEAL_DAMAGE 이펙트 구성을 보고 설명에 단일/광역 키워드가 없으면 덧붙입니다.
        /// </summary>
        public static string EnsureDamageFormKeywords(string description, System.Collections.Generic.IReadOnlyList<GeneratedCardEffect> effects)
        {
            CollectDamageForms(effects, out var hasSingle, out var hasAoe);
            if (!hasSingle && !hasAoe)
                return description ?? string.Empty;

            var text = description ?? string.Empty;
            var missing = new StringBuilder();

            if (hasSingle && !text.Contains(SingleKeyword))
                missing.Append(missing.Length == 0 ? SingleKeyword : $"·{SingleKeyword}");
            if (hasAoe && !text.Contains(AoeKeyword))
                missing.Append(missing.Length == 0 ? AoeKeyword : $"·{AoeKeyword}");

            if (missing.Length == 0)
                return text;

            if (string.IsNullOrWhiteSpace(text))
                return missing.ToString();

            return $"{text.TrimEnd()} ({missing})";
        }

        public static void CollectDamageForms(
            System.Collections.Generic.IReadOnlyList<GeneratedCardEffect> effects,
            out bool hasSingle,
            out bool hasAoe)
        {
            hasSingle = false;
            hasAoe = false;
            if (effects == null)
                return;

            foreach (var effect in effects)
            {
                if (effect == null || effect.type != CardEffectType.DEAL_DAMAGE)
                    continue;

                if (effect.target == DamageTargetType.Aoe)
                    hasAoe = true;
                else
                    hasSingle = true;
            }
        }
    }
}
