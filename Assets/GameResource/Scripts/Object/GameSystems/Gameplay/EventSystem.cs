using Backend.Object.Management;
using R3;
using UnityEngine;

namespace Backend.Object.GameSystems.Gameplay
{
    /// <summary>
    /// 맵 Event 노드의 선택지 적용과 세션 생명주기를 관리합니다.
    /// </summary>
    public static class EventSystem
    {
        public const string EventIdCaveLake = "cave_lake";
        public const string EventIdDiceGambling = "dice_gambling";
        public const string EventIdFortuneTeller = "fortune_teller";

        private const float DiceSuccessChance = 0.2f;
        private const int CaveLakeMaxHpBonus = 2;
        private const int DiceMaxManaBonus = 2;

        private static readonly Subject<Unit> _onEventStarted = new();
        private static readonly Subject<Unit> _onEventEnded = new();
        private static readonly Subject<Unit> _onStateChanged = new();

        private static bool _isActive;
        private static bool _awaitingResultContinue;
        private static bool _diceSucceeded;
        private static bool _startedCardCreation;
        private static MapNode _sourceNode;
        private static MapEventData _eventData;

        public static Observable<Unit> OnEventStarted => _onEventStarted;
        public static Observable<Unit> OnEventEnded => _onEventEnded;
        public static Observable<Unit> OnStateChanged => _onStateChanged;

        public static bool IsActive => _isActive;
        public static bool AwaitingResultContinue => _awaitingResultContinue;
        public static bool DiceSucceeded => _diceSucceeded;
        public static bool StartedCardCreation => _startedCardCreation;
        public static MapNode SourceNode => _sourceNode;
        public static MapEventData EventData => _eventData;
        public static string EventId => _eventData?.event_id ?? string.Empty;

        public static void Initialize()
        {
            ResetState();
        }

        public static void Dispose()
        {
            ResetState();
        }

        /// <summary>
        /// Event 노드 진입 시 세션을 시작합니다.
        /// </summary>
        public static bool BeginEvent(MapNode node)
        {
            if (node.NodeType != MapNodeType.Event)
            {
                Debug.LogWarning($"[EventSystem] BeginEvent called with non-event node: {node.NodeType}");
                return false;
            }

            var data = TableManager.GetMapEvent(node.ContentId);
            if (data == null)
            {
                Debug.LogError($"[EventSystem] Unknown event_id: {node.ContentId}");
                return false;
            }

            _isActive = true;
            _awaitingResultContinue = false;
            _diceSucceeded = false;
            _startedCardCreation = false;
            _sourceNode = node;
            _eventData = data;

            GameManager.SetPhase(GamePhase.EventNode);
            _onEventStarted.OnNext(Unit.Default);
            _onStateChanged.OnNext(Unit.Default);
            return true;
        }

        /// <summary>
        /// 선택지 A를 실행합니다. 주사위 이벤트는 결과 대기 상태로 전환될 수 있습니다.
        /// </summary>
        public static void ChooseOptionA()
        {
            if (!_isActive || _awaitingResultContinue || _eventData == null)
                return;

            switch (_eventData.event_id)
            {
                case EventIdCaveLake:
                    PlayerStateSystem.ChangeMaxHp(CaveLakeMaxHpBonus);
                    PlayerStateSystem.Heal(99);
                    FinishAndReturnToMap();
                    break;

                case EventIdDiceGambling:
                    _diceSucceeded = UnityEngine.Random.value < DiceSuccessChance;
                    if (_diceSucceeded)
                    {
                        PlayerStateSystem.ChangeMaxMana(DiceMaxManaBonus);
                        PlayerStateSystem.RecoverMana(DiceMaxManaBonus);
                    }

                    _awaitingResultContinue = true;
                    _onStateChanged.OnNext(Unit.Default);
                    break;

                case EventIdFortuneTeller:
                    StartCardCreationAndEndEventUi();
                    break;

                default:
                    Debug.LogWarning($"[EventSystem] Unhandled event option A: {_eventData.event_id}");
                    FinishAndReturnToMap();
                    break;
            }
        }

        /// <summary>
        /// 선택지 B(지나치기)를 실행합니다.
        /// </summary>
        public static void ChooseOptionB()
        {
            if (!_isActive || _awaitingResultContinue)
                return;

            FinishAndReturnToMap();
        }

        /// <summary>
        /// 주사위 결과 확인 후 맵으로 복귀합니다.
        /// </summary>
        public static void ConfirmResultAndLeave()
        {
            if (!_isActive || !_awaitingResultContinue)
                return;

            FinishAndReturnToMap();
        }

        private static void StartCardCreationAndEndEventUi()
        {
            var node = _sourceNode;
            _startedCardCreation = true;
            _isActive = false;
            _awaitingResultContinue = false;
            _onEventEnded.OnNext(Unit.Default);
            _onStateChanged.OnNext(Unit.Default);
            ResetFields();

            // CardCreationSystem.EndSession이 CompleteCurrentNode를 호출한다.
            CardCreationSystem.BeginSession(node);
        }

        private static void FinishAndReturnToMap()
        {
            _isActive = false;
            _awaitingResultContinue = false;
            _onEventEnded.OnNext(Unit.Default);
            _onStateChanged.OnNext(Unit.Default);
            MapSystem.CompleteCurrentNode();
            ResetFields();
        }

        private static void ResetState()
        {
            _isActive = false;
            ResetFields();
        }

        private static void ResetFields()
        {
            _awaitingResultContinue = false;
            _diceSucceeded = false;
            _startedCardCreation = false;
            _sourceNode = default;
            _eventData = null;
        }
    }
}
