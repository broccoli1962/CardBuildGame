using System.Collections.Generic;
using TableData;
using UnityEngine;

[CreateAssetMenu(fileName = "MonsterActionTable", menuName = "Tables/MonsterActionTable")]
public class MonsterActionTable : ScriptableObject, ITable
{
    public List<MonsterActionData> dataList = new List<MonsterActionData>();

	public void SetData(List<List<string>> data)
	{
		dataList = new List<MonsterActionData>();
		foreach (var item in data)
		{
			MonsterActionData newData = new();
			newData.SetData(item);
			dataList.Add(newData);
		}
	}
}
