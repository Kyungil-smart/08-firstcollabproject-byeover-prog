using System.Collections.Generic;

namespace MyGame2.Stage
{
    // 카메라 감지 범위 계산의 유일한 출처.
    // CameraEnemy와 StageTileRenderer 양쪽이 이 클래스를 호출한다.
    // 자체 상태 없음 — 순수 함수 모음.
    public static class CameraDetectionPattern
    {
        // 카메라의 감지 범위 셀 목록을 계산한다.
        public static void CollectCells(
            StageState state, EntityState camera, List<GridPos> outBuffer)
        {
            outBuffer.Clear();

            switch (camera.Camera.Pattern)
            {
                case CameraType.LineShort:
                    AddLine(state, camera.Position, camera.Facing, 3, outBuffer);
                    break;
                case CameraType.LineLong:
                    AddLine(state, camera.Position, camera.Facing, 5, outBuffer);
                    break;
                case CameraType.PyramidSmall:
                    AddPyramid(state, camera.Position, camera.Facing, 3, outBuffer);
                    break;
                case CameraType.PyramidLarge:
                    AddPyramid(state, camera.Position, camera.Facing, 5, outBuffer);
                    break;
            }
        }

        // 직선 감지 (1×range). 벽/시야차단 엔티티에 의해 중단.
        private static void AddLine(
            StageState state, GridPos origin, Direction facing, int range,
            List<GridPos> result)
        {
            GridPos cursor = origin;
            for (int i = 0; i < range; i++)
            {
                cursor = cursor.Move(facing);
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
        
        // 피라미드 감지 (rows줄).
        // 1줄째: 1칸, 2줄째: 3칸, 3줄째: 5칸...
        // 각 칸별 벽 판정은 개별 수행.
        private static void AddPyramid(
            StageState state, GridPos origin, Direction facing, int rows,
            List<GridPos> result)
        {
            Direction left = facing.RotateClockwise().RotateClockwise().RotateClockwise();
            Direction right = facing.RotateClockwise();

            for (int row = 0; row < rows; row++)
            {
                // center = origin에서 forward로 (row+1)칸
                GridPos center = origin;
                for (int s = 0; s <= row; s++)
                    center = center.Move(facing);

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
    }
}
