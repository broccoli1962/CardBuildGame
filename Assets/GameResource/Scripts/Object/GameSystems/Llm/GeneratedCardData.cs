using System;
using System.Collections.Generic;
using Backend.Object.GameSystems.Gameplay;

namespace Backend.Object.GameSystems.Llm
{
    [Serializable]
    public sealed class GeneratedCardEffect
    {
        public CardEffectType type;
        public int value;
        /// <summary>
        /// DEAL_DAMAGE 전용. 그 외 타입에서는 무시됩니다.
        /// </summary>
        public DamageTargetType target = DamageTargetType.Single;
    }

    /// <summary>
    /// LLM 또는 Fallback 으로 생성된 런타임 카드 데이터.
    /// </summary>
    [Serializable]
    public sealed class GeneratedCardData
    {
        public string card_id;
        public string name;
        public string description;
        public string image_path = "assets/cards/joker.png";
        public CardType card_type = CardType.Attack;
        public int mana_cost;
        public bool is_generated = true;
        public List<GeneratedCardEffect> effects = new();
        public float power_score;
    }
}
