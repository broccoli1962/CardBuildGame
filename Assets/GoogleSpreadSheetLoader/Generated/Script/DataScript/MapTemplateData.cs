using System;
using System.Collections.Generic;
using TableData;
using UnityEngine;

[Serializable]
public partial class MapTemplateData : IData
{
    public int chapter => _chapter;
    [SerializeField] private int _chapter;

    public int floor => _floor;
    [SerializeField] private int _floor;

    public int slot_count => _slot_count;
    [SerializeField] private int _slot_count;

    public string allowed_types => _allowed_types;
    [SerializeField] private string _allowed_types;

    public string type_weights => _type_weights;
    [SerializeField] private string _type_weights;

    public string forced_type => _forced_type;
    [SerializeField] private string _forced_type;

	public void SetData(List<string> data)
	{
		if (data.Count > 0 && !string.IsNullOrEmpty(data[0]))
		{
			_chapter = int.Parse(data[0]);
		}
		if (data.Count > 1 && !string.IsNullOrEmpty(data[1]))
		{
			_floor = int.Parse(data[1]);
		}
		if (data.Count > 2 && !string.IsNullOrEmpty(data[2]))
		{
			_slot_count = int.Parse(data[2]);
		}
		_allowed_types = data.Count > 3 ? data[3] : string.Empty;
		_type_weights = data.Count > 4 ? data[4] : string.Empty;
		_forced_type = data.Count > 5 ? data[5] : string.Empty;
	}
}
