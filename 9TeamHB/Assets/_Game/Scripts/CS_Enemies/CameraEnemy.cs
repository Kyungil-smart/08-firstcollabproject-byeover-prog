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

        // Fixed3x3: 3개 열(좌/중/우)을 각각 독립 라인으로 처리
        // 기획서(1-4-3): "감시범위에 오브젝트가 위치할 경우, 그 뒤쪽 타일은 감시범위에서 제외"
        private void AddFixed3x3Cells(StageState state, EntityState camera, List<GridPos> result)
        {
            Direction facing = camera.Facing;
            if (facing == Direction.None) facing = Direction.Down;

            Direction left = facing.RotateClockwise().RotateClockwise().RotateClockwise();
            Direction right = facing.RotateClockwise();

            // 3개 열별 독립 시야 차단 라인
            AddLineCellsbyPosition(state, camera, camera.Position.Move(left), 3, result);
            AddLineCellsbyPosition(state, camera, camera.Position, 3, result);
            AddLineCellsbyPosition(state, camera, camera.Position.Move(right), 3, result);
        }

        // 지정 위치에서 facing 방향으로 range칸 라인 감지
        // BlocksCameraSight 엔티티가 있으면 그 뒤는 감지 안 함
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