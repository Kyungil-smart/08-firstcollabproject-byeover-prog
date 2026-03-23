using System.Collections.Generic;
using UnityEngine;

namespace MyGame2.Stage
{
    // 스테이지 로드 시 그리드 타일 + 카메라 감시 범위를 시각화한다.
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

        private readonly List<GameObject> _tiles = new List<GameObject>(128);
        private readonly List<GameObject> _detections = new List<GameObject>(64);
        private Transform _tileRoot;
        private Transform _detRoot;

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
            // 함정 덮임/상자 이동 반영을 위해 타일도 갱신
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

        // ── 카메라 감시 범위 ──

        private void RenderDetection(StageState state)
        {
            Clear(_detections);
            EnsureRoot(ref _detRoot, "_DetRoot");
            float scale = 1f - tilePadding;

            for (int i = 0; i < state.CameraIds.Count; i++)
            {
                if (!state.TryGetEntity(state.CameraIds[i], out EntityState cam)) continue;
                if (!cam.IsAlive) continue;

                List<GridPos> cells = GetDetectionCells(state, cam);
                for (int j = 0; j < cells.Count; j++)
                {
                    _detections.Add(MakeSprite(
                        $"D_{cam.Id}_{j}", _detRoot,
                        cells[j].ToWorld(1f), scale, detectionColor, detectionOrder));
                }
            }
        }

        private List<GridPos> GetDetectionCells(StageState state, EntityState cam)
        {
            List<GridPos> result = new List<GridPos>(16);

            switch (cam.Camera.Pattern)
            {
                case CameraType.LineShort:    AddLine(state, cam, 3, result); break;
                case CameraType.LineLong:     AddLine(state, cam, 5, result); break;
                case CameraType.PyramidSmall: AddPyramid(state, cam, 3, result); break;
                case CameraType.PyramidLarge: AddPyramid(state, cam, 5, result); break;
            }

            return result;
        }

        // 직선 감지 (1×range)
        private void AddLine(StageState state, EntityState cam, int range, List<GridPos> result)
        {
            GridPos cursor = cam.Position;
            for (int i = 0; i < range; i++)
            {
                cursor = cursor.Move(cam.Facing);
                if (!state.IsInside(cursor)) break;

                CellData cell = state.GetCell(cursor);
                if (cell.HasWall) break;

                result.Add(cursor);

                if (cell.IsOccupied &&
                    state.TryGetEntity(cell.OccupantId, out EntityState occ) &&
                    occ.BlocksCameraSight && occ.IsAlive)
                    break;
            }
        }

        // 피라미드 감지 (rows줄: 1, 3, 5, 7, 9칸...)
        private void AddPyramid(StageState state, EntityState cam, int rows, List<GridPos> result)
        {
            Direction fwd = cam.Facing;
            Direction left = fwd.RotateClockwise().RotateClockwise().RotateClockwise();
            Direction right = fwd.RotateClockwise();

            for (int row = 0; row < rows; row++)
            {
                // center = 카메라로부터 forward (row+1)칸
                GridPos center = cam.Position;
                for (int s = 0; s <= row; s++)
                    center = center.Move(fwd);

                int half = row;
                for (int off = -half; off <= half; off++)
                {
                    GridPos cell = center;
                    if (off < 0)
                        for (int s = 0; s < -off; s++) cell = cell.Move(left);
                    else if (off > 0)
                        for (int s = 0; s < off; s++) cell = cell.Move(right);

                    if (!state.IsInside(cell)) continue;
                    if (state.GetCell(cell).HasWall) continue;

                    result.Add(cell);
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