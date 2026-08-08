using Backend.Object.GameSystems.Gameplay;

namespace Backend.Object.Management
{
    /// <summary>
    /// 상황별 BGM 키 매핑과 재생 진입점.
    /// </summary>
    public static class GameBgm
    {
        public static class Keys
        {
            public const string Lobby = "Title_ALegendWillRise";
            public const string Map = "Map_FieldOfDreams";
            public const string Battle = "Battle_BattleThemeA";
            public const string BossBattle = "Battle_DeterminedPursuit_Loop";
            public const string Rest = "Rest_OldTowerInn_Loop";
            public const string Treasure = "Event_FantasyChoir1";
            public const string CardCreation = "Event_Enchantress_Holizna";
            public const string Death = "Event_Cave2_Holizna";
            public const string EventDefault = "Event_AncientPowerOfSerpents";
            public const string EventCaveLake = "Event_CaveTheme";
            public const string EventDiceGambling = "Event_AncientPowerOfSerpents";
            public const string EventFortuneTeller = "Event_Enchantress_Holizna";
        }

        public static void PlayLobby() => AudioManager.PlayBgm(Keys.Lobby);

        public static void PlayMap() => AudioManager.PlayBgm(Keys.Map);

        public static void PlayRest() => AudioManager.PlayBgm(Keys.Rest);

        public static void PlayTreasure() => AudioManager.PlayBgm(Keys.Treasure);

        public static void PlayCardCreation() => AudioManager.PlayBgm(Keys.CardCreation);

        public static void PlayDeath() => AudioManager.PlayBgm(Keys.Death);

        public static void PlayForNode(MapNode node)
        {
            switch (node.NodeType)
            {
                case MapNodeType.Battle:
                case MapNodeType.Elite:
                    AudioManager.PlayBgm(Keys.Battle);
                    break;
                case MapNodeType.Boss:
                    AudioManager.PlayBgm(Keys.BossBattle);
                    break;
                case MapNodeType.Rest:
                    PlayRest();
                    break;
                case MapNodeType.Treasure:
                    PlayTreasure();
                    break;
                case MapNodeType.Event:
                    PlayForEvent(EventSystem.EventId);
                    break;
            }
        }

        public static void PlayForEvent(string eventId)
        {
            var key = eventId switch
            {
                EventSystem.EventIdCaveLake => Keys.EventCaveLake,
                EventSystem.EventIdDiceGambling => Keys.EventDiceGambling,
                EventSystem.EventIdFortuneTeller => Keys.EventFortuneTeller,
                _ => Keys.EventDefault,
            };
            AudioManager.PlayBgm(key);
        }
    }
}
