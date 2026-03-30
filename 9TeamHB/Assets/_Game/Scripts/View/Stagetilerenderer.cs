using System.Collections.Generic;
using UnityEngine;

namespace MyGame2.Stage
{
    public sealed class StageTileRenderer : MonoBehaviour
    {
        [Header("씬 참조")]
        [SerializeField] private StageManager stageManager;

        [Header("타일 색상")]
        [SerializeField] private Color wallColor = new Color(0.15f, 0.15f, 0.18f, 1f);
        [SerializeField] private Color floorColor = new Color(0.85f, 0.87f, 0.90f, 1f);
        [SerializeField] private Color goalColor = new Color(1f, 0.84f, 0f, 1f);
        [SerializeField] private Color trapColor = new Color(0.55f, 0.15f, 0.70f, 1f);

        [Header("카메라 감시 범위")]
        [SerializeField] private Color cameraDetectionColor = new Color(1f, 0.2f, 0.2f, 0.25f);

        [Header("로봇 감지 범위")]
        [SerializeField] private Color robotDetectionColor = new Color(1f, 0.6f, 0f, 0.3f);

        [Header("B감시자(Summoner) 감지 범위")]
        [SerializeField] private Color summonerDetectionColor = new Color(0.2f, 0.6f, 1f, 0.3f);

        [Header("렌더링")]
        [SerializeField] private int tileOrder = -1;
        [SerializeField] private int detectionOrder = 0;
        [SerializeField] private float tilePadding = 0.05f;

        private readonly List<GameObject> _tiles = new List<GameObject>(256);
        private readonly List<GameObject> _camDetections = new List<GameObject>(64);
        private readonly List<GameObject> _robotDetections = new List<GameObject>(16);
        private readonly List<GameObject> _summonerDetections = new List<GameObject>(16);
        private Transform _tileRoot;
        private Transform _camDetRoot;
        private Transform _robotDetRoot;
        private Transform _summonerDetRoot;

        private CameraEnemy _cameraEnemy;
        private RobotEnemy _robotEnemy;
        private DetectionArea3x3 _detector3x3;

        private void Awake()
        {
            _cameraEnemy = new CameraEnemy();
            _robotEnemy = new RobotEnemy();
            _detector3x3 = new DetectionArea3x3();
        }

        private void OnEnable()
        {
            if (stageManager == null) return;
            stageManager.Events.StageLoaded += OnStageLoaded;
            stageManager.Events.TurnExecuted += OnTurnExecuted;
        }

        private void OnDisable()
        {
            if (stageManager == null) return;
            stageManager.Events.StageLoaded -= OnStageLoaded;
            stageManager.Events.TurnExecuted -= OnTurnExecuted;
        }

        private void Start()
        {
            if (stageManager != null && stageManager.CurrentState != null)
                RefreshAll(stageManager.CurrentState);
        }

        private void OnStageLoaded(int idx)
        {
            RefreshAll(stageManager.CurrentState);
        }

        private void OnTurnExecuted(TurnOutcome outcome)
        {
            RefreshAll(stageManager.CurrentState);
        }

        private void RefreshAll(StageState state)
        {
            RenderTiles(state);
            RenderCameraDetection(state);
            RenderRobotDetection(state);
            RenderSummonerDetection(state);
        }

        // ── 타일 ──

        private void RenderTiles(StageState state)
        {
            Clear(_tiles);
            EnsureRoot(ref _tileRoot, "_TileRoot");
            float scale = 1f - tilePadding;

            for (int y = 0; y < state.Height; y++)
            for (int x = 0; x < state.Width; x++)
            {
                GridPos pos = new GridPos(x, y);
                CellData cell = state.GetCell(pos);

                Color c;
                if (cell.HasWall)       c = wallColor;
                else if (cell.HasTrap)  c = trapColor;
                else if (cell.HasGoal)  c = goalColor;
                else                    c = floorColor;

                _tiles.Add(MakeSprite($"T_{x}_{y}", _tileRoot, pos.ToWorld(1f), scale, c, tileOrder));
            }
        }

        // ── 카메라 감시 범위 ──

        private void RenderCameraDetection(StageState state)
        {
            Clear(_camDetections);
            EnsureRoot(ref _camDetRoot, "_CamDetRoot");
            float scale = 1f - tilePadding;

            for (int i = 0; i < state.CameraIds.Count; i++)
            {
                List<GridPos> cells = _cameraEnemy.CollectSightLine(
                    state, state.CameraIds[i]);

                for (int j = 0; j < cells.Count; j++)
                {
                    _camDetections.Add(MakeSprite(
                        $"CD_{state.CameraIds[i]}_{j}", _camDetRoot,
                        cells[j].ToWorld(1f), scale, cameraDetectionColor, detectionOrder));
                }
            }
        }

        // ── 로봇 감지 범위 (앞 2칸 + 뒤 2칸) ──

        private void RenderRobotDetection(StageState state)
        {
            Clear(_robotDetections);
            EnsureRoot(ref _robotDetRoot, "_RobotDetRoot");
            float scale = 1f - tilePadding;

            for (int i = 0; i < state.RobotIds.Count; i++)
            {
                List<GridPos> cells = _robotEnemy.CollectDetectionCells(
                    state, state.RobotIds[i]);

                for (int j = 0; j < cells.Count; j++)
                {
                    _robotDetections.Add(MakeSprite(
                        $"RD_{state.RobotIds[i]}_{j}", _robotDetRoot,
                        cells[j].ToWorld(1f), scale, robotDetectionColor, detectionOrder));
                }
            }
        }

        // ── B감시자(Summoner) 3×3 감지 범위 ──

        private void RenderSummonerDetection(StageState state)
        {
            Clear(_summonerDetections);
            EnsureRoot(ref _summonerDetRoot, "_SummonerDetRoot");
            float scale = 1f - tilePadding;

            for (int i = 0; i < state.SummonerIds.Count; i++)
            {
                int summonerId = state.SummonerIds[i];
                if (!state.TryGetEntity(summonerId, out EntityState summoner) || !summoner.IsAlive)
                    continue;

                // 3×3 감지범위 수집 (오브젝트 무시 = true, 기획서 4-5-2)
                List<GridPos> cells = _detector3x3.CollectDetectionCells(
                    state, summoner.Position, summoner.Facing, true);

                for (int j = 0; j < cells.Count; j++)
                {
                    _summonerDetections.Add(MakeSprite(
                        $"SD_{summonerId}_{j}", _summonerDetRoot,
                        cells[j].ToWorld(1f), scale, summonerDetectionColor, detectionOrder));
                }
            }
        }

        // 유틸리티

        private GameObject MakeSprite(string name, Transform parent, Vector3 pos,
            float scale, Color color, int order)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.position = pos;
            obj.transform.localScale = new Vector3(scale, scale, 1f);

            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = GetSquare();
            sr.color = color;
            sr.sortingOrder = order;
            return obj;
        }

        private void Clear(List<GameObject> list)
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i] != null) Destroy(list[i]);
            list.Clear();
        }

        private void EnsureRoot(ref Transform root, string name)
        {
            if (root != null) return;
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(transform, false);
            root = obj.transform;
        }

        private static Sprite _sq;
        private static Sprite GetSquare()
        {
            if (_sq != null) return _sq;
            Texture2D t = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            Color[] px = new Color[16];
            for (int i = 0; i < 16; i++) px[i] = Color.white;
            t.SetPixels(px); t.Apply(); t.filterMode = FilterMode.Point;
            _sq = Sprite.Create(t, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            return _sq;
        }
    }
}