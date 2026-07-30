using System.Collections.Generic;
using TableData;
using UnityEngine;

[CreateAssetMenu(fileName = "RestOptionTable", menuName = "Tables/RestOptionTable")]
public class RestOptionTable : ScriptableObject, ITable
{
    public List<RestOptionData> dataList = new List<RestOptionData>();

	public void SetData(List<List<string>> data)
	{
		dataList = new List<RestOptionData>();
		foreach (var item in data)
		{
			RestOptionData newData = new();
			newData.SetData(item);
			dataList.Add(newData);
		}
	}
}
