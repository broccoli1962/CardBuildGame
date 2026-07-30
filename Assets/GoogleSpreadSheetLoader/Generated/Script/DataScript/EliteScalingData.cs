using System;
using System.Collections.Generic;
using TableData;
using UnityEngine;

[Serializable]
public partial class EliteScalingData : IData
{
    public int chapter => _chapter;
    [SerializeField] private int _chapter;

    public float hp_mult => _hp_mult;
    [SerializeField] private float _hp_mult;

    public float atk_mult => _atk_mult;
    [SerializeField] private float _atk_mult;

    public int extra_action_count => _extra_action_count;
    [SerializeField] private int _extra_action_count;

    public int bonus_max_hp => _bonus_max_hp;
    [SerializeField] private int _bonus_max_hp;

	public void SetData(List<string> data)
	{
		if (data.Count > 0 && !string.IsNullOrEmpty(data[0]))
		{
			_chapter = int.Parse(data[0]);
		}
		if (data.Count > 1 && !string.IsNullOrEmpty(data[1]))
		{
			_hp_mult = float.Parse(data[1]);
		}
		if (data.Count > 2 && !string.IsNullOrEmpty(data[2]))
		{
			_atk_mult = float.Parse(data[2]);
		}
		if (data.Count > 3 && !string.IsNullOrEmpty(data[3]))
		{
			_extra_action_count = int.Parse(data[3]);
		}
		if (data.Count > 4 && !string.IsNullOrEmpty(data[4]))
		{
			_bonus_max_hp = int.Parse(data[4]);
		}
	}
}
