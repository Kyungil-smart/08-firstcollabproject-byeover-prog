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
                    AddPyramid(state, camera, 3, outBuffer);
                    break;
                case CameraType.PyramidLarge:
                    AddPyramid(state, camera, 5, outBuffer);
                    break;
                case CameraType.Fixed3x3:
                    AddFixed3x3(state, camera, outBuffer);
                    break;
            }
        }

        // Line 패턴 (기존 — 정상)

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

        // Pyramid 패턴 (수정)
        // 시그니처 변경: origin → camera (카메라 ID 제외용)

        private static void AddPyramid(
            StageState state, EntityState camera, int rows,
            List<GridPos> result)
        {
            Direction facing = camera.Facing;
            Direction left = facing.RotateClockwise().RotateClockwise().RotateClockwise();
            Direction right = facing.RotateClockwise();

            for (int row = 0; row < rows; row++)
            {
                GridPos center = camera.Position;
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
                    // Pyramid에서도 시야 차단은 같은 열의 다음 행에 영향을 주어야 하지만,
                    // Pyramid는 행마다 열 수가 변해서 단순 열 차단이 어려움.
                    // 기획서 상 Pyramid에 대한 차단 스펙이 없으므로 우선 현행 유지.
                }
            }
        }

        // Fixed3x3 패턴 (수정됨)
        // 열별 독립적 시야 차단 처리

        private static void AddFixed3x3(
            StageState state, EntityState camera, List<GridPos> result)
        {
            Direction forward = camera.Facing;
            if (forward == Direction.None) forward = Direction.Down;
            Direction left = forward.RotateClockwise().RotateClockwise().RotateClockwise();
            Direction right = forward.RotateClockwise();

            bool blockedLeft = false;
            bool blockedCenter = false;
            bool blockedRight = false;

            for (int row = 0; row < 3; row++)
            {
                GridPos center = camera.Position;
                for (int s = 0; s < row; s++)
                    center = center.Move(forward);

                // 좌측 열
                if (!blockedLeft)
                    blockedLeft = TryAddCell(state, center.Move(left), camera.Id, result);

                // 중앙 열
                if (!blockedCenter)
                    blockedCenter = TryAddCell(state, center, camera.Id, result);

                // 우측 열
                if (!blockedRight)
                    blockedRight = TryAddCell(state, center.Move(right), camera.Id, result);
            }
        }
        
        // 셀을 result에 추가하고, 시야 차단 여부를 반환한다.
        // true 반환 = 이 열의 이후 행은 추가하지 않아야 함.

        private static bool TryAddCell(
            StageState state, GridPos pos, int cameraId, List<GridPos> result)
        {
            if (!state.IsInside(pos)) return false;

            CellData cell = state.GetCell(pos);

            // 벽 -> 추가 안 하고 차단
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
    }
}