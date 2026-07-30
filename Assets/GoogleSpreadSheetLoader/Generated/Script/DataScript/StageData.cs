using System;
using System.Collections.Generic;
using TableData;
using UnityEngine;

[Serializable]
public partial class StageData : IData
{
    public string stage_id => _stage_id;
    [SerializeField] private string _stage_id;

    public int chapter => _chapter;
    [SerializeField] private int _chapter;

    public int floor => _floor;
    [SerializeField] private int _floor;

    public string monster_pool => _monster_pool;
    [SerializeField] private string _monster_pool;

    public bool is_boss => _is_boss;
    [SerializeField] private bool _is_boss;

	public void SetData(List<string> data)
	{
		_stage_id = data.Count > 0 ? data[0] : string.Empty;
		if (data.Count > 1 && !string.IsNullOrEmpty(data[1]))
		{
			_chapter = int.Parse(data[1]);
		}
		if (data.Count > 2 && !string.IsNullOrEmpty(data[2]))
		{
			_floor = int.Parse(data[2]);
		}
		_monster_pool = data.Count > 3 ? data[3] : string.Empty;
		if (data.Count > 4 && !string.IsNullOrEmpty(data[4]))
		{
			_is_boss = bool.Parse(data[4]);
		}
	}
}
