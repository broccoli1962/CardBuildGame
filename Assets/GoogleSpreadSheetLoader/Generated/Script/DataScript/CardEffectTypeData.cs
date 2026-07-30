using System;
using System.Collections.Generic;
using TableData;
using UnityEngine;

[Serializable]
public partial class CardEffectTypeData : IData
{
    public CardEffectType effect_type => _effect_type;
    [SerializeField] private CardEffectType _effect_type;

    public int min_value => _min_value;
    [SerializeField] private int _min_value;

    public int max_value => _max_value;
    [SerializeField] private int _max_value;

    public string description_key => _description_key;
    [SerializeField] private string _description_key;

    public bool is_bool => _is_bool;
    [SerializeField] private bool _is_bool;

    public int requires_penalty_over => _requires_penalty_over;
    [SerializeField] private int _requires_penalty_over;

	public void SetData(List<string> data)
	{
		if (data.Count > 0 && !string.IsNullOrEmpty(data[0]))
		{
			_effect_type = CardEffectType.Parse<CardEffectType>(data[0]);
		}
		if (data.Count > 1 && !string.IsNullOrEmpty(data[1]))
		{
			_min_value = int.Parse(data[1]);
		}
		if (data.Count > 2 && !string.IsNullOrEmpty(data[2]))
		{
			_max_value = int.Parse(data[2]);
		}
		_description_key = data.Count > 3 ? data[3] : string.Empty;
		if (data.Count > 4 && !string.IsNullOrEmpty(data[4]))
		{
			_is_bool = bool.Parse(data[4]);
		}
		if (data.Count > 5 && !string.IsNullOrEmpty(data[5]))
		{
			_requires_penalty_over = int.Parse(data[5]);
		}
	}
}
