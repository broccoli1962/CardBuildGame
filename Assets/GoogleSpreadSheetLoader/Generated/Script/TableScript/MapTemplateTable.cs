using System.Collections.Generic;
using TableData;
using UnityEngine;

[CreateAssetMenu(fileName = "MapTemplateTable", menuName = "Tables/MapTemplateTable")]
public class MapTemplateTable : ScriptableObject, ITable
{
    public List<MapTemplateData> dataList = new List<MapTemplateData>();

	public void SetData(List<List<string>> data)
	{
		dataList = new List<MapTemplateData>();
		foreach (var item in data)
		{
			MapTemplateData newData = new();
			newData.SetData(item);
			dataList.Add(newData);
		}
	}
}
