using System.Collections.Generic;
using UnityEngine;

namespace MyGame2.Stage
{
    public sealed class StageTileRenderer : MonoBehaviour
    {
        [Header("씬 참조")]
        [SerializeField] private StageManager stageManager;

        [Header("타일 색상 (스프라이트 없을 때 폴백)")]
        [SerializeField] private Color wallColor = new Color(0.15f, 0.15f, 0.18f, 1f);
        [SerializeField] private Color floorColor = new Color(0.85f, 0.87f, 0.90f, 1f);
        [SerializeField] private Color crackColor = new Color(0.05f, 0.05f, 0.05f, 1f);
        [SerializeField] private Color goalColor = new Color(1f, 0.84f, 0f, 1f);
        [SerializeField] private Color trapColor = new Color(0.55f, 0.15f, 0.70f, 1f);
        [SerializeField] private Color teleportColor = new Color(0.7f, 0.7f, 0.99f, 1f);
        [SerializeField] private Color buttonColor = new Color(0.9f, 0.2f, 0.2f, 1f);
        [SerializeField] private Color switchColor = new Color(0.9f, 0.6f, 0.6f, 1f);
        [SerializeField] private Color opendDoorColor = new Color(0.9f, 0.7f, 0.4f, 1f);
        [SerializeField] private Color closedDoorColor = new Color(0.4f, 0.3f, 0.2f, 1f);

        [Header("타일 스프라이트 (비우면 색상 사각형 사용)")]
        [SerializeField] private Sprite wallSpriteFace;
        [SerializeField] private Sprite wallSpriteSide;
        [SerializeField] private Sprite floorABase;
        [SerializeField] private Sprite floorAAdd;
        [SerializeField] private Sprite crackSprite;
        [SerializeField] private Sprite goalSprite;
        [SerializeField] private Sprite trapSprite;
        [SerializeField] private Sprite teleportSprite;
        [SerializeField] private Sprite buttonSprite;
        [SerializeField] private Sprite switchSprite;
        [SerializeField] private Sprite openedDoorSprite;
        [SerializeField] private Sprite closedDoorSprite;

        [Header("카메라 감시 범위")]
        [SerializeField] private Color cameraDetectionColor = new Color(1f, 0.2f, 0.2f, 0.25f);

        [Header("로봇 감지 범위")]
        [SerializeField] private Color robotDetectionColor = new Color(1f, 0.6f, 0f, 0.3f);

        [Header("B감시자(Summoner) 감지 범위")]
        [SerializeField] private Color summonerDetectionColor = new Color(0.2f, 0.6f, 1f, 0.3f);

        [Header("렌더링 레이어")]
        [Tooltip("틈새 타일 sortingOrder")]
        [SerializeField] private int crackTileOrder = -4;
        [Tooltip("바닥/벽/골 등 일반 타일 sortingOrder")]
        [SerializeField] private int tileOrder = -2;
        [Tooltip("함정 타일 sortingOrder")]
        [SerializeField] private int trapTileOrder = -1;
        [SerializeField] private int detectionOrder = 0;
        [SerializeField] private float tilePadding = 0.05f;

        private readonly List<GameObject> _tiles = new List<GameObject>(256);
        private readonly List<GameObject> _camDetections = new List<GameObject>(64);
        private readonly List<GameObject> _robotDetections = new List<GameObject>(16);
        private readonly List<GameObject> _summonerDetections = new List<GameObject>(16);
        private readonly List<GameObject> _animalDetections = new List<GameObject>(16);
        private Transform _tileRoot;
        private Transform _camDetRoot;
        private Transform _robotDetRoot;
        private Transform _summonerDetRoot;
        private Transform _animalDetRoot;

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
            stageManager.Events.UndoExecuted += OnUndoExecuted;
        }

