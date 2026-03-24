namespace MyGame2.Stage
{
    // 로봇 적 AI.
    // 웨이포인트를 순서대로 이동하며, 상자/벽에 막히면 역방향으로 전환.
    public sealed class RobotEnemy
    {
        public MoveResult ResolveTurn(StageState state, int robotId, MovementRule movementRule)
        {
            if (!state.TryGetEntity(robotId, out EntityState robot) || !robot.IsAlive)
                return MoveResult.Blocked(robotId, new GridPos(0, 0), new GridPos(0, 0), MoveBlockReason.DeadEntity);

            if (!robot.Patrol.HasWaypoints)
                return MoveResult.Blocked(robotId, robot.Position, robot.Position, MoveBlockReason.InvalidDirection);

            // 현재 웨이포인트에 도착했으면 다음으로 전진
            Direction dir = robot.Patrol.GetDirectionFrom(robot.Position);
            if (dir == Direction.None)
            {
                robot.Patrol.AdvanceToNext();
                dir = robot.Patrol.GetDirectionFrom(robot.Position);
                if (dir == Direction.None)
                    return MoveResult.Blocked(robotId, robot.Position, robot.Position, MoveBlockReason.InvalidDirection);
            }

            // 순방향 이동 시도
            MoveResult result = movementRule.TryMove(state, robotId, dir);

            if (result.Succeeded)
            {
                state.SetFacing(robotId, dir);
                state.MoveEntity(robotId, result.To);

                // 웨이포인트 도착 확인
                if (result.To.Equals(robot.Patrol.CurrentTarget))
                    robot.Patrol.AdvanceToNext();

                return result;
            }

            if (result.IsContactKill)
            {
                state.SetFacing(robotId, dir);
                return result;
            }

            // 막혔으면 역방향 전환 후 재시도
            robot.Patrol.Reverse();
            Direction reverseDir = robot.Patrol.GetDirectionFrom(robot.Position);

            if (reverseDir == Direction.None)
                return result;

            MoveResult reverseResult = movementRule.TryMove(state, robotId, reverseDir);

            if (reverseResult.Succeeded)
            {
                state.SetFacing(robotId, reverseDir);
                state.MoveEntity(robotId, reverseResult.To);

                if (reverseResult.To.Equals(robot.Patrol.CurrentTarget))
                    robot.Patrol.AdvanceToNext();

                return reverseResult;
            }

            if (reverseResult.IsContactKill)
            {
                state.SetFacing(robotId, reverseDir);
                return reverseResult;
            }

            // 양쪽 다 막힘 → 대기
            return result;
        }
    }
}