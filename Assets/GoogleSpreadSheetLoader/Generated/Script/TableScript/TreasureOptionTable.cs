using System.Collections.Generic;
using TableData;
using UnityEngine;

[CreateAssetMenu(fileName = "TreasureOptionTable", menuName = "Tables/TreasureOptionTable")]
public class TreasureOptionTable : ScriptableObject, ITable
{
    public List<TreasureOptionData> dataList = new List<TreasureOptionData>();

	public void SetData(List<List<string>> data)
	{
		dataList = new List<TreasureOptionData>();
		foreach (var item in data)
		{
			TreasureOptionData newData = new();
			newData.SetData(item);
			dataList.Add(newData);
		}
	}
}
