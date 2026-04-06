using System.Collections.Generic;

namespace MyGame2.Stage
{
    public sealed class RobotEnemy
    {
        public MoveResult ResolveTurn(StageState state, int robotId, MovementRule movementRule)
        {
            if (!state.TryGetEntity(robotId, out EntityState robot) || !robot.IsAlive)
                return MoveResult.Blocked(robotId, new GridPos(0, 0), new GridPos(0, 0), MoveBlockReason.DeadEntity);

            PatrolData patrol = robot.Get<PatrolData>();
            if (patrol == null || !patrol.HasWaypoints)
                return MoveResult.Blocked(robotId, robot.Position, robot.Position, MoveBlockReason.InvalidDirection);

            Direction dir = patrol.GetDirectionFrom(robot.Position);
            if (dir == Direction.None)
            {
                patrol.AdvanceToNext();
                dir = patrol.GetDirectionFrom(robot.Position);
                if (dir == Direction.None)
                    return MoveResult.Blocked(robotId, robot.Position, robot.Position, MoveBlockReason.InvalidDirection);
            }

            MoveResult result = movementRule.TryMove(state, robotId, dir);

            if (result.Succeeded)
            {
                state.SetFacing(robotId, dir);
                state.MoveEntity(robotId, result.To);
                if (result.To.Equals(patrol.CurrentTarget))
                    patrol.AdvanceToNext();
                return result;
            }
            CellData nextCell = state.GetCell(result.To);
            if (nextCell.IsOccupied &&
                state.TryGetEntity(nextCell.OccupantId, out EntityState occ) &&
                occ.IsPlayer && occ.IsAlive)
            {
                state.SetFacing(robotId, dir);
                state.KillEntity(occ.Id);
                state.MarkGameOver();
                state.SetViewDirty();
                return result;
            }
            

            if (result.IsContactKill)
            {
                state.SetFacing(robotId, dir);
                return result;
            }

            patrol.Reverse();
            Direction reverseDir = patrol.GetDirectionFrom(robot.Position);
            if (reverseDir == Direction.None) return result;

            MoveResult rev = movementRule.TryMove(state, robotId, reverseDir);
            if (rev.Succeeded)
            {
                state.SetFacing(robotId, reverseDir);
                state.MoveEntity(robotId, rev.To);
                if (rev.To.Equals(patrol.CurrentTarget))
                    patrol.AdvanceToNext();
                return rev;
            }
            if (rev.IsContactKill)
            {
                state.SetFacing(robotId, reverseDir);
                return rev;
            }

            return result;
        }

        public bool TryDetect(StageState state, int robotId,
            out int detectedPlayerId, out bool detectedFromBehind)
        {
            detectedPlayerId = StageState.InvalidEntityId;
            detectedFromBehind = false;

            if (!state.TryGetEntity(robotId, out EntityState robot) || !robot.IsAlive)
                return false;

            if (robot.Facing == Direction.None)
                return false;

            if (ScanLine(state, robot.Position, robot.Facing, 2, out detectedPlayerId))
            {
                detectedFromBehind = false;
                return true;
            }

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

                // 부쉬는 시야를 차단하고, 안의 플레이어를 감지 불가
                if (cell.HasBush) break;

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

        public List<GridPos> CollectDetectionCells(StageState state, int robotId)
        {
            List<GridPos> result = new List<GridPos>(4);
            if (!state.TryGetEntity(robotId, out EntityState robot) || !robot.IsAlive) return result;
            if (robot.Facing == Direction.None) return result;

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

                // 부쉬 시야 차단
                if (cell.HasBush) break;

                result.Add(cursor);
                if (cell.IsOccupied &&
                    state.TryGetEntity(cell.OccupantId, out EntityState occ) &&
                    occ.BlocksCameraSight && occ.IsAlive) break;
            }
        }
    }
}