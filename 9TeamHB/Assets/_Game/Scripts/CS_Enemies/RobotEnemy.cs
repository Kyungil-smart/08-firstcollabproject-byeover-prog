using System.Collections.Generic;

namespace MyGame2.Stage
{
    // 로봇 적 AI.
    // 웨이포인트를 순서대로 이동하며, 상자/벽에 막히면 역방향 전환.
    // [감지 범위] Facing 앞 2칸 + 뒤 2칸 (경고용, 즉사 아님)
    // [즉사 조건] 플레이어와 같은 셀에 접촉했을 때만
    public sealed class RobotEnemy
    {
        // 순찰 이동 (웨이포인트 기반)

        public MoveResult ResolveTurn(StageState state, int robotId, MovementRule movementRule)
        {
            if (!state.TryGetEntity(robotId, out EntityState robot) || !robot.IsAlive)
                return MoveResult.Blocked(robotId, new GridPos(0, 0), new GridPos(0, 0), MoveBlockReason.DeadEntity);

            if (!robot.Patrol.HasWaypoints)
                return MoveResult.Blocked(robotId, robot.Position, robot.Position, MoveBlockReason.InvalidDirection);

            Direction dir = robot.Patrol.GetDirectionFrom(robot.Position);
            if (dir == Direction.None)
            {
                robot.Patrol.AdvanceToNext();
                dir = robot.Patrol.GetDirectionFrom(robot.Position);
                if (dir == Direction.None)
                    return MoveResult.Blocked(robotId, robot.Position, robot.Position, MoveBlockReason.InvalidDirection);
            }

            MoveResult result = movementRule.TryMove(state, robotId, dir);

            if (result.Succeeded)
            {
                state.SetFacing(robotId, dir);
                state.MoveEntity(robotId, result.To);
                if (result.To.Equals(robot.Patrol.CurrentTarget))
                    robot.Patrol.AdvanceToNext();
                return result;
            }

            if (result.IsContactKill)
            {
                state.SetFacing(robotId, dir);
                return result;
            }

            // 막혔으면 역방향 전환
            robot.Patrol.Reverse();
            Direction reverseDir = robot.Patrol.GetDirectionFrom(robot.Position);
            if (reverseDir == Direction.None) return result;

            MoveResult rev = movementRule.TryMove(state, robotId, reverseDir);
            if (rev.Succeeded)
            {
                state.SetFacing(robotId, reverseDir);
                state.MoveEntity(robotId, rev.To);
                if (rev.To.Equals(robot.Patrol.CurrentTarget))
                    robot.Patrol.AdvanceToNext();
                return rev;
            }
            if (rev.IsContactKill)
            {
                state.SetFacing(robotId, reverseDir);
                return rev;
            }

            return result;
        }
        
        // 앞뒤 2칸 감지. detectedFromBehind로 뒤에서 감지되었는지 알려준다.
        public bool TryDetect(StageState state, int robotId,
            out int detectedPlayerId, out bool detectedFromBehind)
        {
            detectedPlayerId = StageState.InvalidEntityId;
            detectedFromBehind = false;

            if (!state.TryGetEntity(robotId, out EntityState robot) || !robot.IsAlive)
                return false;

            if (robot.Facing == Direction.None)
                return false;

            // 앞 2칸 먼저
            if (ScanLine(state, robot.Position, robot.Facing, 2, out detectedPlayerId))
            {
                detectedFromBehind = false;
                return true;
            }

            // 뒤 2칸
            if (ScanLine(state, robot.Position, robot.Facing.Opposite(), 2, out detectedPlayerId))
            {
                detectedFromBehind = true;
                return true;
            }

            return false;
        }

        private bool ScanLine(StageState state, GridPos origin, Direction dir,
            int range, out int detectedPlayerId)
        {
            detectedPlayerId = StageState.InvalidEntityId;
            GridPos cursor = origin;

            for (int i = 0; i < range; i++)
            {
                cursor = cursor.Move(dir);
                if (!state.IsInside(cursor)) break;

                CellData cell = state.GetCell(cursor);
                if (cell.HasWall) break;

                if (cell.IsOccupied && state.TryGetEntity(cell.OccupantId, out EntityState occ))
                {
                    if (occ.IsPlayer && occ.IsAlive)
                    {
                        detectedPlayerId = occ.Id;
                        return true;
                    }
                    if (occ.BlocksCameraSight && occ.IsAlive) break;
                }
            }

            return false;
        }

        // 감지 범위 셀 목록 (시각화용). 앞 2칸 + 뒤 2칸.
        public List<GridPos> CollectDetectionCells(StageState state, int robotId)
        {
            List<GridPos> result = new List<GridPos>(4);

            if (!state.TryGetEntity(robotId, out EntityState robot) || !robot.IsAlive)
                return result;

            if (robot.Facing == Direction.None)
                return result;

            CollectLineCells(state, robot.Position, robot.Facing, 2, result);
            CollectLineCells(state, robot.Position, robot.Facing.Opposite(), 2, result);

            return result;
        }

        private void CollectLineCells(StageState state, GridPos origin, Direction dir,
            int range, List<GridPos> result)
        {
            GridPos cursor = origin;
            for (int i = 0; i < range; i++)
            {
                cursor = cursor.Move(dir);
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
    }
}