namespace Backend.Object.GameSystems.Gameplay
{
    public enum BattleVfxType
    {
        Attack,
        Defend,
        Heal,
        /// <summary>적이 플레이어를 공격할 때 플레이어 측 연출.</summary>
        EnemyAttack,
        /// <summary>적이 방어도를 얻을 때 몬스터 측 연출.</summary>
        EnemyDefend,
    }
}
