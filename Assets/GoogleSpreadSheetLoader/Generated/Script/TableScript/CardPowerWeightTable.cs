using System.Collections.Generic;
using TableData;
using UnityEngine;

[CreateAssetMenu(fileName = "CardPowerWeightTable", menuName = "Tables/CardPowerWeightTable")]
public class CardPowerWeightTable : ScriptableObject, ITable
{
    public List<CardPowerWeightData> dataList = new List<CardPowerWeightData>();

	public void SetData(List<List<string>> data)
	{
		dataList = new List<CardPowerWeightData>();
		foreach (var item in data)
		{
			CardPowerWeightData newData = new();
			newData.SetData(item);
			dataList.Add(newData);
		}
	}
}
