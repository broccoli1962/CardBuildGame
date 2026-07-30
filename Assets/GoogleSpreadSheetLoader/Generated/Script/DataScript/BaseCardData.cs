using System;
using System.Collections.Generic;
using TableData;
using UnityEngine;

[Serializable]
public partial class BaseCardData : IData
{
    public string card_id => _card_id;
    [SerializeField] private string _card_id;

    public string name_key => _name_key;
    [SerializeField] private string _name_key;

    public string desc_key => _desc_key;
    [SerializeField] private string _desc_key;

    public CardType card_type => _card_type;
    [SerializeField] private CardType _card_type;

    public int mana_cost => _mana_cost;
    [SerializeField] private int _mana_cost;

    public string effect_json => _effect_json;
    [SerializeField] private string _effect_json;

    public int count => _count;
    [SerializeField] private int _count;

    public string icon_key => _icon_key;
    [SerializeField] private string _icon_key;

	public void SetData(List<string> data)
	{
		_card_id = data.Count > 0 ? data[0] : string.Empty;
		_name_key = data.Count > 1 ? data[1] : string.Empty;
		_desc_key = data.Count > 2 ? data[2] : string.Empty;
		if (data.Count > 3 && !string.IsNullOrEmpty(data[3]))
		{
			_card_type = CardType.Parse<CardType>(data[3]);
		}
		if (data.Count > 4 && !string.IsNullOrEmpty(data[4]))
		{
			_mana_cost = int.Parse(data[4]);
		}
		_effect_json = data.Count > 5 ? data[5] : string.Empty;
		if (data.Count > 6 && !string.IsNullOrEmpty(data[6]))
		{
			_count = int.Parse(data[6]);
		}
		_icon_key = data.Count > 7 ? data[7] : string.Empty;
	}
}
