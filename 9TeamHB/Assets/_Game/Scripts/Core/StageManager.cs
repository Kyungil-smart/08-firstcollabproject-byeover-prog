using System.Collections.Generic;
using UnityEngine;

namespace MyGame2.Stage
{
    // 스테이지 생명주기와 View 동기화를 관리
    // StageEvents를 소유하고, TurnSystem과 View 사이를 연결
    // GameManager와의 직접 참조를 제거하고 이벤트로 구현
 
    public sealed class StageManager : MonoBehaviour
    {
        [Header("씬 참조")]
        [Tooltip("엔티티 View를 생성할 부모 트랜스폼 (비워두면 자기 자신)")]
        [SerializeField] private Transform entityRoot;

        [Tooltip("엔티티 종류별 프리팹 매핑")]
        [SerializeField] private StagePrefabRegistry prefabRegistry;

        [Header("스테이지 데이터")]
        [Tooltip("스테이지 텍스트 파일 배열 (인덱스 = 스테이지 번호)")]
        [SerializeField] private TextAsset[] stageFiles;

        [Tooltip("게임 시작 시 로드할 스테이지 인덱스")]
        [SerializeField] private int startStageIndex;

        [Tooltip("그리드 셀 하나의 월드 크기")]
        [SerializeField] private float cellSize = 1f;

       
        // 이벤트 허브
        private readonly StageEvents _events = new StageEvents();
        
        private readonly MapLoader _mapLoader = new MapLoader();
        private readonly TagSystem _tagSystem = new TagSystem();
        private TurnSystem _turnSystem;
        
        // 런타임 상태
        private readonly Dictionary<int, GridEntityView> _views = new Dictionary<int, GridEntityView>(16);
        private int _currentStageIndex;
        
        // 이벤트 허브. GameManager, PlayerInputReader 등이 구독한다.
        public StageEvents Events { get { return _events; } }

        // 현재 스테이지 상태 (null이면 아직 로드 전).
        public StageState CurrentState { get; private set; }

        // 마지막 턴의 결과.
        public TurnOutcome LastOutcome { get; private set; }

        private void Awake()
        {
            if (entityRoot == null)
            {
                entityRoot = transform;
            }

            _turnSystem = TurnSystem.CreateDefault();
            LastOutcome = TurnOutcome.None();

            SubscribeToEvents();
        }

        private void Start()
        {
            LoadStage(startStageIndex);
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            _events.TurnExecuted += OnTurnExecuted;
            _events.ActivePlayerChanged += OnActivePlayerChanged;
        }

        private void UnsubscribeFromEvents()
        {
            _events.TurnExecuted -= OnTurnExecuted;
            _events.ActivePlayerChanged -= OnActivePlayerChanged;
        }

        // 턴 실행 완료 시 View 일괄 동기화.
        private void OnTurnExecuted(TurnOutcome outcome)
        {
            LastOutcome = outcome;
            SyncViews();
            SyncSelection();
        }

        // 활성 플레이어 전환 시 선택 마커 갱신.
        private void OnActivePlayerChanged(int newActivePlayerId)
        {
            SyncSelection();
        }
        
        // 밑에 Load 구조는 경민님이 마음대로 수정 하셔도 됩니다.
        // 지정한 인덱스의 스테이지를 로드한다.
        public void LoadStage(int stageIndex)
        {
            if (stageFiles == null || stageFiles.Length == 0)
            {
                Debug.LogError("[StageManager] stageFiles 배열이 비어있습니다.", this);
                return;
            }

            if (stageIndex < 0 || stageIndex >= stageFiles.Length)
            {
                Debug.LogError($"[StageManager] 스테이지 인덱스 {stageIndex} 범위 초과.", this);
                return;
            }

            TextAsset stageFile = stageFiles[stageIndex];
            if (stageFile == null)
            {
                Debug.LogError($"[StageManager] 인덱스 {stageIndex}의 TextAsset이 null.", this);
                return;
            }

            _currentStageIndex = stageIndex;
            ClearViews();

            MapDefinition definition = _mapLoader.Load(stageFile);
            CurrentState = StageState.FromMapDefinition(definition, _events);
            _tagSystem.Initialize(CurrentState);

            SpawnViews();
            SyncSelection();

            LastOutcome = TurnOutcome.None();
            _events.RaiseStageLoaded(stageIndex);
        }
        
