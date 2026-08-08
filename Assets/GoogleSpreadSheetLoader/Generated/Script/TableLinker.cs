using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace TableData
{
    [CreateAssetMenu(fileName = "TableLinker", menuName = "Tables/TableLinker")]
    public class TableLinker : ScriptableObject
    {
		 public StageTable StageTable;
		 public MonsterActionTable MonsterActionTable;
		 public CardPowerWeightTable CardPowerWeightTable;
		 public ThreatScalingTable ThreatScalingTable;
		 public BaseCardTable BaseCardTable;
		 public CardEffectTypeTable CardEffectTypeTable;
		 public MapTemplateTable MapTemplateTable;
		 public RestOptionTable RestOptionTable;
		 public EliteScalingTable EliteScalingTable;
		 public TreasureOptionTable TreasureOptionTable;
		 public MonsterTable MonsterTable;
		 public MapNodeTypeTable MapNodeTypeTable;
		 public BalanceConstantTable BalanceConstantTable;
		 public MapEventTable MapEventTable;

    }
}