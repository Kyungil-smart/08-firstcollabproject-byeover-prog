using System.Collections.Generic;
using UnityEngine;

namespace MyGame2.Stage
{
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

        [Header("상태 참조 SO")]
        [Tooltip("MoveComponent들이 StageState에 접근하기 위한 SO. Assets/_Game/SO/Util/StageState 에셋 연결")]
        [SerializeField] private StageStateReferenceSO stageStateReference;

        [Tooltip("되돌리기 반복 간격 (초)")]
        [SerializeField] private float undoRepeatInterval = 0.2f;

        private MapLoader _mapLoader;

        private readonly StageEvents _events = new StageEvents();
        private readonly TagSystem _tagSystem = new TagSystem();
        private TurnSystem _turnSystem;

        private readonly Dictionary<int, GridEntityView> _views = new Dictionary<int, GridEntityView>(16);
        private int _currentStageIndex;

        private Stack<StageSnapshot> snapshotStack = new Stack<StageSnapshot>();

        public StageEvents Events { get { return _events; } }
        public StageState CurrentState { get; private set; }
        public TurnOutcome LastOutcome { get; private set; }

        private float _nextUndoTime;

        private void Awake()
        {
            _mapLoader = new MapLoader(symbolRegistry);
            if (entityRoot == null) entityRoot = transform;
            _turnSystem = TurnSystemBuilder.Default().Build();
            LastOutcome = TurnOutcome.None();
            _events.TurnExecuted += OnTurnExecuted;
            _events.ActivePlayerChanged += OnActivePlayerChanged;
            _events.EntityKilled += OnEntityKilled;
            _events.UndoExecuted += OnUndoExecuted;
        }

        private void Start()
        {
            LoadStage(startStageIndex);
        }

        private void Update()
        {
            if (CurrentState == null) return;
            if (CurrentState.IsUndoProcessing)
            {
                if (snapshotStack.Count == 0)
                {
                    LeaveUndo();
                    return;
                }
                if (Time.time >= _nextUndoTime)
                {
                    _nextUndoTime = Time.time + undoRepeatInterval;
                    CurrentState.Restore(snapshotStack.Pop());
                    _events.RaiseUndoExecuted();
                    Debug.Log($"Pop SnapshotStack, Stack Size : {snapshotStack.Count}");
                }
            }
        }

        private void OnDestroy()
        {
            _events.TurnExecuted -= OnTurnExecuted;
            _events.ActivePlayerChanged -= OnActivePlayerChanged;
            _events.EntityKilled -= OnEntityKilled;
            _events.UndoExecuted -= OnUndoExecuted;
        }

        private void OnTurnExecuted(TurnOutcome outcome)
        {
            LastOutcome = outcome;
            SyncViews();
            SyncSelection();
        }

        private void OnActivePlayerChanged(int id) { SyncSelection(); }

        // 히든 함정 등 지연 Kill 후에도 뷰가 갱신되도록 처리
        private void OnEntityKilled(int id)
        {
            SyncViews();
            SyncSelection();
        }

        private void OnUndoExecuted()
        {
            SyncViews();
            SyncSelection();
        }

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

            // 자동 페어링: 맵 텍스트를 다시 스캔해서 같은 pairGroup끼리 SetCellPair
            ApplyPairGroups(file, CurrentState);

            // 핵심 추가: MoveComponent들이 StageState에 접근할 수 있도록 등록
            if (stageStateReference != null)
                stageStateReference.Register(CurrentState);

            _tagSystem.Initialize(CurrentState);

