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

        private MapLoader _mapLoader;

        // 이벤트 허브
        private readonly StageEvents _events = new StageEvents();
        private readonly TagSystem _tagSystem = new TagSystem();
        private TurnSystem _turnSystem;

        // 런타임 상태
        private readonly Dictionary<int, GridEntityView> _views = new Dictionary<int, GridEntityView>(16);
        private int _currentStageIndex;

        // 게임오버 딜레이 코루틴
        private Coroutine _gameOverDelayCoroutine;

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

            // 게임오버 -> 1초 딜레이 후 이벤트 발행
            // IsGameOver=true 시점에서 입력은 CanAcceptInput()에 의해 즉시 차단됨
            // 시간은 계속 흐르므로 사망 원인(히든 트랩 애니메이션 등)이 보임
            if (CurrentState != null && CurrentState.IsGameOver && _gameOverDelayCoroutine == null)
            {
                _gameOverDelayCoroutine = StartCoroutine(DelayedGameOver());
            }
        }

        // 1초 대기 후 게임오버 이벤트 발행
        private IEnumerator DelayedGameOver()
        {
            yield return new WaitForSeconds(1f);

            _gameOverDelayCoroutine = null;
            _events.RaiseGameOver();
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

            // 게임오버 딜레이 코루틴 정리
            if (_gameOverDelayCoroutine != null)
            {
                StopCoroutine(_gameOverDelayCoroutine);
                _gameOverDelayCoroutine = null;
            }

            _currentStageIndex = stageIndex;
            ClearViews();

            // 이전 상태의 IUpdate/IDisposable 컴포넌트 이벤트 구독 해제
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
            if (next >= stageFiles.Length)
            {
                LoadingManager.LoadScene("Ending_Scene");
                return false;
            }
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

            return _turnSystem.TryExecutePlayerTurn(CurrentState, direction);
        }

        // 페어 그룹 자동 연결

        private void ResolvePairGroups()
        {
            if (CurrentState == null) return;

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

                if (triggers.Count > 0 && doors.Count > 0)
                {
                    if (triggers.Count == 1)
                    {
                        for (int i = 0; i < doors.Count; i++)
                            CurrentState.SetCellPair(triggers[0].Position, doors[i].Position);
                    }
                    else
                    {
                        int pairCount = Mathf.Min(triggers.Count, doors.Count);
                        for (int i = 0; i < pairCount; i++)
                            CurrentState.SetCellPair(triggers[i].Position, doors[i].Position);
                    }
                }
                else if (triggers.Count == 0 && doors.Count == 0 && others.Count >= 2)
                {
                    for (int i = 0; i + 1 < others.Count; i += 2)
                    {
                        CurrentState.SetCellPair(others[i].Position, others[i + 1].Position);
                    }
                }
                else if (members.Count >= 2)
                {
                    for (int i = 0; i + 1 < members.Count; i += 2)
                    {
                        CurrentState.SetCellPair(members[i].Position, members[i + 1].Position);
                    }
                }

                if (members.Count < 2)
                {
                }
            }
        }

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

                RegisterViewRequest(e.Id, view);
                InitTileVisual(view, e.Id);
            }
        }

        private void RegisterViewRequest(int entityId, GridEntityView view)
        {
            _events.ViewRequestSubscribe(entityId, request =>
            {
                request.Callback?.Invoke(view);
            });
        }

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

                    RegisterViewRequest(e.Id, view);
                    InitTileVisual(view, e.Id);
                }
            }

            List<int> toRemove = null;
            foreach (var pair in _views)
            {
                if (pair.Value == null) continue;
                if (!CurrentState.TryGetEntity(pair.Key, out _))
                {
                    Destroy(pair.Value.gameObject);
                    if (toRemove == null) toRemove = new List<int>(4);
                    toRemove.Add(pair.Key);
                }
            }
            if (toRemove != null)
            {
                for (int i = 0; i < toRemove.Count; i++)
                    _views.Remove(toRemove[i]);
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