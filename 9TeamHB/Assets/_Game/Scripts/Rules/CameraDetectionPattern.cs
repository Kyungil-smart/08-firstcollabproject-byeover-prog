using System.Collections.Generic;

namespace MyGame2.Stage
{
    // 카메라 감지 범위 계산의 유일한 출처.
    // CameraEnemy와 StageTileRenderer 양쪽이 이 클래스를 호출한다.
    public static class CameraDetectionPattern
    {
        // 카메라의 감지 범위 셀 목록을 계산한다.
        public static void CollectCells(
            StageState state, EntityState camera, List<GridPos> outBuffer)
        {
            outBuffer.Clear();
            CameraData data = camera.Get<CameraData>();

            switch (data.Pattern)
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
                case CameraType.Fixed3x3:
                    AddFixed3x3(state, camera.Position, camera.Facing, outBuffer);
                    break;
            }
        }

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

        private static void AddPyramid(
            StageState state, GridPos origin, Direction facing, int rows,
            List<GridPos> result)
        {
            Direction left = facing.RotateClockwise().RotateClockwise().RotateClockwise();
            Direction right = facing.RotateClockwise();

            for (int row = 0; row < rows; row++)
            {
                GridPos center = origin;
                for (int s = 0; s <= row; s++)
                    center = center.Move(facing);

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
        private static void AddFixed3x3(
            StageState state, GridPos origin, Direction facing,
            List<GridPos> result)
        {
            Direction forward = facing;
            if (forward == Direction.None) forward = Direction.Down;
            Direction left = forward.RotateClockwise().RotateClockwise().RotateClockwise();
            Direction right = forward.RotateClockwise();

            for (int row = 0; row < 3; row++)
            {
                GridPos center = origin;
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