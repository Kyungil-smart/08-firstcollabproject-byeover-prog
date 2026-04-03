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

        // Fixed3x3: 3개 열(좌/중/우)별 독립 시야 차단
        // 기획서(1-4-3): "감시범위에 오브젝트가 위치할 경우, 그 뒤쪽 타일은 감시범위에서 제외"
        private void AddFixed3x3Cells(StageState state, EntityState camera, List<GridPos> result)
        {
            Direction facing = camera.Facing;
            if (facing == Direction.None) facing = Direction.Down;

            Direction left = facing.RotateClockwise().RotateClockwise().RotateClockwise();
            Direction right = facing.RotateClockwise();

<<<<<<< HEAD
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
=======
            // 열별 차단 상태
            bool blockedLeft = false;
            bool blockedCenter = false;
            bool blockedRight = false;

            for (int row = 0; row < 3; row++)
            {
                GridPos center = camera.Position;
                for (int s = 0; s < row; s++)
                    center = center.Move(facing);

                // 좌측 열
                if (!blockedLeft)
                    blockedLeft = AddCellWithSightCheck(state, center.Move(left), camera.Id, result);

                // 중앙 열
                if (!blockedCenter)
                    blockedCenter = AddCellWithSightCheck(state, center, camera.Id, result);

                // 우측 열
                if (!blockedRight)
                    blockedRight = AddCellWithSightCheck(state, center.Move(right), camera.Id, result);
            }
>>>>>>> 79fef92 (투사체 버그/CCTV 감시자 버그 픽스 완료)
        }

        // 셀을 result에 추가하고, 이 열이 차단되었는지 반환한다.
        // true = 이 열의 이후 행은 감시범위에서 제외해야 함.
        private static bool AddCellWithSightCheck(
            StageState state, GridPos pos, int cameraId, List<GridPos> result)
        {
            if (!state.IsInside(pos)) return false;

            CellData cell = state.GetCell(pos);

            // 벽 → 추가 안 하고 차단
            if (cell.HasWall) return true;

            result.Add(pos);

            // 시야 차단 오브젝트 체크 (카메라 자신 제외)
            if (cell.IsOccupied &&
                cell.OccupantId != cameraId &&
                state.TryGetEntity(cell.OccupantId, out EntityState occ) &&
                occ.BlocksCameraSight && occ.IsAlive)
            {
                return true;
            }

            return false;
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