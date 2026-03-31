using System.Collections.Generic;

namespace MyGame2.Stage
{
    // 이동형 감시자 공용 3×3 감시범위 판정.

    public sealed class DetectionArea3x3
    {
        // 감시범위 내 플레이어를 감지한다.
        // center: 감시자의 현재 좌표
        // facing: 감시자가 바라보는 방향 (진행방향 기준 3x3)
        // ignoreObjects: true면 오브젝트를 무시하고 감시 (새 감시자A/B)
        // detectedPlayerId: 감지된 플레이어 ID (없으면 InvalidEntityId)
        public bool TryDetect(StageState state, GridPos center, Direction facing,
            bool ignoreObjects, out int detectedPlayerId)
        {
            detectedPlayerId = StageState.InvalidEntityId;

            if (facing == Direction.None) facing = Direction.Down;

            // 3×3 범위: 감시자 위치 기준 자기 줄 + 앞 1줄 + 앞 2줄
            // 각 줄은 좌-중앙-우 3칸
            Direction left = facing.RotateClockwise().RotateClockwise().RotateClockwise();
            Direction right = facing.RotateClockwise();

            for (int row = 0; row < 3; row++)
            {
                GridPos rowCenter = center;
                for (int s = 0; s < row; s++)
                    rowCenter = rowCenter.Move(facing);

                for (int offset = -1; offset <= 1; offset++)
                {
                    GridPos cell = rowCenter;
                    if (offset == -1) cell = cell.Move(left);
                    else if (offset == 1) cell = cell.Move(right);

                    if (!state.IsInside(cell)) continue;

                    CellData cellData = state.GetCell(cell);
                    if (cellData.HasWall) continue;

                    // 부쉬 타일 위의 플레이어는 감지 불가
                    if (cellData.HasBush) continue;

                    // 오브젝트 차단 체크 (ignoreObjects가 false인 경우만)
                    if (!ignoreObjects && !cell.Equals(center))
                    {
                        if (IsBlockedByObject(state, center, cell))
                            continue;
                    }

                    // 이 셀에 살아있는 플레이어가 있는지 확인
                    if (cellData.IsOccupied &&
                        state.TryGetEntity(cellData.OccupantId, out EntityState occupant) &&
                        occupant.IsPlayer && occupant.IsAlive)
                    {
                        detectedPlayerId = occupant.Id;
                        return true;
                    }
                }
            }

            return false;
        }

        // 감시범위 셀 목록을 수집한다 (시각화/함정화용).
        // 반환 리스트에는 감시자 자신의 위치도 포함될 수 있다.
        public List<GridPos> CollectDetectionCells(StageState state, GridPos center,
            Direction facing, bool ignoreObjects)
        {
            var result = new List<GridPos>(9);

            if (facing == Direction.None) facing = Direction.Down;

            Direction left = facing.RotateClockwise().RotateClockwise().RotateClockwise();
            Direction right = facing.RotateClockwise();

            for (int row = 0; row < 3; row++)
            {
                GridPos rowCenter = center;
                for (int s = 0; s < row; s++)
                    rowCenter = rowCenter.Move(facing);

                for (int offset = -1; offset <= 1; offset++)
                {
                    GridPos cell = rowCenter;
                    if (offset == -1) cell = cell.Move(left);
                    else if (offset == 1) cell = cell.Move(right);

                    if (!state.IsInside(cell)) continue;
                    if (state.GetCell(cell).HasWall) continue;

                    if (!ignoreObjects && !cell.Equals(center))
                    {
                        if (IsBlockedByObject(state, center, cell))
                            continue;
                    }

                    result.Add(cell);
                }
            }

            return result;
        }

        // center에서 target까지 직선 경로 상에 시야를 차단하는 오브젝트가 있는지 확인.
        // 간단한 인접 셀 체크: 3x3 범위이므로 최대 2칸 거리.
        // center와 target 사이에 BlocksCameraSight인 살아있는 엔티티가 있으면 차단.
        private bool IsBlockedByObject(StageState state, GridPos center, GridPos target)
        {
            // 인접 1칸(row 0)은 차단 체크 불필요
            int dx = target.X - center.X;
            int dy = target.Y - center.Y;
            int dist = (dx < 0 ? -dx : dx) + (dy < 0 ? -dy : dy);

            if (dist <= 1) return false;

            // 2칸 거리일 때 중간 칸 체크
            // 중간 칸은 center에서 target 방향으로 1칸
            int midX = center.X;
            int midY = center.Y;

            // 2칸 거리의 중간 칸 후보 계산
            if (dx != 0) midX = center.X + (dx > 0 ? 1 : -1);
            if (dy != 0) midY = center.Y + (dy > 0 ? 1 : -1);

            // 대각선이 아닌 직선 이동인 경우
            if (dx == 0 || dy == 0)
            {
                GridPos mid = new GridPos(midX, midY);
                if (state.IsInside(mid))
                {
                    CellData midCell = state.GetCell(mid);
                    if (midCell.IsOccupied &&
                        state.TryGetEntity(midCell.OccupantId, out EntityState blocker) &&
                        blocker.BlocksCameraSight && blocker.IsAlive)
                        return true;
                }
            }

            return false;
        }
    }
}