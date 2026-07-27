// Auto Generate Code.
using System.Collections.Generic;

namespace Backend.AddressableKey
{
    public static class AddressableKeys
    {
        public static class UI
        {
            private static readonly Dictionary<string, string> Keys = new Dictionary<string, string>()
            {
                { "BottomNavBar", "UI/BottomNavBar.prefab" },
                { "SegmentedGaugeBar", "UI/Common/SegmentedGaugeBar.prefab" },
                { "SingleGaugeBar", "UI/Common/SingleGaugeBar.prefab" },
                { "DifficultyButton", "UI/DifficultyButton.prefab" },
                { "Icon", "UI/Icon.prefab" },
                { "InGameBottomHud", "UI/InGameBottomHud.prefab" },
                { "InGameTopHud", "UI/InGameTopHud.prefab" },
                { "LobbyPanel", "UI/LobbyPanel.prefab" },
                { "Monster", "UI/Monster.prefab" },
                { "OptionPopup", "UI/OptionPopup.prefab" },
                { "QuestBox", "UI/QuestBox.prefab" },
                { "QuestDetailPanel", "UI/QuestDetailPanel.prefab" },
                { "TapIcon", "UI/TapIcon.prefab" },
                { "TopNavBar", "UI/TopNavBar.prefab" },
                { "UIBlocker", "UI/UIBlocker.prefab" },
                { "UIRoot", "UI/UIRoot.prefab" },
                { "UnitBox", "UI/UnitBox.prefab" },
                { "UnitDetailPanel", "UI/UnitDetailPanel.prefab" },
                { "UnitPartyPanel", "UI/UnitPartyPanel.prefab" },
                { "UnitPartySelectBox", "UI/UnitPartySelectBox.prefab" },
                { "UnitPartySelectPanel", "UI/UnitPartySelectPanel.prefab" },
            };

            public static string Get<T>() => Keys.TryGetValue(typeof(T).Name, out var key) ? key : null;
            public static string Get(string keyName) => Keys.TryGetValue(keyName, out var key) ? key : null;
        }

