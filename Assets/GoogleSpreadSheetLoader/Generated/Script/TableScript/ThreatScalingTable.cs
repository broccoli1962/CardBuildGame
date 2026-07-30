using System.Collections.Generic;
using TableData;
using UnityEngine;

[CreateAssetMenu(fileName = "ThreatScalingTable", menuName = "Tables/ThreatScalingTable")]
public class ThreatScalingTable : ScriptableObject, ITable
{
    public List<ThreatScalingData> dataList = new List<ThreatScalingData>();

	public void SetData(List<List<string>> data)
	{
		dataList = new List<ThreatScalingData>();
		foreach (var item in data)
		{
			ThreatScalingData newData = new();
			newData.SetData(item);
			dataList.Add(newData);
		}
	}
}
