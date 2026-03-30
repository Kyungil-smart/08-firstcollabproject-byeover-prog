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

        private MapLoader _mapLoader;
        
        private readonly StageEvents _events = new StageEvents();
        private readonly TagSystem _tagSystem = new TagSystem();
        private TurnSystem _turnSystem;

        private readonly Dictionary<int, GridEntityView> _views = new Dictionary<int, GridEntityView>(16);
        private int _currentStageIndex;

        public StageEvents Events { get { return _events; } }
        public StageState CurrentState { get; private set; }
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

        private void OnTurnExecuted(TurnOutcome outcome)
        {
            LastOutcome = outcome;
            SyncViews();
            SyncSelection();
        }

        private void OnActivePlayerChanged(int id) { SyncSelection(); }

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
            return _tagSystem.Switch(CurrentState);
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

            return _turnSystem.TryExecutePlayerTurn(CurrentState, direction);
        }

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