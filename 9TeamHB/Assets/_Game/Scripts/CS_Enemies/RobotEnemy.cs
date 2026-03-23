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
        
        // 이동로직
        // 앵커 리스트, 정방향 - 역방향 정보,  
        // 현재 위치에서 다음 앵커를 향해 나아가는 로직
        // 다음 앵커를 바라보고 있지 않다면 - 회전 
        // 다음 앵커를 바라보고 있다면 한칸 이동
        // 이동이 불가능하다면 앵커 순회 방향을 변경하고, 다음 앵커를 변경
        // 앵커에 도달했다면 다음 앵커를 방향에 따라 인덱스에서 찾아 변경
    }
}