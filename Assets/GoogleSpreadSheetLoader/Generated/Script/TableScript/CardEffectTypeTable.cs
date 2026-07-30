using System.Collections.Generic;
using TableData;
using UnityEngine;

[CreateAssetMenu(fileName = "CardEffectTypeTable", menuName = "Tables/CardEffectTypeTable")]
public class CardEffectTypeTable : ScriptableObject, ITable
{
    public List<CardEffectTypeData> dataList = new List<CardEffectTypeData>();

	public void SetData(List<List<string>> data)
	{
		dataList = new List<CardEffectTypeData>();
		foreach (var item in data)
		{
			CardEffectTypeData newData = new();
			newData.SetData(item);
			dataList.Add(newData);
		}
	}
}
