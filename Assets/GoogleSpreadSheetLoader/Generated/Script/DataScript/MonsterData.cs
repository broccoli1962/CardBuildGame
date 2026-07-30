using System;
using System.Collections.Generic;
using TableData;
using UnityEngine;

[Serializable]
public partial class MonsterData : IData
{
    public string monster_id => _monster_id;
    [SerializeField] private string _monster_id;

    public string name_key => _name_key;
    [SerializeField] private string _name_key;

    public int max_hp => _max_hp;
    [SerializeField] private int _max_hp;

    public int base_attack => _base_attack;
    [SerializeField] private int _base_attack;

    public string icon_key => _icon_key;
    [SerializeField] private string _icon_key;

    public bool is_boss => _is_boss;
    [SerializeField] private bool _is_boss;

	public void SetData(List<string> data)
	{
		_monster_id = data.Count > 0 ? data[0] : string.Empty;
		_name_key = data.Count > 1 ? data[1] : string.Empty;
		if (data.Count > 2 && !string.IsNullOrEmpty(data[2]))
		{
			_max_hp = int.Parse(data[2]);
		}
		if (data.Count > 3 && !string.IsNullOrEmpty(data[3]))
		{
			_base_attack = int.Parse(data[3]);
		}
		_icon_key = data.Count > 4 ? data[4] : string.Empty;
		if (data.Count > 5 && !string.IsNullOrEmpty(data[5]))
		{
			_is_boss = bool.Parse(data[5]);
		}
	}
}
