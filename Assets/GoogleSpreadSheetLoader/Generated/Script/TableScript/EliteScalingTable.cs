using System.Collections.Generic;
using TableData;
using UnityEngine;

[CreateAssetMenu(fileName = "EliteScalingTable", menuName = "Tables/EliteScalingTable")]
public class EliteScalingTable : ScriptableObject, ITable
{
    public List<EliteScalingData> dataList = new List<EliteScalingData>();

	public void SetData(List<List<string>> data)
	{
		dataList = new List<EliteScalingData>();
		foreach (var item in data)
		{
			EliteScalingData newData = new();
			newData.SetData(item);
			dataList.Add(newData);
		}
	}
}
