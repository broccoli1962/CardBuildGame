namespace Backend.Object.GameSystems.Gameplay
{
    /// <summary>
    /// 피해 이펙트의 대상 형태. 멀티 적 전투 확장 시 BattleSystem이 분기합니다.
    /// </summary>
    public enum DamageTargetType
    {
        /// <summary>단일 대상.</summary>
        Single = 0,
        /// <summary>모든 적(광역).</summary>
        Aoe = 1,
    }
}
