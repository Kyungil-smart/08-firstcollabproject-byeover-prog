using System.Collections.Generic;

namespace MyGame2.Stage
{
    // 카메라 감지 로직.
    // Fixed3x3: 자기 위치(+좌우) + 앞 방향 2줄(각 3칸), 비회전
    public sealed class CameraEnemy
    {
        public bool TryDetect(StageState state, int cameraId, out int detectedPlayerId)
        {
            detectedPlayerId = StageState.InvalidEntityId;

            if (!state.TryGetEntity(cameraId, out EntityState camera) || !camera.IsAlive)
                return false;

            List<GridPos> cells = GetDetectionCells(state, camera);

            for (int i = 0; i < cells.Count; i++)
            {
                CellData cell = state.GetCell(cells[i]);
                if (!cell.IsOccupied) continue;

                if (state.TryGetEntity(cell.OccupantId, out EntityState occupant) &&
                    occupant.IsPlayer && occupant.IsAlive)
                {
                    detectedPlayerId = occupant.Id;
                    return true;
                }
            }

            return false;
        }

        public List<GridPos> CollectSightLine(StageState state, int cameraId)
        {
            if (!state.TryGetEntity(cameraId, out EntityState camera) || !camera.IsAlive)
                return new List<GridPos>();

            return GetDetectionCells(state, camera);
        }

        private List<GridPos> GetDetectionCells(StageState state, EntityState camera)
        {
            List<GridPos> result = new List<GridPos>(16);
            CameraData data = camera.Get<CameraData>();

            switch (data.Pattern)
            {
                case CameraType.LineShort:    AddLineCells(state, camera, 3, result); break;
                case CameraType.LineLong:     AddLineCells(state, camera, 5, result); break;
                case CameraType.PyramidSmall: AddPyramidCells(state, camera, 3, result); break;
                case CameraType.PyramidLarge: AddPyramidCells(state, camera, 5, result); break;
                case CameraType.Fixed3x3:     AddFixed3x3Cells(state, camera, result); break;
            }

            return result;
        }

        private void AddLineCells(StageState state, EntityState camera, int range, List<GridPos> result)
        {
            GridPos cursor = camera.Position;
            for (int i = 0; i < range; i++)
            {
                cursor = cursor.Move(camera.Facing);
                if (!state.IsInside(cursor)) break;
                CellData cell = state.GetCell(cursor);
                if (cell.HasWall) break;
                result.Add(cursor);
                if (cell.IsOccupied &&
                    state.TryGetEntity(cell.OccupantId, out EntityState occ) &&
                    occ.BlocksCameraSight && occ.IsAlive) break;
            }
        }

        private void AddPyramidCells(StageState state, EntityState camera, int rows, List<GridPos> result)
        {
            Direction forward = camera.Facing;
            Direction left = forward.RotateClockwise().RotateClockwise().RotateClockwise();
            Direction right = forward.RotateClockwise();

            for (int row = 0; row < rows; row++)
            {
                GridPos center = camera.Position;
                for (int step = 0; step <= row; step++)
                    center = center.Move(forward);

                int half = row;
                for (int off = -half; off <= half; off++)
                {
                    GridPos cell = center;
                    if (off < 0) for (int s = 0; s < -off; s++) cell = cell.Move(left);
                    else if (off > 0) for (int s = 0; s < off; s++) cell = cell.Move(right);
                    if (!state.IsInside(cell)) continue;
                    if (state.GetCell(cell).HasWall) continue;
                    result.Add(cell);
                }
            }
        }

        // 고정형 3×3: 자기 위치(+좌우) + 앞 2줄(각 3칸)
        private void AddFixed3x3Cells(StageState state, EntityState camera, List<GridPos> result)
        {
            Direction forward = camera.Facing;
            if (forward == Direction.None) forward = Direction.Down;
            Direction left = forward.RotateClockwise().RotateClockwise().RotateClockwise();
            Direction right = forward.RotateClockwise();

            for (int row = 0; row < 3; row++)
            {
                GridPos center = camera.Position;
                for (int s = 0; s < row; s++)
                    center = center.Move(forward);

                for (int off = -1; off <= 1; off++)
                {
                    GridPos cell = center;
                    if (off == -1) cell = cell.Move(left);
                    else if (off == 1) cell = cell.Move(right);
                    if (!state.IsInside(cell)) continue;
                    if (state.GetCell(cell).HasWall) continue;
                    result.Add(cell);
                }
            }
        }
    }
}