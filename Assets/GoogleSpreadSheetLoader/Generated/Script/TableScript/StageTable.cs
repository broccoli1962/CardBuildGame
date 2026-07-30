using System.Collections.Generic;
using TableData;
using UnityEngine;

[CreateAssetMenu(fileName = "StageTable", menuName = "Tables/StageTable")]
public class StageTable : ScriptableObject, ITable
{
    public List<StageData> dataList = new List<StageData>();

	public void SetData(List<List<string>> data)
	{
		dataList = new List<StageData>();
		foreach (var item in data)
		{
			StageData newData = new();
			newData.SetData(item);
			dataList.Add(newData);
		}
	}
}
