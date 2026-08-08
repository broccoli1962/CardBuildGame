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
                { "AudioSource", "InGame/AudioSource.prefab" },
                { "BattleVfxController", "InGame/BattleVfxController.prefab" },
                { "CardController", "InGame/CardController.prefab" },
                { "MonsterController", "InGame/MonsterController.prefab" },
            };

            public static string Get<T>() => Keys.TryGetValue(typeof(T).Name, out var key) ? key : null;
            public static string Get(string keyName) => Keys.TryGetValue(keyName, out var key) ? key : null;
        }

        public static class Icons
        {
            private static readonly Dictionary<string, string> Keys = new Dictionary<string, string>()
            {
                { "Badge_Deck", "Icons/Badge_Deck.png" },
                { "Badge_ManaCost", "Icons/Badge_ManaCost.png" },
                { "Badge_Stage", "Icons/Badge_Stage.png" },
                { "Bar_EnemyHP", "Icons/Bar_EnemyHP.png" },
                { "Bg_Death_FallenAdventurer", "Icons/Bg_Death_FallenAdventurer.png" },
                { "Bg_Event_Camping", "Icons/Bg_Event_Camping.png" },
                { "Bg_Event_CaveLake", "Icons/Bg_Event_CaveLake.png" },
                { "Bg_Event_DiceGambling", "Icons/Bg_Event_DiceGambling.png" },
                { "Bg_Event_FortuneTeller", "Icons/Bg_Event_FortuneTeller.png" },
                { "Bg_Event_Treasure", "Icons/Bg_Event_Treasure.png" },
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
                { "Icon_Deck", "Icons/Icon_Deck.png" },
                { "Icon_EnemySkull", "Icons/Icon_EnemySkull.png" },
                { "Icon_Graveyard", "Icons/Icon_Graveyard.png" },
                { "Icon_Heal", "Icons/Icon_Heal.png" },
                { "Icon_Heart", "Icons/Icon_Heart.png" },
                { "Icon_Mana", "Icons/Icon_Mana.png" },
                { "Icon_Map", "Icons/Icon_Map.png" },
                { "Icon_Settings", "Icons/Icon_Settings.png" },
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
                { "PromptCraft_Logo", "Icons/PromptCraft_Logo.png" },
            };

            public static string Get<T>() => Keys.TryGetValue(typeof(T).Name, out var key) ? key : null;
            public static string Get(string keyName) => Keys.TryGetValue(keyName, out var key) ? key : null;
        }

        public static class UI
        {
            private static readonly Dictionary<string, string> Keys = new Dictionary<string, string>()
            {
                { "Vfx_Shield", "Assets/GameResource/Images/VFX/Vfx_Shield.png" },
                { "Vfx_SlashArc", "Assets/GameResource/Images/VFX/Vfx_SlashArc.png" },
                { "Vfx_Sword", "Assets/GameResource/Images/VFX/Vfx_Sword.png" },
                { "AttackSlashVfx", "UI/AttackSlashVfx.prefab" },
                { "Card", "UI/Card.prefab" },
                { "CardCreationPanel", "UI/CardCreationPanel.prefab" },
                { "DeathPanel", "UI/DeathPanel.prefab" },
                { "DeckInspectPopup", "UI/DeckInspectPopup.prefab" },
                { "DefendShieldVfx", "UI/DefendShieldVfx.prefab" },
                { "EventPanel", "UI/EventPanel.prefab" },
                { "GamePanel", "UI/GamePanel.prefab" },
                { "LoadingPanel", "UI/LoadingPanel.prefab" },
                { "LobbyScreenPanel", "UI/LobbyScreenPanel.prefab" },
                { "MapGraphView", "UI/MapGraphView.prefab" },
                { "MapNodeView", "UI/MapNodeView.prefab" },
                { "MapPanel", "UI/MapPanel.prefab" },
                { "MapPreviewPopup", "UI/MapPreviewPopup.prefab" },
                { "Monster", "UI/Monster.prefab" },
                { "RestPanel", "UI/RestPanel.prefab" },
                { "SoundSettingsPopup", "UI/SoundSettingsPopup.prefab" },
                { "TreasurePanel", "UI/TreasurePanel.prefab" },
                { "UIRoot", "UI/UIRoot.prefab" },
            };

            public static string Get<T>() => Keys.TryGetValue(typeof(T).Name, out var key) ? key : null;
            public static string Get(string keyName) => Keys.TryGetValue(keyName, out var key) ? key : null;
        }

        public static class Sounds
        {
            private static readonly Dictionary<string, string> Keys = new Dictionary<string, string>()
            {
                { "AudioMixer", "Assets/GameResource/Sounds/AudioMixer.mixer" },
                { "Battle_BattleThemeA", "Assets/GameResource/Sounds/Bgm/Battle_BattleThemeA.mp3" },
                { "Battle_DeterminedPursuit_Loop", "Assets/GameResource/Sounds/Bgm/Battle_DeterminedPursuit_Loop.wav" },
                { "Event_AncientPowerOfSerpents", "Assets/GameResource/Sounds/Bgm/Event_AncientPowerOfSerpents.ogg" },
                { "Event_Cave1_Holizna", "Assets/GameResource/Sounds/Bgm/Event_Cave1_Holizna.mp3" },
                { "Event_Cave2_Holizna", "Assets/GameResource/Sounds/Bgm/Event_Cave2_Holizna.mp3" },
                { "Event_CaveTheme", "Assets/GameResource/Sounds/Bgm/Event_CaveTheme.ogg" },
                { "Event_Enchantress_Holizna", "Assets/GameResource/Sounds/Bgm/Event_Enchantress_Holizna.mp3" },
                { "Event_FantasyChoir1", "Assets/GameResource/Sounds/Bgm/Event_FantasyChoir1.mp3" },
                { "Lobby_TownTheme", "Assets/GameResource/Sounds/Bgm/Lobby_TownTheme.mp3" },
                { "Map_FieldOfDreams", "Assets/GameResource/Sounds/Bgm/Map_FieldOfDreams.mp3" },
                { "Rest_OldTowerInn_Loop", "Assets/GameResource/Sounds/Bgm/Rest_OldTowerInn_Loop.wav" },
                { "Title_ALegendWillRise", "Assets/GameResource/Sounds/Bgm/Title_ALegendWillRise.mp3" },
                { "Attack_Swing", "Assets/GameResource/Sounds/Sfx/Attack_Swing.wav" },
                { "Card_Flip", "Assets/GameResource/Sounds/Sfx/Card_Flip.wav" },
                { "sfx_button", "Assets/GameResource/Sounds/Sfx/sfx_button.wav" },
                { "Shield_Sound", "Assets/GameResource/Sounds/Sfx/Shield_Sound.wav" },
                { "UIClick_UI", "Assets/GameResource/Sounds/Sfx/UIClick_UI.wav" },
            };

            public static string Get<T>() => Keys.TryGetValue(typeof(T).Name, out var key) ? key : null;
            public static string Get(string keyName) => Keys.TryGetValue(keyName, out var key) ? key : null;
        }

    }
}
