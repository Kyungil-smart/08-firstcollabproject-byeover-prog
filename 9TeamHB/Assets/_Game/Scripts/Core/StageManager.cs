using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyGame2.Stage
{
    // 스테이지 생명주기와 View 동기화를 관리
    // StageEvents를 소유하고, TurnSystem과 View 사이를 연결

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

        [Header("상태 참조")]
        [Tooltip("EntityFunctionSO 등이 StageState에 접근하기 위한 SO")]
        [SerializeField] private StageStateReferenceSO stageStateReference;

        [Header("되돌리기 설정")]
        [Tooltip("되감기 시 한 턴당 소요 시간 (초)")]
        [SerializeField] private float undoStepInterval = 0.15f;

        private MapLoader _mapLoader;
        
        // 이벤트 허브
        private readonly StageEvents _events = new StageEvents();
        private readonly TagSystem _tagSystem = new TagSystem();
        private TurnSystem _turnSystem;

        // 런타임 상태
        private readonly Dictionary<int, GridEntityView> _views = new Dictionary<int, GridEntityView>(16);
        private int _currentStageIndex;

        // Undo 스냅샷 스택
        private readonly Stack<StageSnapshot> _undoStack = new Stack<StageSnapshot>(64);
        private Coroutine _undoCoroutine;

        // 외부에서 현재 스테이지 인덱스 참조용
        public int CurrentStageIndex => _currentStageIndex;

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

        // PlayerPrefs에서 선택된 스테이지 읽기
        private void Start()
        {
            int selected = PlayerPrefs.GetInt("SelectedStage", startStageIndex);
            LoadStage(selected);
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

            // 이전 상태의 IUpdate/IDisposable 컴포넌트 이벤트 구독 해제
            // (투사체 발사기 등이 다음 스테이지에서도 계속 발사하는 버그 방지)
            if (CurrentState != null)
            {
                foreach (EntityState e in CurrentState.Entities)
                {
                    foreach (IComponentData comp in e.Components)
                    {
                        if (comp is System.IDisposable disposable)
                            disposable.Dispose();
                    }
                }
            }

            // 투사체 속도 레지스트리 초기화
            ProjectileSpeedRegistry.Clear();

            _undoStack.Clear(); // Undo 스택 초기화

            MapDefinition def = _mapLoader.Load(file);
            CurrentState = StageState.FromMapDefinition(def, _events);

            // StageStateReferenceSO에 현재 상태 등록
            if (stageStateReference != null)
                stageStateReference.Register(CurrentState);

            _tagSystem.Initialize(CurrentState);

            // PairGroup 기반 엔티티 페어 연결 (버튼↔문 등)
            ResolvePairGroups();

            // 텔레포트 셀 페어 연결
            ResolveTeleportPairs();

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
        public TurnOutcome TryExecuteTurn(Direction direction)
        {
            if (!CanAcceptInput())
            {
                LastOutcome = TurnOutcome.Ignored(MoveResult.Blocked(
                    StageState.InvalidEntityId, new GridPos(0, 0), new GridPos(0, 0),
                    MoveBlockReason.DeadEntity));
                return LastOutcome;
            }

            // 턴 실행 전 스냅샷 저장 (Undo용)
            CaptureSnapshot();

            return _turnSystem.TryExecutePlayerTurn(CurrentState, direction);
        }

        // 되돌리기 (Undo)

        // 턴 실행 직전에 호출하여 현재 상태를 스냅샷으로 저장.
        private void CaptureSnapshot()
        {
            if (CurrentState == null) return;
            StageSnapshot snapshot = StageSnapshot.Capture(CurrentState);
            if (snapshot != null)
            {
                _undoStack.Push(snapshot);
                Debug.Log($"[Undo] 스냅샷 저장 — 턴 {CurrentState.TurnIndex}, 스택 {_undoStack.Count}개");
            }
        }

        // 되돌리기 모드 진입 (Space 누름). 연속 되감기 코루틴 시작.
        public bool TryEnterUndo()
        {
            if (CurrentState == null || CurrentState.IsGameOver || CurrentState.IsStageClear)
            {
                Debug.Log($"[Undo] 진입 실패: state={CurrentState != null}, gameOver={CurrentState?.IsGameOver}, clear={CurrentState?.IsStageClear}");
                return false;
            }
            if (CurrentState.IsUndoProcessing)
            {
                Debug.Log("[Undo] 이미 되돌리기 중");
                return false;
            }
            if (_undoStack.Count == 0)
            {
                Debug.Log("[Undo] 스냅샷 없음 (첫 턴 전)");
                return false;
            }

            Debug.Log($"[Undo] 되돌리기 시작 — 스냅샷 {_undoStack.Count}개");
            CurrentState.IsUndoProcessing = true;

            // 즉시 1턴 되돌리기 + 이후 홀딩 중 연속 되감기
            _undoCoroutine = StartCoroutine(UndoRewindLoop());
            return true;
        }

        // 되돌리기 모드 해제 (Space 뗌).
        public void LeaveUndo()
        {
            if (_undoCoroutine != null)
            {
                StopCoroutine(_undoCoroutine);
                _undoCoroutine = null;
            }

            if (CurrentState == null) return;
            CurrentState.IsUndoProcessing = false;

            // 되돌려진 상태를 View에 반영
            SyncViews();
            SyncSelection();

            // Undo 완료 이벤트 발행 (InteractableTileVisual 등이 구독)
            _events.RaiseUndoExecuted();
        }

        // Space 홀딩 중 연속으로 턴을 되감는 코루틴.
        private IEnumerator UndoRewindLoop()
        {
            while (_undoStack.Count > 0 && CurrentState.IsUndoProcessing)
            {
                StageSnapshot snapshot = _undoStack.Pop();
                CurrentState.Restore(snapshot);

                // 동적 스폰된 엔티티의 View 정리
                CleanupOrphanViews();

                // 부드러운 슬라이딩으로 되감기 (Lerp 기반)
                SyncViews();
                SyncSelection();
                _events.RaiseTurnExecuted(TurnOutcome.None());

                yield return new WaitForSecondsRealtime(undoStepInterval);
            }

            // 스냅샷 소진 시 자동 해제
            if (CurrentState.IsUndoProcessing)
            {
                CurrentState.IsUndoProcessing = false;
                _events.RaiseUndoExecuted();
            }
            _undoCoroutine = null;
        }

        // Undo 후 상태에 없는 View를 제거.
        private void CleanupOrphanViews()
        {
            List<int> orphans = null;
            foreach (var pair in _views)
            {
                if (!CurrentState.TryGetEntity(pair.Key, out _))
                {
                    if (orphans == null) orphans = new List<int>(4);
                    orphans.Add(pair.Key);
                }
            }
            if (orphans != null)
            {
                for (int i = 0; i < orphans.Count; i++)
                {
                    if (_views.TryGetValue(orphans[i], out GridEntityView view) && view != null)
                        Destroy(view.gameObject);
                    _views.Remove(orphans[i]);
                }
            }
        }

        // 페어 그룹 자동 연결

        // PairGroupData를 가진 엔티티들을 그룹별로 묶어서
        // 버튼/레버 위치 <-> 문 위치를 SetCellPair로 연결한다.
        private void ResolvePairGroups()
        {
            if (CurrentState == null) return;

            // 그룹별 엔티티 수집
            var groups = new Dictionary<int, List<EntityState>>();

            foreach (EntityState e in CurrentState.Entities)
            {
                if (!e.Has<PairGroupData>()) continue;
                int group = e.Get<PairGroupData>().PairGroup;
                if (group <= 0) continue;

                if (!groups.ContainsKey(group))
                    groups[group] = new List<EntityState>(4);
                groups[group].Add(e);
            }

            foreach (var kvp in groups)
            {
                List<EntityState> members = kvp.Value;

                // 트리거(버튼/레버)와 대상(문) 분리
                List<EntityState> triggers = new List<EntityState>();
                List<EntityState> doors = new List<EntityState>();
                List<EntityState> others = new List<EntityState>();

                for (int i = 0; i < members.Count; i++)
                {
                    EntityState e = members[i];
                    if (e.Kind == EntityKind.ButtonEntity || e.Kind == EntityKind.LeverEntity)
                        triggers.Add(e);
                    else if (e.Kind == EntityKind.DoorEntity || e.Has<HiddenTrapData>())
                        doors.Add(e);
                    else
                        others.Add(e);
                }

                // 트리거 <-> 문 연결
                if (triggers.Count > 0 && doors.Count > 0)
                {
                    // 트리거 1개 : 문 N개
                    if (triggers.Count == 1)
                    {
                        for (int i = 0; i < doors.Count; i++)
                            CurrentState.SetCellPair(triggers[0].Position, doors[i].Position);
                    }
                    else
                    {
                        // N:N -> 순서대로 1:1
                        int pairCount = Mathf.Min(triggers.Count, doors.Count);
                        for (int i = 0; i < pairCount; i++)
                            CurrentState.SetCellPair(triggers[i].Position, doors[i].Position);
                    }
                }
                // 트리거/문이 아닌 범용 페어 (텔레포트 등)
                else if (triggers.Count == 0 && doors.Count == 0 && others.Count >= 2)
                {
                    // 2개씩 순서대로 연결
                    for (int i = 0; i + 1 < others.Count; i += 2)
                    {
                        CurrentState.SetCellPair(others[i].Position, others[i + 1].Position);
                    }
                }
                // 혼합 (기타 + 문 등) -> 전체를 순서대로 페어
                else if (members.Count >= 2)
                {
                    for (int i = 0; i + 1 < members.Count; i += 2)
                    {
                        CurrentState.SetCellPair(members[i].Position, members[i + 1].Position);
                    }
                }

                if (members.Count < 2)
                {
                    Debug.LogWarning($"[StageManager] PairGroup {kvp.Key}: " +
                                     $"멤버 {members.Count}개 — 페어 연결 불가.");
                }
            }
        }

        // 텔레포트 셀 페어: HasTeleport 셀을 읽기 순서(좌->우, 위->아래)로 2개씩 묶어 SetCellPair
        private void ResolveTeleportPairs()
        {
            if (CurrentState == null) return;

            List<GridPos> teleportCells = new List<GridPos>();

            for (int y = 0; y < CurrentState.Height; y++)
            {
                for (int x = 0; x < CurrentState.Width; x++)
                {
                    GridPos pos = new GridPos(x, y);
                    CellData cell = CurrentState.GetCell(pos);
                    if (cell.HasTeleport)
                        teleportCells.Add(pos);
                }
            }

            // 2개씩 순서대로 페어
            for (int i = 0; i + 1 < teleportCells.Count; i += 2)
            {
                CurrentState.SetCellPair(teleportCells[i], teleportCells[i + 1]);
            }

            if (teleportCells.Count % 2 != 0)
            {
                Debug.LogWarning($"[StageManager] 텔레포트 셀이 홀수({teleportCells.Count}개) — 마지막 셀 미페어.");
            }
        }

        // 내부 유틸리티

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

                // ViewRequest 등록: CameraManager 등이 ID로 View를 찾을 수 있도록
                RegisterViewRequest(e.Id, view);

                // InteractableTileVisual 초기화 (문/레버/버튼 애니메이션)
                InitTileVisual(view, e.Id);
            }
        }

        // ViewRequest 콜백 등록 헬퍼
        private void RegisterViewRequest(int entityId, GridEntityView view)
        {
            _events.ViewRequestSubscribe(entityId, request =>
            {
                request.Callback?.Invoke(view);
            });
        }

        // InteractableTileVisual 초기화 헬퍼 (문/레버/버튼 프리팹용)
        private void InitTileVisual(GridEntityView view, int entityId)
        {
            var tileVisual = view.GetComponent<InteractableTileVisual>();
            if (tileVisual != null)
            {
                tileVisual.Initialize(entityId);
            }
        }

        private void SyncViews()
        {
            // 제거된 엔티티의 View 파괴 (투사체 소멸 등)
            CleanupOrphanViews();

            // 동적 생성된 엔티티의 View가 없으면 자동 생성
            foreach (EntityState e in CurrentState.Entities)
            {
                if (!_views.ContainsKey(e.Id))
                {
                    GridEntityView prefab = e.Prefab;
                    if (prefab == null) continue;
                    GridEntityView view = Instantiate(prefab, entityRoot);
                    view.name = $"{e.Kind}_{e.Id}";
                    view.Bind(e, cellSize);
                    _views[e.Id] = view;

                    // 동적 생성된 View도 ViewRequest 등록
                    RegisterViewRequest(e.Id, view);

                    // InteractableTileVisual 초기화
                    InitTileVisual(view, e.Id);
                }
            }

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