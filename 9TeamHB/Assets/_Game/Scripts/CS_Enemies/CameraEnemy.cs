using System.Collections.Generic;

namespace MyGame2.Stage
{
    // 카메라 감지 로직.
    // Fixed3x3: 자기 위치(+좌우) + 앞 방향 2줄(각 3칸), 비회전
    // 부쉬 타일 위의 플레이어는 감지하지 않는다.
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

                // 부쉬 타일 위의 플레이어는 감지하지 않음
                if (cell.HasBush) continue;

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
                for (int offset = -half; offset <= half; offset++)
                {
                    GridPos pos = center;
                    if (offset < 0) for (int s = 0; s < -offset; s++) pos = pos.Move(left);
                    else if (offset > 0) for (int s = 0; s < offset; s++) pos = pos.Move(right);

                    if (!state.IsInside(pos)) continue;
                    CellData cell = state.GetCell(pos);
                    if (cell.HasWall) continue;
                    result.Add(pos);
                }
            }
        }

        private void AddFixed3x3Cells(StageState state, EntityState camera, List<GridPos> result)
        {
            // Fixed3x3: 아래 방향 기준 (s=Down, S=Up)
            Direction facing = camera.Facing;
            if (facing == Direction.None) facing = Direction.Down;

            Direction left = facing.RotateClockwise().RotateClockwise().RotateClockwise();
            Direction right = facing.RotateClockwise();

            // 본인 줄
            AddLineCellsbyPosition(state, camera, camera.Position,3, result);
            // 옆 줄
            AddLineCellsbyPosition(state, camera, camera.Position.Move(left),3, result);
            AddLineCellsbyPosition(state, camera, camera.Position.Move(right),3, result);
            // // 본인 줄 (본인 + 좌우)
            // AddIfValid(state, camera.Position, result);
            // AddIfValid(state, camera.Position.Move(left), result);
            // AddIfValid(state, camera.Position.Move(right), result);

            // // 앞 1줄
            // GridPos front1 = camera.Position.Move(facing);
            // AddIfValid(state, front1, result);
            // AddIfValid(state, front1.Move(left), result);
            // AddIfValid(state, front1.Move(right), result);

            // // 앞 2줄
            // GridPos front2 = front1.Move(facing);
            // AddIfValid(state, front2, result);
            // AddIfValid(state, front2.Move(left), result);
            // AddIfValid(state, front2.Move(right), result);
        }

        private static void AddIfValid(StageState state, GridPos pos, List<GridPos> result)
        {
            if (state.IsInside(pos) && !state.GetCell(pos).HasWall)
                result.Add(pos);
        }
        private void AddLineCellsbyPosition(StageState state, EntityState camera, GridPos pos, int range, List<GridPos> result)
        {
            for (int i = 0; i < range; i++)
            {
                if (!state.IsInside(pos)) break;
                CellData cell = state.GetCell(pos);
                if (cell.HasWall) break;
                result.Add(pos);
                if (cell.IsOccupied &&
                    state.TryGetEntity(cell.OccupantId, out EntityState occ) &&
                    occ.BlocksCameraSight && occ.IsAlive) break;
                pos = pos.Move(camera.Facing);
            }
        }
    }
}