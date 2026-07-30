using System;
using System.Collections.Generic;
using TableData;
using UnityEngine;

[Serializable]
public partial class MonsterActionData : IData
{
    public string action_id => _action_id;
    [SerializeField] private string _action_id;

    public string monster_id => _monster_id;
    [SerializeField] private string _monster_id;

    public string turn_cycle => _turn_cycle;
    [SerializeField] private string _turn_cycle;

    public EnemyActionType action_type => _action_type;
    [SerializeField] private EnemyActionType _action_type;

    public int value => _value;
    [SerializeField] private int _value;

    public string display_key => _display_key;
    [SerializeField] private string _display_key;

    public int tier_min => _tier_min;
    [SerializeField] private int _tier_min;

	public void SetData(List<string> data)
	{
		_action_id = data.Count > 0 ? data[0] : string.Empty;
		_monster_id = data.Count > 1 ? data[1] : string.Empty;
		_turn_cycle = data.Count > 2 ? data[2] : string.Empty;
		if (data.Count > 3 && !string.IsNullOrEmpty(data[3]))
		{
			_action_type = EnemyActionType.Parse<EnemyActionType>(data[3]);
		}
		if (data.Count > 4 && !string.IsNullOrEmpty(data[4]))
		{
			_value = int.Parse(data[4]);
		}
		_display_key = data.Count > 5 ? data[5] : string.Empty;
		if (data.Count > 6 && !string.IsNullOrEmpty(data[6]))
		{
			_tier_min = int.Parse(data[6]);
		}
	}
}
