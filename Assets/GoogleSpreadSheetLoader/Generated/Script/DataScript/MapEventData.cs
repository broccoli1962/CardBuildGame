using System;
using System.Collections.Generic;
using TableData;
using UnityEngine;

[Serializable]
public partial class MapEventData : IData
{
    public string event_id => _event_id;
    [SerializeField] private string _event_id;

    public string name_key => _name_key;
    [SerializeField] private string _name_key;

    public string desc_key => _desc_key;
    [SerializeField] private string _desc_key;

    public int min_floor => _min_floor;
    [SerializeField] private int _min_floor;

    public string option_a_key => _option_a_key;
    [SerializeField] private string _option_a_key;

    public string option_a_json => _option_a_json;
    [SerializeField] private string _option_a_json;

    public string option_b_key => _option_b_key;
    [SerializeField] private string _option_b_key;

    public string option_b_json => _option_b_json;
    [SerializeField] private string _option_b_json;

	public void SetData(List<string> data)
	{
		_event_id = data.Count > 0 ? data[0] : string.Empty;
		_name_key = data.Count > 1 ? data[1] : string.Empty;
		_desc_key = data.Count > 2 ? data[2] : string.Empty;
		if (data.Count > 3 && !string.IsNullOrEmpty(data[3]))
		{
			_min_floor = int.Parse(data[3]);
		}
		_option_a_key = data.Count > 4 ? data[4] : string.Empty;
		_option_a_json = data.Count > 5 ? data[5] : string.Empty;
		_option_b_key = data.Count > 6 ? data[6] : string.Empty;
		_option_b_json = data.Count > 7 ? data[7] : string.Empty;
	}
}
