using System;
using System.Collections.Generic;
using TableData;
using UnityEngine;

[Serializable]
public partial class BalanceConstantData : IData
{
    public string key => _key;
    [SerializeField] private string _key;

    public float value => _value;
    [SerializeField] private float _value;

    public string desc => _desc;
    [SerializeField] private string _desc;

	public void SetData(List<string> data)
	{
		_key = data.Count > 0 ? data[0] : string.Empty;
		if (data.Count > 1 && !string.IsNullOrEmpty(data[1]))
		{
			_value = float.Parse(data[1]);
		}
		_desc = data.Count > 2 ? data[2] : string.Empty;
	}
}
