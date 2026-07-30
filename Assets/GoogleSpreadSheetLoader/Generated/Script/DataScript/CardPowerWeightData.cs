using System;
using System.Collections.Generic;
using TableData;
using UnityEngine;

[Serializable]
public partial class CardPowerWeightData : IData
{
    public CardEffectType effect_type => _effect_type;
    [SerializeField] private CardEffectType _effect_type;

    public float weight => _weight;
    [SerializeField] private float _weight;

    public float flat_bonus => _flat_bonus;
    [SerializeField] private float _flat_bonus;

	public void SetData(List<string> data)
	{
		if (data.Count > 0 && !string.IsNullOrEmpty(data[0]))
		{
			_effect_type = CardEffectType.Parse<CardEffectType>(data[0]);
		}
		if (data.Count > 1 && !string.IsNullOrEmpty(data[1]))
		{
			_weight = float.Parse(data[1]);
		}
		if (data.Count > 2 && !string.IsNullOrEmpty(data[2]))
		{
			_flat_bonus = float.Parse(data[2]);
		}
	}
}
