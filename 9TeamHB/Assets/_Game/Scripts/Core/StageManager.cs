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
        [SerializeField] private Transform entityRoot;
        [SerializeField] private StagePrefabRegistry prefabRegistry;

        [Header("스테이지 데이터")]
        [SerializeField] private TextAsset[] stageFiles;
        [SerializeField] private int startStageIndex;
        [SerializeField] private float cellSize = 1f;

        [Header("기호 레지스트리")]
        [Tooltip("맵 텍스트의 문자 매핑 SO")]
        [SerializeField] private MapSymbolRegistrySO symbolRegistry;

        private MapLoader _mapLoader;
        
        // 이벤트 허브
        private readonly StageEvents _events = new StageEvents();
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
            _mapLoader = new MapLoader(symbolRegistry);
            if (entityRoot == null) entityRoot = transform;
            _turnSystem = TurnSystemBuilder.Default().Build();
            LastOutcome = TurnOutcome.None();
            _events.TurnExecuted += OnTurnExecuted;
            _events.ActivePlayerChanged += OnActivePlayerChanged;
        }

        private void Start()
        {
            LoadStage(startStageIndex);
        }

        private void OnDestroy()
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
        private void OnActivePlayerChanged(int id) { SyncSelection(); }

        // 밑에 Load 구조는 경민님이 마음대로 수정 하셔도 됩니다.
        // 지정한 인덱스의 스테이지를 로드한다.

        public void LoadStage(int stageIndex)
        {
            if (stageFiles == null || stageFiles.Length == 0)
            { Debug.LogError("[StageManager] stageFiles 비어있음.", this); return; }

            if (stageIndex < 0 || stageIndex >= stageFiles.Length)
            { Debug.LogError($"[StageManager] 인덱스 {stageIndex} 범위 초과.", this); return; }

            TextAsset file = stageFiles[stageIndex];
            if (file == null)
            { Debug.LogError($"[StageManager] 인덱스 {stageIndex} TextAsset null.", this); return; }

            _currentStageIndex = stageIndex;
            ClearViews();

            MapDefinition def = _mapLoader.Load(file);
            CurrentState = StageState.FromMapDefinition(def, _events);
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
            if (next >= stageFiles.Length) { Debug.Log("[StageManager] 마지막 스테이지.", this); return false; }
            LoadStage(next);
            return true;
        }

        // 활성 플레이어를 전환한다.
        public bool SwitchActivePlayer()
        {
            if (!CanAcceptInput()) return false;
            return _tagSystem.Switch(CurrentState);
        }

        // 플레이어 턴을 실행한다.
        // 성공 시 View 동기화는 TurnExecuted 이벤트를 통해 자동 처리된다.

        public TurnOutcome TryExecuteTurn(Direction direction)
        {
            if (!CanAcceptInput())
            {
                LastOutcome = TurnOutcome.Ignored(MoveResult.Blocked(
                    StageState.InvalidEntityId, new GridPos(0, 0), new GridPos(0, 0),
                    MoveBlockReason.DeadEntity));
                return LastOutcome;
            }

            // TurnSystem이 실행 → StageState가 이벤트 발행
            // OnTurnExecuted에서 SyncViews/SyncSelection 자동 호출
            return _turnSystem.TryExecutePlayerTurn(CurrentState, direction);
        }

        // 내부 유틸리티
        // 안전장치를 많이 넣어놔서 그렇지 크게 복잡한 구조는 아닙니다...?

        private bool CanAcceptInput()
        {
            return CurrentState != null && !CurrentState.IsGameOver && !CurrentState.IsStageClear;
        }

        private void SpawnViews()
        {
            if (prefabRegistry == null) return;
            foreach (EntityState e in CurrentState.Entities)
            {
                GridEntityView prefab = e.Prefab;
                if (prefab == null) continue;
                GridEntityView view = Instantiate(prefab, entityRoot);
                view.name = $"{e.Kind}_{e.Id}";
                view.Bind(e, cellSize);
                _views[e.Id] = view;
            }
        }

        private void SyncViews()
        {
            foreach (var pair in _views)
            {
                if (pair.Value == null) continue;
                if (CurrentState.TryGetEntity(pair.Key, out EntityState e))
                    pair.Value.Sync(e, cellSize);
            }
        }

        private void SyncSelection()
        {
            foreach (var pair in _views)
            {
                if (pair.Value == null) continue;
                bool sel = CurrentState != null &&
                           pair.Key == CurrentState.ActivePlayerId &&
                           CurrentState.TryGetEntity(pair.Key, out EntityState e) &&
                           e.IsPlayer && e.IsAlive;
                pair.Value.SyncSelection(sel);
            }
        }

        private void ClearViews()
        {
            foreach (var pair in _views)
                if (pair.Value != null) Destroy(pair.Value.gameObject);
            _views.Clear();
        }
    }
}
