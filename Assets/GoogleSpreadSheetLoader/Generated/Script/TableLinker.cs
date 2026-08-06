using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace TableData
{
    [CreateAssetMenu(fileName = "TableLinker", menuName = "Tables/TableLinker")]
    public class TableLinker : ScriptableObject
    {
		 public MapNodeTypeTable MapNodeTypeTable;
		 public StageTable StageTable;
		 public MonsterActionTable MonsterActionTable;
		 public CardPowerWeightTable CardPowerWeightTable;
		 public ThreatScalingTable ThreatScalingTable;
		 public BaseCardTable BaseCardTable;
		 public CardEffectTypeTable CardEffectTypeTable;
		 public MapTemplateTable MapTemplateTable;
		 public MapEventTable MapEventTable;
		 public RestOptionTable RestOptionTable;
		 public EliteScalingTable EliteScalingTable;
		 public TreasureOptionTable TreasureOptionTable;
		 public BalanceConstantTable BalanceConstantTable;
		 public MonsterTable MonsterTable;

    }
}