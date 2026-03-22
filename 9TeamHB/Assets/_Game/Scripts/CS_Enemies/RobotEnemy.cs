namespace MyGame2.Stage
{
    public sealed class RobotEnemy
    {
        public MoveResult ResolveTurn(StageState state, int robotId, MovementRule movementRule)
        {
            if (!state.TryGetEntity(robotId, out EntityState robot) || !robot.IsAlive)
            {
                return MoveResult.Blocked(robotId, new GridPos(0, 0), new GridPos(0, 0), MoveBlockReason.DeadEntity);
            }

            Direction direction = GetDesiredDirection(robot);
            if (direction == Direction.None)
            {
                return MoveResult.Blocked(robotId, robot.Position, robot.Position, MoveBlockReason.InvalidDirection);
            }

            MoveResult result = movementRule.TryMove(state, robotId, direction);
            state.SetFacing(robotId, direction);

            if (result.Succeeded)
            {
                state.MoveEntity(robotId, result.To);
            }

            if (robot.PatrolRoute != null && robot.PatrolRoute.Length > 0)
            {
                state.AdvancePatrolIndex(robotId);
            }
            else if (result.BlockReason == MoveBlockReason.OutOfBounds ||
                     result.BlockReason == MoveBlockReason.BlockedByWall ||
                     result.BlockReason == MoveBlockReason.BlockedByEntity)
            {
                state.SetFacing(robotId, direction.RotateClockwise());
            }

            return result;
        }

        private Direction GetDesiredDirection(EntityState robot)
        {
            if (robot.PatrolRoute != null && robot.PatrolRoute.Length > 0)
            {
                int index = robot.PatrolIndex % robot.PatrolRoute.Length;
                return robot.PatrolRoute[index];
            }

            return robot.Facing == Direction.None ? Direction.Right : robot.Facing;
        }
    }
}