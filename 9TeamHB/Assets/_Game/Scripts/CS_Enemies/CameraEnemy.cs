using System.Collections.Generic;

namespace MyGame2.Stage
{
    // 카메라 감지 로직.
    // CameraType에 따라 감지 범위가 달라진다.
    // LineShort: 직선 3칸
    // LineLong: 직선 5칸
    // PyramidSmall: 피라미드 3줄 (1+3+5칸)
    // PyramidLarge: 피라미드 5줄 (1+3+5+7+9칸)
    public sealed class CameraEnemy
    {
        // 카메라가 플레이어를 감지하는지 판정한다.
        public bool TryDetect(StageState state, int cameraId, out int detectedPlayerId)
        {
            detectedPlayerId = StageState.InvalidEntityId;

            if (!state.TryGetEntity(cameraId, out EntityState camera) || !camera.IsAlive)
            {
                return false;
            }

            List<GridPos> cells = GetDetectionCells(state, camera);

            for (int i = 0; i < cells.Count; i++)
            {
                CellData cell = state.GetCell(cells[i]);
                if (!cell.IsOccupied)
                {
                    continue;
                }

                if (state.TryGetEntity(cell.OccupantId, out EntityState occupant) &&
                    occupant.IsPlayer && occupant.IsAlive)
                {
                    detectedPlayerId = occupant.Id;
                    return true;
                }
            }

            return false;
        }

        // 카메라의 감지 범위 셀 목록을 반환한다 (시각화용).
        public List<GridPos> CollectSightLine(StageState state, int cameraId)
        {
            if (!state.TryGetEntity(cameraId, out EntityState camera) || !camera.IsAlive)
            {
                return new List<GridPos>();
            }

            return GetDetectionCells(state, camera);
        }

        // 카메라 타입과 방향에 따른 감지 셀 계산.
        private List<GridPos> GetDetectionCells(StageState state, EntityState camera)
        {
            List<GridPos> result = new List<GridPos>(16);

            switch (camera.DetectionPattern)
            {
                case CameraType.LineShort:
                    AddLineCells(state, camera, 3, result);
                    break;
                case CameraType.LineLong:
                    AddLineCells(state, camera, 5, result);
                    break;
                case CameraType.PyramidSmall:
                    AddPyramidCells(state, camera, 3, result);
                    break;
                case CameraType.PyramidLarge:
                    AddPyramidCells(state, camera, 5, result);
                    break;
            }

            return result;
        }

        // 직선형 감지 (1×range). 벽/상자에 의해 차단된다.
        private void AddLineCells(StageState state, EntityState camera, int range, List<GridPos> result)
        {
            GridPos cursor = camera.Position;

            for (int i = 0; i < range; i++)
            {
                cursor = cursor.Move(camera.Facing);

                if (!state.IsInside(cursor))
                    break;

                CellData cell = state.GetCell(cursor);
                if (cell.HasWall)
                    break;

                result.Add(cursor);

                // 시야 차단 엔티티 확인 (상자 등)
                if (cell.IsOccupied &&
                    state.TryGetEntity(cell.OccupantId, out EntityState occupant) &&
                    occupant.BlocksCameraSight && occupant.IsAlive)
                {
                    break;
                }
            }
        }
        
        // 피라미드형 감지.
        // rows줄: 1줄째 1칸, 2줄째 3칸, 3줄째 5칸...
        // 각 칸별로 벽 판정을 개별 수행한다.
        
        private void AddPyramidCells(StageState state, EntityState camera, int rows, List<GridPos> result)
        {
            Direction forward = camera.Facing;
            // 반시계 = 시계×3
            Direction left = forward.RotateClockwise().RotateClockwise().RotateClockwise();
            Direction right = forward.RotateClockwise();

            for (int row = 0; row < rows; row++)
            {
                // center = 카메라로부터 forward (row+1)칸
                GridPos center = camera.Position;
                for (int step = 0; step <= row; step++)
                {
                    center = center.Move(forward);
                }

                // 이 줄의 너비: 1 + 2*row
                int halfWidth = row;

                for (int offset = -halfWidth; offset <= halfWidth; offset++)
                {
                    GridPos cell = center;

                    if (offset < 0)
                    {
                        for (int s = 0; s < -offset; s++)
                            cell = cell.Move(left);
                    }
                    else if (offset > 0)
                    {
                        for (int s = 0; s < offset; s++)
                            cell = cell.Move(right);
                    }

                    if (!state.IsInside(cell))
                        continue;

                    CellData cellData = state.GetCell(cell);
                    if (cellData.HasWall)
                        continue;

                    result.Add(cell);
                }
            }
        }
    }
}