// Auto Generate Code.
using System.Collections.Generic;

namespace Backend.AddressableKey
{
    public static class AddressableKeys
    {
        public static class InGame
        {
            private static readonly Dictionary<string, string> Keys = new Dictionary<string, string>()
            {
                { "CardController", "Assets/GameResource/Prefab/InGame/CardController.prefab" },
                { "MonsterController", "Assets/GameResource/Prefab/InGame/MonsterController.prefab" },
            };

            public static string Get<T>() => Keys.TryGetValue(typeof(T).Name, out var key) ? key : null;
            public static string Get(string keyName) => Keys.TryGetValue(keyName, out var key) ? key : null;
        }

        public static class Icons
        {
            private static readonly Dictionary<string, string> Keys = new Dictionary<string, string>()
            {
                { "Icons_Node_Event_png", "Icons/Node_Event.png" },
                { "Icons_Node_Battle_png", "Icons/Node_Battle.png" },
                { "Icons_Node_Rest_png", "Icons/Node_Rest.png" },
                { "Icons_Node_Treasure_png", "Icons/Node_Treasure.png" },
                { "Icons_Node_Boss_png", "Icons/Node_Boss.png" },
                { "Icons_Node_Elite_png", "Icons/Node_Elite.png" },
                { "Badge_Deck", "Icons/Badge_Deck.png" },
                { "Badge_ManaCost", "Icons/Badge_ManaCost.png" },
                { "Badge_Stage", "Icons/Badge_Stage.png" },
                { "Bar_EnemyHP", "Icons/Bar_EnemyHP.png" },
                { "Button_EndTurn", "Icons/Button_EndTurn.png" },
                { "Card_1_Defense", "Icons/Card_1_Defense.png" },
                { "Card_2_Attack", "Icons/Card_2_Attack.png" },
                { "Card_3_Attack", "Icons/Card_3_Attack.png" },
                { "Card_4_Attack", "Icons/Card_4_Attack.png" },
                { "Card_5_Defense", "Icons/Card_5_Defense.png" },
                { "Card_Attack", "Icons/Card_Attack.png" },
                { "Card_Defense", "Icons/Card_Defense.png" },
                { "CardFrame_Attack", "Icons/CardFrame_Attack.png" },
                { "CardFrame_Defense", "Icons/CardFrame_Defense.png" },
                { "CardFrame_Heal", "Icons/CardFrame_Heal.png" },
                { "CardFrame_Special", "Icons/CardFrame_Special.png" },
                { "Icon_EnemySkull", "Icons/Icon_EnemySkull.png" },
                { "Icon_Heal", "Icons/Icon_Heal.png" },
                { "Icon_Heart", "Icons/Icon_Heart.png" },
                { "Icon_Mana", "Icons/Icon_Mana.png" },
                { "Icon_Shield", "Icons/Icon_Shield.png" },
                { "Icon_Special", "Icons/Icon_Special.png" },
                { "Icon_Swords", "Icons/Icon_Swords.png" },
                { "Monster_DragonBoss", "Icons/Monster_DragonBoss.png" },
                { "Monster_Goblin", "Icons/Monster_Goblin.png" },
                { "Monster_OrcChief", "Icons/Monster_OrcChief.png" },
                { "Monster_Slime", "Icons/Monster_Slime.png" },
                { "Monster_WraithKnight", "Icons/Monster_WraithKnight.png" },
                { "Node_Battle", "Icons/Node_Battle.png" },
                { "Node_Boss", "Icons/Node_Boss.png" },
                { "Node_Elite", "Icons/Node_Elite.png" },
                { "Node_Event", "Icons/Node_Event.png" },
                { "Node_Rest", "Icons/Node_Rest.png" },
                { "Node_StageActive", "Icons/Node_StageActive.png" },
                { "Node_StageInactive", "Icons/Node_StageInactive.png" },
                { "Node_StageRow", "Icons/Node_StageRow.png" },
                { "Node_Treasure", "Icons/Node_Treasure.png" },
                { "Pip_HP", "Icons/Pip_HP.png" },
                { "Pip_Mana", "Icons/Pip_Mana.png" },
            };

            public static string Get<T>() => Keys.TryGetValue(typeof(T).Name, out var key) ? key : null;
            public static string Get(string keyName) => Keys.TryGetValue(keyName, out var key) ? key : null;
        }

        public static class UI
        {
            private static readonly Dictionary<string, string> Keys = new Dictionary<string, string>()
            {
                { "UI_MapPanel_prefab", "UI/MapPanel.prefab" },
                { "UI_CardCreationPanel_prefab", "UI/CardCreationPanel.prefab" },
                { "UI_MapPreviewPopup_prefab", "UI/MapPreviewPopup.prefab" },
                { "Card", "UI/Card.prefab" },
                { "CardCreationPanel", "UI/CardCreationPanel.prefab" },
                { "DeckInspectPopup", "UI/DeckInspectPopup.prefab" },
                { "GamePanel", "UI/GamePanel.prefab" },
                { "LoadingPanel", "UI/LoadingPanel.prefab" },
                { "LobbyScreenPanel", "UI/LobbyScreenPanel.prefab" },
                { "MapGraphView", "UI/MapGraphView.prefab" },
                { "MapNodeView", "UI/MapNodeView.prefab" },
                { "MapPanel", "UI/MapPanel.prefab" },
                { "MapPreviewPopup", "UI/MapPreviewPopup.prefab" },
                { "Monster", "UI/Monster.prefab" },
                { "UIRoot", "UI/UIRoot.prefab" },
                { "UI_DeckInspectPopup_prefab", "UI/DeckInspectPopup.prefab" },
            };

            public static string Get<T>() => Keys.TryGetValue(typeof(T).Name, out var key) ? key : null;
            public static string Get(string keyName) => Keys.TryGetValue(keyName, out var key) ? key : null;
        }

        public static class Sounds
        {
            private static readonly Dictionary<string, string> Keys = new Dictionary<string, string>()
            {
                { "AudioMixer", "Assets/GameResource/Sounds/AudioMixer.mixer" },
            };

            public static string Get<T>() => Keys.TryGetValue(typeof(T).Name, out var key) ? key : null;
            public static string Get(string keyName) => Keys.TryGetValue(keyName, out var key) ? key : null;
        }

    }
}
