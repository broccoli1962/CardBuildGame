using System;
using System.Collections.Generic;
using TableData;
using UnityEngine;

[Serializable]
public partial class MapNodeTypeData : IData
{
    public MapNodeType node_type => _node_type;
    [SerializeField] private MapNodeType _node_type;

    public string name_key => _name_key;
    [SerializeField] private string _name_key;

    public string icon_key => _icon_key;
    [SerializeField] private string _icon_key;

    public string desc_key => _desc_key;
    [SerializeField] private string _desc_key;

    public bool has_battle => _has_battle;
    [SerializeField] private bool _has_battle;

    public int gen_count => _gen_count;
    [SerializeField] private int _gen_count;

    public int min_floor => _min_floor;
    [SerializeField] private int _min_floor;

    public int max_per_map => _max_per_map;
    [SerializeField] private int _max_per_map;

    public string color_hex => _color_hex;
    [SerializeField] private string _color_hex;

	public void SetData(List<string> data)
	{
		if (data.Count > 0 && !string.IsNullOrEmpty(data[0]))
		{
			_node_type = MapNodeType.Parse<MapNodeType>(data[0]);
		}
		_name_key = data.Count > 1 ? data[1] : string.Empty;
		_icon_key = data.Count > 2 ? data[2] : string.Empty;
		_desc_key = data.Count > 3 ? data[3] : string.Empty;
		if (data.Count > 4 && !string.IsNullOrEmpty(data[4]))
		{
			_has_battle = bool.Parse(data[4]);
		}
		if (data.Count > 5 && !string.IsNullOrEmpty(data[5]))
		{
			_gen_count = int.Parse(data[5]);
		}
		if (data.Count > 6 && !string.IsNullOrEmpty(data[6]))
		{
			_min_floor = int.Parse(data[6]);
		}
		if (data.Count > 7 && !string.IsNullOrEmpty(data[7]))
		{
			_max_per_map = int.Parse(data[7]);
		}
		_color_hex = data.Count > 8 ? data[8] : string.Empty;
	}
}
