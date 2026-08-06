/// <summary>
/// 적의 다음 턴 행동(Intent) 미리보기 데이터입니다.
/// </summary>
public readonly struct EnemyActionPreview
{
    public EnemyActionType ActionType { get; }
    public int Value { get; }
    public string DisplayKey { get; }

    public bool IsValid => ActionType != EnemyActionType.None;

    public EnemyActionPreview(EnemyActionType actionType, int value, string displayKey)
    {
        ActionType = actionType;
        Value = value;
        DisplayKey = displayKey ?? string.Empty;
    }

    public static EnemyActionPreview FromAction(MonsterActionData action)
    {
        if (action == null)
            return default;

        return new EnemyActionPreview(action.action_type, action.value, action.display_key);
    }
}
