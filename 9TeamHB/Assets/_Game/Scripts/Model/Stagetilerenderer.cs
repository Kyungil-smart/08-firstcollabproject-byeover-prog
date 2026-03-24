using System.Collections.Generic;
using UnityEngine;

namespace MyGame2.Stage
{
    // 스테이지 로드 시 그리드 타일 + 카메라 감시 범위를 시각화한다.
    // 감시 범위 계산은 CameraEnemy.CollectSightLine에 위임한다.
    // 카메라가 매 턴 회전하므로 감시 범위도 매 턴 갱신된다.
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
        [SerializeField] private Color detectionColor = new Color(1f, 0.2f, 0.2f, 0.25f);

        [Header("렌더링")]
        [SerializeField] private int tileOrder = -1;
        [SerializeField] private int detectionOrder = 0;
        [SerializeField] private float tilePadding = 0.05f;

        private readonly List<GameObject> _tiles = new List<GameObject>(256);
        private readonly List<GameObject> _detections = new List<GameObject>(64);
        private Transform _tileRoot;
        private Transform _detRoot;

        // 감시 범위 계산은 이 인스턴스에 위임 (중복 코드 제거)
        private CameraEnemy _cameraEnemy;

        private void Awake()
        {
            _cameraEnemy = new CameraEnemy();
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
            {
                RenderTiles(stageManager.CurrentState);
                RenderDetection(stageManager.CurrentState);
            }
        }

        private void OnStageLoaded(int idx)
        {
            RenderTiles(stageManager.CurrentState);
            RenderDetection(stageManager.CurrentState);
        }

        private void OnTurnExecuted(TurnOutcome outcome)
        {
            // 함정 재활성화/상자 이동 반영을 위해 타일도 갱신
            RenderTiles(stageManager.CurrentState);
            RenderDetection(stageManager.CurrentState);
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

        // ── 카메라 감시 범위 — CameraEnemy.CollectSightLine에 위임 ──

        private void RenderDetection(StageState state)
        {
            Clear(_detections);
            EnsureRoot(ref _detRoot, "_DetRoot");
            float scale = 1f - tilePadding;

            for (int i = 0; i < state.CameraIds.Count; i++)
            {
                // CollectSightLine이 Fixed3x3 포함 모든 타입을 처리한다
                List<GridPos> cells = _cameraEnemy.CollectSightLine(
                    state, state.CameraIds[i]);

                for (int j = 0; j < cells.Count; j++)
                {
                    _detections.Add(MakeSprite(
                        $"D_{state.CameraIds[i]}_{j}", _detRoot,
                        cells[j].ToWorld(1f), scale, detectionColor, detectionOrder));
                }
            }
        }

        // ── 유틸리티 ──

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