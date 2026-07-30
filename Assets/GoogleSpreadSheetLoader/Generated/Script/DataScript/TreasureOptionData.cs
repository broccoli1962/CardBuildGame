using System;
using System.Collections.Generic;
using TableData;
using UnityEngine;

[Serializable]
public partial class TreasureOptionData : IData
{
    public string option_id => _option_id;
    [SerializeField] private string _option_id;

    public string name_key => _name_key;
    [SerializeField] private string _name_key;

    public string desc_key => _desc_key;
    [SerializeField] private string _desc_key;

    public string icon_key => _icon_key;
    [SerializeField] private string _icon_key;

    public TreasureOptionType effect_type => _effect_type;
    [SerializeField] private TreasureOptionType _effect_type;

    public int value => _value;
    [SerializeField] private int _value;

    public float threat_ratio => _threat_ratio;
    [SerializeField] private float _threat_ratio;

	public void SetData(List<string> data)
	{
		_option_id = data.Count > 0 ? data[0] : string.Empty;
		_name_key = data.Count > 1 ? data[1] : string.Empty;
		_desc_key = data.Count > 2 ? data[2] : string.Empty;
		_icon_key = data.Count > 3 ? data[3] : string.Empty;
		if (data.Count > 4 && !string.IsNullOrEmpty(data[4]))
		{
			_effect_type = TreasureOptionType.Parse<TreasureOptionType>(data[4]);
		}
		if (data.Count > 5 && !string.IsNullOrEmpty(data[5]))
		{
			_value = int.Parse(data[5]);
		}
		if (data.Count > 6 && !string.IsNullOrEmpty(data[6]))
		{
			_threat_ratio = float.Parse(data[6]);
		}
	}
}
