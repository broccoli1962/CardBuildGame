using System.Collections.Generic;
using TableData;
using UnityEngine;

[CreateAssetMenu(fileName = "MapNodeTypeTable", menuName = "Tables/MapNodeTypeTable")]
public class MapNodeTypeTable : ScriptableObject, ITable
{
    public List<MapNodeTypeData> dataList = new List<MapNodeTypeData>();

	public void SetData(List<List<string>> data)
	{
		dataList = new List<MapNodeTypeData>();
		foreach (var item in data)
		{
			MapNodeTypeData newData = new();
			newData.SetData(item);
			dataList.Add(newData);
		}
	}
}
