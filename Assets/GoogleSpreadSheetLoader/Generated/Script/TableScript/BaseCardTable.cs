using System.Collections.Generic;
using TableData;
using UnityEngine;

[CreateAssetMenu(fileName = "BaseCardTable", menuName = "Tables/BaseCardTable")]
public class BaseCardTable : ScriptableObject, ITable
{
    public List<BaseCardData> dataList = new List<BaseCardData>();

	public void SetData(List<List<string>> data)
	{
		dataList = new List<BaseCardData>();
		foreach (var item in data)
		{
			BaseCardData newData = new();
			newData.SetData(item);
			dataList.Add(newData);
		}
	}
}
