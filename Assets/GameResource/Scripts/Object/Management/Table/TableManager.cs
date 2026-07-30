using Backend.Util.Management;
using TableData;
using UnityEngine;

namespace Backend.Object.Management
{
    public partial class TableManager : SingletonGameObject<TableManager>
    {
        #region Constants

        private const string TableLinkerResourcePath = "TableLinker";

        #endregion

        #region Fields

        private TableLinker _tableLinker;

        [System.NonSerialized]
        private bool _isInitialized;

        private static readonly string[] EmptyStrings = System.Array.Empty<string>();
        private static readonly StageData[] EmptyStages = System.Array.Empty<StageData>();
        private static readonly MonsterActionData[] EmptyMonsterActions = System.Array.Empty<MonsterActionData>();
        private static readonly BaseCardData[] EmptyBaseCards = System.Array.Empty<BaseCardData>();
        private static readonly MapNodeType[] EmptyMapNodeTypes = System.Array.Empty<MapNodeType>();
        private static readonly int[] EmptyInts = System.Array.Empty<int>();
        private static readonly MapEventData[] EmptyMapEvents = System.Array.Empty<MapEventData>();
        private static readonly RestOptionData[] EmptyRestOptions = System.Array.Empty<RestOptionData>();
        private static readonly TreasureOptionData[] EmptyTreasureOptions = System.Array.Empty<TreasureOptionData>();

        #endregion

        #region Properties

        /// <summary>
        /// 테이블 매니저 초기화 완료 여부를 반환합니다.
        /// </summary>
        public static bool IsInitialized
        {
            get
            {
                if (GameStateUtil.IsQuitting) return false;

                var instance = Instance;
                return instance != null && instance._isInitialized;
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// 테이블 데이터를 로드하고 인덱스를 구축합니다.
        /// </summary>
        public static void Init() => Instance?.Init_Internal();

        private void Init_Internal()
        {
            if (_isInitialized) return;

            _tableLinker = Resources.Load<TableLinker>(TableLinkerResourcePath);
            if (_tableLinker == null)
            {
                Debug.LogError("[TableManager] TableLinker를 Resources에서 찾을 수 없습니다.");
                return;
            }

            BuildStageIndex();
            BuildMonsterIndex();
            BuildCardIndex();
            BuildThreatIndex();
            BuildMapIndex();
            BuildBalanceIndex();

            try
            {
                LocalizeTable.Initialize(global::Util.LanguageUtil.GetLanguageCode());
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[TableManager] 로컬라이즈 초기화 실패: {ex.Message}");
            }

            _isInitialized = true;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            ValidateTables();
#endif
        }

        #endregion

        #region Access Guard

        private static bool TryGetTable(out TableManager manager)
        {
            manager = null;

            if (GameStateUtil.IsQuitting) return false;

            var instance = Instance;
            if (instance == null) return false;

            if (!instance._isInitialized)
            {
                Debug.LogError("[TableManager] 초기화 전에 테이블에 접근했습니다. GameManager 초기화 순서를 확인하세요.");
                return false;
            }

            manager = instance;
            return true;
        }

        #endregion
    }
}