        // 현재 스테이지를 다시 로드한다.
        
        public void RestartCurrentStage()
        {
            LoadStage(_currentStageIndex);
        }

        // 다음 스테이지를 로드한다.
        public bool LoadNextStage()
        {
            int next = _currentStageIndex + 1;
            if (next >= stageFiles.Length)
            {
                Debug.Log("[StageManager] 마지막 스테이지입니다.", this);
                return false;
            }

            LoadStage(next);
            return true;
        }

        // 활성 플레이어를 전환한다.
        public bool SwitchActivePlayer()
        {
            if (!CanAcceptPlayerInput())
            {
                return false;
            }

            return _tagSystem.Switch(CurrentState);
        }
        
        // 플레이어 턴을 실행한다.
        // 성공 시 View 동기화는 TurnExecuted 이벤트를 통해 자동 처리된다.
        
        public TurnOutcome TryExecuteTurn(Direction direction)
        {
            if (!CanAcceptPlayerInput())
            {
                LastOutcome = TurnOutcome.Ignored(
                    MoveResult.Blocked(
                        StageState.InvalidEntityId,
                        new GridPos(0, 0),
                        new GridPos(0, 0),
                        MoveBlockReason.DeadEntity));

                return LastOutcome;
            }

            // TurnSystem이 실행 → StageState가 이벤트 발행
            // OnTurnExecuted에서 SyncViews/SyncSelection 자동 호출
            return _turnSystem.TryExecutePlayerTurn(CurrentState, direction);
        }
        
        // 내부 유틸리티
        // 안전장치를 많이 넣어놔서 그렇지 크게 복잡한 구조는 아닙니다...?

        private bool CanAcceptPlayerInput()
        {
            return CurrentState != null &&
                   !CurrentState.IsGameOver &&
                   !CurrentState.IsStageClear;
        }

        private void SpawnViews()
        {
            if (prefabRegistry == null)
            {
                Debug.LogWarning("[StageManager] StagePrefabRegistry 미할당. View 없이 로직만 동작합니다.", this);
                return;
            }

            foreach (EntityState entity in CurrentState.Entities)
            {
                GridEntityView prefab = prefabRegistry.GetPrefab(entity);
                if (prefab == null)
                {
                    continue;
                }

                GridEntityView view = Instantiate(prefab, entityRoot);
                view.name = $"{entity.Kind}_{entity.Id}";
                view.Bind(entity, cellSize);
                _views[entity.Id] = view;
            }
        }

        private void SyncViews()
        {
            foreach (KeyValuePair<int, GridEntityView> pair in _views)
            {
                if (pair.Value == null)
                {
                    continue;
                }

                if (CurrentState.TryGetEntity(pair.Key, out EntityState entity))
                {
                    pair.Value.Sync(entity, cellSize);
                }
            }
        }

        private void SyncSelection()
        {
            foreach (KeyValuePair<int, GridEntityView> pair in _views)
            {
                if (pair.Value == null)
                {
                    continue;
                }

                bool isSelected = CurrentState != null &&
                                  pair.Key == CurrentState.ActivePlayerId &&
                                  CurrentState.TryGetEntity(pair.Key, out EntityState entity) &&
                                  entity.IsPlayer &&
                                  entity.IsAlive;

                pair.Value.SyncSelection(isSelected);
            }
        }

        private void ClearViews()
        {
            foreach (KeyValuePair<int, GridEntityView> pair in _views)
            {
                if (pair.Value != null)
                {
                    Destroy(pair.Value.gameObject);
                }
            }

            _views.Clear();
        }
    }
}
