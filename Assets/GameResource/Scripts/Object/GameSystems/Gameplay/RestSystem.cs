using Backend.Object.Management;
using R3;
using UnityEngine;

namespace Backend.Object.GameSystems.Gameplay
{
    /// <summary>
    /// 맵 Rest(야영) 노드의 선택지 적용과 세션 생명주기를 관리합니다.
    /// </summary>
    public static class RestSystem
    {
        private static readonly Subject<Unit> _onRestStarted = new();
        private static readonly Subject<Unit> _onRestEnded = new();

        private static bool _isActive;
        private static MapNode _sourceNode;

        public static Observable<Unit> OnRestStarted => _onRestStarted;
        public static Observable<Unit> OnRestEnded => _onRestEnded;
        public static bool IsActive => _isActive;
        public static MapNode SourceNode => _sourceNode;

        public static void Initialize()
        {
            ResetState();
        }

        public static void Dispose()
        {
            ResetState();
        }

        /// <summary>
        /// Rest 노드 진입 시 세션을 시작합니다.
        /// </summary>
        public static bool BeginRest(MapNode node)
        {
            if (node.NodeType != MapNodeType.Rest)
            {
                Debug.LogWarning($"[RestSystem] BeginRest called with non-rest node: {node.NodeType}");
                return false;
            }

            _isActive = true;
            _sourceNode = node;
            GameManager.SetPhase(GamePhase.RestNode);
            _onRestStarted.OnNext(Unit.Default);
            return true;
        }

        /// <summary>
        /// 야영 휴식으로 최대 체력의 일정 비율만큼 회복합니다.
        /// </summary>
        public static void ChooseHeal()
        {
            if (!_isActive)
                return;

            var option = FindOption(RestOptionType.Rest);
            var ratio = option != null ? Mathf.Clamp01(option.value) : 0.5f;
            var amount = Mathf.Max(1, Mathf.RoundToInt(PlayerStateSystem.MaxHp.CurrentValue * ratio));
            PlayerStateSystem.Heal(amount);
            FinishAndReturnToMap();
        }

        /// <summary>
        /// 아무 행동 없이 야영지를 떠납니다.
        /// </summary>
        public static void ChoosePass()
        {
            if (!_isActive)
                return;

            FinishAndReturnToMap();
        }

        private static RestOptionData FindOption(RestOptionType type)
        {
            var options = TableManager.GetRestOptions();
            for (var i = 0; i < options.Count; i++)
            {
                var option = options[i];
                if (option != null && option.effect_type == type)
                    return option;
            }

            return null;
        }

        private static void FinishAndReturnToMap()
        {
            _isActive = false;
            _onRestEnded.OnNext(Unit.Default);
            MapSystem.CompleteCurrentNode();
            ResetState();
        }

        private static void ResetState()
        {
            _isActive = false;
            _sourceNode = default;
        }
    }
}