        public static class InGame
        {
            private static readonly Dictionary<string, string> Keys = new Dictionary<string, string>()
            {
                { "AudioSource", "InGame/AudioSource.prefab" },
                { "ChainLine", "InGame/ChainLine.prefab" },
                { "CharacterSlot", "InGame/CharacterSlot.prefab" },
                { "CharacterSlotController", "InGame/CharacterSlotController.prefab" },
                { "AttackFx_Fire", "InGame/FX/AttackFx_Fire.prefab" },
                { "AttackFx_Grass", "InGame/FX/AttackFx_Grass.prefab" },
                { "AttackFx_Light", "InGame/FX/AttackFx_Light.prefab" },
                { "AttackFx_Water", "InGame/FX/AttackFx_Water.prefab" },
                { "MonsterAttackFx", "InGame/FX/MonsterAttackFx.prefab" },
                { "MonsterController", "InGame/MonsterController.prefab" },
                { "Panel", "InGame/Panel.prefab" },
                { "PuzzleController", "InGame/PuzzleController.prefab" },
                { "TurnController", "InGame/TurnController.prefab" },
                { "GameScene", "Scenes/GameScene.unity" },
                { "LobbyScene", "Scenes/LobbyScene.unity" },
                { "CirclePanel", "Images/4panel/CirclePanel.png" },
                { "Square", "Images/4panel/Square.png" },
                { "boom", "Images/4panel_old/boom.aseprite" },
                { "crash_count", "Images/4panel_old/crash_count.aseprite" },
                { "empty_panel_fire", "Images/4panel_old/empty_panel_fire.aseprite" },
                { "empty_panel_grass", "Images/4panel_old/empty_panel_grass.aseprite" },
                { "empty_panel_light", "Images/4panel_old/empty_panel_light.aseprite" },
                { "empty_panel_water", "Images/4panel_old/empty_panel_water.aseprite" },
                { "large_empty_panel_grass", "Images/4panel_old/large_empty_panel_grass.aseprite" },
                { "large_empty_panel_light", "Images/4panel_old/large_empty_panel_light.aseprite" },
                { "large_empty_panel_red", "Images/4panel_old/large_empty_panel_red.aseprite" },
                { "large_empty_panel_water", "Images/4panel_old/large_empty_panel_water.aseprite" },
                { "panel0", "Images/4panel_old/panel0.png" },
                { "panel1", "Images/4panel_old/panel1.png" },
                { "panel2", "Images/4panel_old/panel2.png" },
                { "panel3", "Images/4panel_old/panel3.png" },
                { "panel4", "Images/4panel_old/panel4.png" },
                { "CharacterSlotOutLine", "Images/GameUI/Borders/CharacterSlotOutLine.png" },
                { "UICircleIconSmall", "Images/GameUI/Borders/UICircleIconSmall.png" },
                { "UICircleIconSmallFillMask", "Images/GameUI/Borders/UICircleIconSmallFillMask.png" },
                { "UIPanelIcon", "Images/GameUI/Borders/UIPanelIcon.png" },
                { "UIPanelIcon2", "Images/GameUI/Borders/UIPanelIcon2.png" },
                { "UIPanelIconFillMask", "Images/GameUI/Borders/UIPanelIconFillMask.png" },
                { "UIPanelIconFillMask2", "Images/GameUI/Borders/UIPanelIconFillMask2.png" },
                { "UISquareLine", "Images/GameUI/Borders/UISquareLine.png" },
                { "UISquareLine2", "Images/GameUI/Borders/UISquareLine2.png" },
                { "UISquareLineMask", "Images/GameUI/Borders/UISquareLineMask.png" },
                { "UISquareLineMask2", "Images/GameUI/Borders/UISquareLineMask2.png" },
                { "count_base", "Images/GameUI/count_base.aseprite" },
                { "Symbol_Fire_NoBorder_NoBase_SDF", "Images/GameUI/Elements/Symbol_Fire_NoBorder_NoBase_SDF.png" },
                { "Symbol_Fire_NoBorder_SDF", "Images/GameUI/Elements/Symbol_Fire_NoBorder_SDF.png" },
                { "Symbol_Fire_SDF", "Images/GameUI/Elements/Symbol_Fire_SDF.png" },
                { "Symbol_Grass_NoBorder_NoBase_SDF", "Images/GameUI/Elements/Symbol_Grass_NoBorder_NoBase_SDF.png" },
                { "Symbol_Grass_NoBorder_SDF", "Images/GameUI/Elements/Symbol_Grass_NoBorder_SDF.png" },
                { "Symbol_Grass_SDF", "Images/GameUI/Elements/Symbol_Grass_SDF.png" },
                { "Symbol_Light_NoBorder_NoBase_SDF", "Images/GameUI/Elements/Symbol_Light_NoBorder_NoBase_SDF.png" },
                { "Symbol_Light_NoBorder_SDF", "Images/GameUI/Elements/Symbol_Light_NoBorder_SDF.png" },
                { "Symbol_Light_SDF", "Images/GameUI/Elements/Symbol_Light_SDF.png" },
                { "Symbol_Water_NoBorder_NoBase_SDF", "Images/GameUI/Elements/Symbol_Water_NoBorder_NoBase_SDF.png" },
                { "Symbol_Water_NoBorder_SDF", "Images/GameUI/Elements/Symbol_Water_NoBorder_SDF.png" },
                { "Symbol_Water_SDF", "Images/GameUI/Elements/Symbol_Water_SDF.png" },
                { "empty_healthbar", "Images/GameUI/empty_healthbar.aseprite" },
                { "EnemyBullet", "Images/GameUI/EnemyBullet.aseprite" },
                { "Floor", "Images/GameUI/Floor.png" },
                { "Btn_Album", "Images/GameUI/Icon/Btn_Album.png" },
                { "Btn_Back", "Images/GameUI/Icon/Btn_Back.png" },
                { "Btn_Home", "Images/GameUI/Icon/Btn_Home.png" },
                { "Btn_Mission", "Images/GameUI/Icon/Btn_Mission.png" },
                { "Btn_Pause", "Images/GameUI/Icon/Btn_Pause.png" },
                { "Btn_Play", "Images/GameUI/Icon/Btn_Play.png" },
                { "Btn_Plus", "Images/GameUI/Icon/Btn_Plus.png" },
                { "Btn_Shop", "Images/GameUI/Icon/Btn_Shop.png" },
                { "Btn_X", "Images/GameUI/Icon/Btn_X.png" },
                { "log_button", "Images/GameUI/log_button.aseprite" },
                { "max_healthbar_1", "Images/GameUI/max_healthbar 1.aseprite" },
                { "monster_turn", "Images/GameUI/monster_turn.aseprite" },
                { "return_button", "Images/GameUI/return_button.aseprite" },
                { "screen", "Images/GameUI/screen.aseprite" },
                { "Tap", "Images/GameUI/Tap.png" },
                { "Tap_temp", "Images/GameUI/Tap_temp.png" },
                { "Target", "Images/GameUI/Target.png" },
                { "TestPlayer", "Images/GameUI/TestPlayer.png" },
                { "Unit_0", "Images/UnitImage/Unit_0.png" },
            };

            public static string Get<T>() => Keys.TryGetValue(typeof(T).Name, out var key) ? key : null;
            public static string Get(string keyName) => Keys.TryGetValue(keyName, out var key) ? key : null;
        }

        public static class Sounds
        {
            private static readonly Dictionary<string, string> Keys = new Dictionary<string, string>()
            {
                { "AudioMixer", "Sounds/AudioMixer.mixer" },
                { "popSound", "Sounds/popSound.mp3" },
            };

            public static string Get<T>() => Keys.TryGetValue(typeof(T).Name, out var key) ? key : null;
            public static string Get(string keyName) => Keys.TryGetValue(keyName, out var key) ? key : null;
        }

    }
}