            SpawnViews();
            SyncSelection();
            LastOutcome = TurnOutcome.None();
            _events.RaiseStageLoaded(stageIndex);
        }

        public void RestartCurrentStage()
        {
            LoadStage(_currentStageIndex);
        }

        public bool LoadNextStage()
        {
            int next = _currentStageIndex + 1;
            if (next >= stageFiles.Length) { Debug.Log("[StageManager] 마지막 스테이지.", this); return false; }
            LoadStage(next);
            return true;
        }

        public bool SwitchActivePlayer()
        {
            if (!CanAcceptInput()) return false;

            bool switchResult = _tagSystem.Switch(CurrentState);
            if (switchResult)
            {
                snapshotStack.Clear();
            }

            return switchResult;
        }

        public TurnOutcome TryExecuteTurn(Direction direction)
        {
            if (!CanAcceptInput())
            {
                LastOutcome = TurnOutcome.Ignored(MoveResult.Blocked(
                    StageState.InvalidEntityId, new GridPos(0, 0), new GridPos(0, 0),
                    MoveBlockReason.DeadEntity));
                return LastOutcome;
            }

            TurnOutcome outcome = _turnSystem.TryExecutePlayerTurn(CurrentState, direction);

            if (outcome.Executed)
            {
                StageSnapshot snapshot = new StageSnapshot(CurrentState);
                snapshotStack.Push(snapshot);

                Debug.Log($"Push Snapshot into Stack, Stack Size : {snapshotStack.Count}");
            }

            return outcome;
        }

        public bool TryEnterUndo()
        {
            if (snapshotStack.Count == 0)
            {
                return false;
            }

            CurrentState.UndoEnter();
            _nextUndoTime = 0.0f;

            return true;
        }

        public void LeaveUndo()
        {
            CurrentState.UndoLeave();
        }

        private bool CanAcceptInput()
        {
            return CurrentState != null && !CurrentState.IsGameOver && !CurrentState.IsStageClear;
        }

        // 스테이지 로드 시 최초 View 생성
        private void SpawnViews()
        {
            if (prefabRegistry == null) return;
            foreach (EntityState e in CurrentState.Entities)
            {
                SpawnViewForEntity(e);
            }
        }

        // 단일 엔티티의 View 생성 (동적 스폰 지원)
        private void SpawnViewForEntity(EntityState e)
        {
            if (_views.ContainsKey(e.Id)) return;

            GridEntityView prefab = e.Prefab;
            if (prefab == null) return;

            GridEntityView view = Instantiate(prefab, entityRoot);
            view.name = $"{e.Kind}_{e.Id}";
            view.Bind(e, cellSize);

            // 톱날 함정: 멀티셀 비주얼 자동 생성
            SawTrapVisual sawVisual = view.GetComponent<SawTrapVisual>();
            if (sawVisual != null && e.Has<SawTrapData>())
            {
                sawVisual.BuildVisual(e.Get<SawTrapData>().Size, e.Facing);
            }

            Events.ViewRequestSubscribe(e.Id, view.OnRequestView);
            _views[e.Id] = view;
        }

        // View 동기화 (동적 스폰/제거 처리)
        private void SyncViews()
        {
            if (CurrentState == null) return;

            foreach (EntityState e in CurrentState.Entities)
            {
                if (!_views.ContainsKey(e.Id))
                    SpawnViewForEntity(e);
            }

            List<int> toRemove = null;
            foreach (var pair in _views)
            {
                if (pair.Value == null) continue;

                if (CurrentState.TryGetEntity(pair.Key, out EntityState e))
                {
                    pair.Value.Sync(e, cellSize);
                }
                else
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

        // 자동 페어링
        private void ApplyPairGroups(TextAsset file, StageState state)
        {
            if (symbolRegistry == null || file == null) return;

            string rawText = file.text;
            if (rawText.Length > 0 && rawText[0] == '\uFEFF')
                rawText = rawText.Substring(1);

            string normalized = rawText.Replace("\r\n", "\n").Replace('\r', '\n');
            string[] split = normalized.Split('\n');
            List<string> validLines = new List<string>(split.Length);

            for (int i = 0; i < split.Length; i++)
            {
                string line = split[i].TrimEnd();
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line.TrimStart().StartsWith("//")) continue;
                validLines.Add(line);
            }

            Dictionary<int, List<GridPos>> groups = new Dictionary<int, List<GridPos>>();

            for (int y = 0; y < validLines.Count; y++)
            {
                string row = validLines[y];
                for (int x = 0; x < row.Length; x++)
                {
                    char ch = row[x];
                    if (!symbolRegistry.TryGetEntry(ch, out MapSymbolEntry entry)) continue;
                    if (entry.pairGroup <= 0) continue;

                    if (!groups.ContainsKey(entry.pairGroup))
                        groups[entry.pairGroup] = new List<GridPos>(2);
                    groups[entry.pairGroup].Add(new GridPos(x, y));
                }
            }

            foreach (var kvp in groups)
            {
                if (kvp.Value.Count == 2)
                {
                    state.SetCellPair(kvp.Value[0], kvp.Value[1]);
                }
                else
                {
                    Debug.LogWarning(
                        $"[StageManager] PairGroup {kvp.Key}: " +
                        $"{kvp.Value.Count}개 셀 발견 (2개여야 페어 성립). 무시됨.");
                }
            }
        }
    }
}