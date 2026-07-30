using System.Collections.Generic;
using TableData;
using UnityEngine;

[CreateAssetMenu(fileName = "BalanceConstantTable", menuName = "Tables/BalanceConstantTable")]
public class BalanceConstantTable : ScriptableObject, ITable
{
    public List<BalanceConstantData> dataList = new List<BalanceConstantData>();

	public void SetData(List<List<string>> data)
	{
		dataList = new List<BalanceConstantData>();
		foreach (var item in data)
		{
			BalanceConstantData newData = new();
			newData.SetData(item);
			dataList.Add(newData);
		}
	}
}