        private void OnDisable()
        {
            if (stageManager == null) return;
            stageManager.Events.StageLoaded -= OnStageLoaded;
            stageManager.Events.TurnExecuted -= OnTurnExecuted;
            stageManager.Events.UndoExecuted -= OnUndoExecuted;
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

        private void OnUndoExecuted()
        {
            RefreshAll(stageManager.CurrentState);
        }

        private void RefreshAll(StageState state)
        {
            RenderTiles(state);
            RenderCameraDetection(state);
            RenderRobotDetection(state);
            RenderSummonerDetection(state);
            RenderAnimalDetection(state);
        }

        // 타일

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
                Vector3 worldPos = pos.ToWorld(1f);

                // 벽은 바닥 없이 벽만
                if (cell.HasWall)
                {
                    Sprite wallSprite = cell.HasExtra ? wallSpriteSide : wallSpriteFace;
                    _tiles.Add(MakeTile($"T_{x}_{y}", _tileRoot, worldPos, scale,
                        wallColor, wallSprite, tileOrder));
                    continue;
                }

                // 벽이 아닌 모든 셀에 바닥 깔기 (틈새는 바닥 없이 틈새만)
                if (!cell.HasCrack)
                {
                    Sprite floorSprite = cell.IsExtraTile ? floorAAdd : floorABase;
                    _tiles.Add(MakeTile($"F_{x}_{y}", _tileRoot, worldPos, scale,
                        floorColor, floorSprite, tileOrder));
                }

                // 특수 타일은 바닥 위에 오버레이 (tileOrder + 1)
                // 문/버튼/레버는 엔티티 프리팹이 비주얼을 담당하므로 오버레이 없음
                int overlayOrder = tileOrder + 1;

                if (cell.HasTrap)
                {
                    _tiles.Add(MakeTile($"T_{x}_{y}", _tileRoot, worldPos, scale,
                        trapColor, trapSprite, trapTileOrder));
                }
                else if (cell.HasGoal)
                {
                    _tiles.Add(MakeTile($"T_{x}_{y}", _tileRoot, worldPos, scale,
                        goalColor, goalSprite, overlayOrder));
                }
                else if (cell.HasCrack)
                {
                    _tiles.Add(MakeTile($"T_{x}_{y}", _tileRoot, worldPos, scale,
                        crackColor, crackSprite, crackTileOrder));
                }
                else if (cell.HasTeleport)
                {
                    _tiles.Add(MakeTile($"T_{x}_{y}", _tileRoot, worldPos, scale,
                        teleportColor, teleportSprite, overlayOrder));
                }
                // 문/버튼/레버 오버레이 제거 — InteractableTileVisual(엔티티 프리팹)이 처리
            }
        }

        // 카메라 감시 범위

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
                    _camDetections.Add(MakeTile(
                        $"CD_{state.CameraIds[i]}_{j}", _camDetRoot,
                        cells[j].ToWorld(1f), scale, cameraDetectionColor, null, detectionOrder));
                }
            }
        }

        // 로봇 감지 범위

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
                    _robotDetections.Add(MakeTile(
                        $"RD_{state.RobotIds[i]}_{j}", _robotDetRoot,
                        cells[j].ToWorld(1f), scale, robotDetectionColor, null, detectionOrder));
                }
            }
        }

        // B감시자(Summoner) 3×3 감지 범위

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

                List<GridPos> cells = _detector3x3.CollectDetectionCells(
                    state, summoner.Position, summoner.Facing, true);

                for (int j = 0; j < cells.Count; j++)
                {
                    _summonerDetections.Add(MakeTile(
                        $"SD_{summonerId}_{j}", _summonerDetRoot,
                        cells[j].ToWorld(1f), scale, summonerDetectionColor, null, detectionOrder));
                }
            }
        }
        
        // 동물 감시자 3x3 감지 범위
        private void RenderAnimalDetection(StageState state)
        {
            Clear(_animalDetections);
            EnsureRoot(ref _animalDetRoot, "_AnimalDetRoot");
            float scale = 1f - tilePadding;

            for (int i = 0; i < state.AnimalIds.Count; i++)
            {
                int animalId = state.AnimalIds[i];
                if(!state.TryGetEntity(animalId, out EntityState animal) || !animal.IsAlive)
                    continue;
                
                List<GridPos> cells = _detector3x3.CollectDetectionCells(
                    state, animal.Position, animal.Facing, true);
                
                for (int j = 0; j < cells.Count; j++)
                {
                    _animalDetections.Add(MakeTile(
                        $"SD_{animalId}_{j}", _animalDetRoot,
                        cells[j].ToWorld(1f), scale, cameraDetectionColor,null, detectionOrder));
                }
            }
        }

        // 유틸리티

        // 스프라이트가 있으면 스프라이트 사용 (Color.white), 없으면 색상 사각형 폴백
        private GameObject MakeTile(string name, Transform parent, Vector3 pos,
            float scale, Color fallbackColor, Sprite tileSprite, int order)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.position = pos;
            obj.transform.localScale = new Vector3(scale, scale, 1f);

            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();

            if (tileSprite != null)
            {
                sr.sprite = tileSprite;
                sr.color = Color.white;
            }
            else
            {
                sr.sprite = GetSquare();
                sr.color = fallbackColor;
            }

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