using System.Collections.Generic;
using TableData;
using UnityEngine;

[CreateAssetMenu(fileName = "MonsterTable", menuName = "Tables/MonsterTable")]
public class MonsterTable : ScriptableObject, ITable
{
    public List<MonsterData> dataList = new List<MonsterData>();

	public void SetData(List<List<string>> data)
	{
		dataList = new List<MonsterData>();
		foreach (var item in data)
		{
			MonsterData newData = new();
			newData.SetData(item);
			dataList.Add(newData);
		}
	}
}
