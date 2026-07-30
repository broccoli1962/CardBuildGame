using System.Collections.Generic;
using TableData;
using UnityEngine;

[CreateAssetMenu(fileName = "MapEventTable", menuName = "Tables/MapEventTable")]
public class MapEventTable : ScriptableObject, ITable
{
    public List<MapEventData> dataList = new List<MapEventData>();

	public void SetData(List<List<string>> data)
	{
		dataList = new List<MapEventData>();
		foreach (var item in data)
		{
			MapEventData newData = new();
			newData.SetData(item);
			dataList.Add(newData);
		}
	}
}
