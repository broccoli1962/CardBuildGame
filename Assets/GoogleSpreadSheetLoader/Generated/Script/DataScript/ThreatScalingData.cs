using System;
using System.Collections.Generic;
using TableData;
using UnityEngine;

[Serializable]
public partial class ThreatScalingData : IData
{
    public int tier => _tier;
    [SerializeField] private int _tier;

    public float threat_min => _threat_min;
    [SerializeField] private float _threat_min;

    public float hp_mult => _hp_mult;
    [SerializeField] private float _hp_mult;

    public float atk_mult => _atk_mult;
    [SerializeField] private float _atk_mult;

    public EnemyActionType extra_action => _extra_action;
    [SerializeField] private EnemyActionType _extra_action;

    public string desc_key => _desc_key;
    [SerializeField] private string _desc_key;

	public void SetData(List<string> data)
	{
		if (data.Count > 0 && !string.IsNullOrEmpty(data[0]))
		{
			_tier = int.Parse(data[0]);
		}
		if (data.Count > 1 && !string.IsNullOrEmpty(data[1]))
		{
			_threat_min = float.Parse(data[1]);
		}
		if (data.Count > 2 && !string.IsNullOrEmpty(data[2]))
		{
			_hp_mult = float.Parse(data[2]);
		}
		if (data.Count > 3 && !string.IsNullOrEmpty(data[3]))
		{
			_atk_mult = float.Parse(data[3]);
		}
		if (data.Count > 4 && !string.IsNullOrEmpty(data[4]))
		{
			_extra_action = EnemyActionType.Parse<EnemyActionType>(data[4]);
		}
		_desc_key = data.Count > 5 ? data[5] : string.Empty;
	}
}
