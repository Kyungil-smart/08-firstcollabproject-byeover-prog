using MyGame2.Stage;
using UnityEngine;
 
[CreateAssetMenu(fileName = "RobotEnemyMove_Fn", menuName = "Scriptable Objects/EntityFunction/RobotEnemyMove_Fn")]
public class RobotEnemyMove_Fn : EntityFunctionSO
{
    public MoveResult ResolveTurn(StageState state, int robotId, MovementRule movementRule)
    {
        if (!state.TryGetEntity(robotId, out EntityState robot) || !robot.IsAlive)
            return MoveResult.Blocked(robotId, new GridPos(0, 0), new GridPos(0, 0), MoveBlockReason.DeadEntity);

        PatrolData patrol = robot.Get<PatrolData>();
        if (patrol == null || !patrol.HasWaypoints)
            return MoveResult.Blocked(robotId, robot.Position, robot.Position, MoveBlockReason.InvalidDirection);

        // 현재 웨이포인트에 도착했으면 다음으로 전진
        Direction dir = patrol.GetDirectionFrom(robot.Position);
        if (dir == Direction.None)
        {
            patrol.AdvanceToNext();
            dir = patrol.GetDirectionFrom(robot.Position);
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
            if (result.To.Equals(patrol.CurrentTarget))
                patrol.AdvanceToNext();

            return result;
        }

        if (result.IsContactKill)
        {
            state.SetFacing(robotId, dir);
            return result;
        }

        // 막혔으면 역방향 전환 후 재시도
        patrol.Reverse();
        Direction reverseDir = patrol.GetDirectionFrom(robot.Position);

        if (reverseDir == Direction.None)
            return result;

        MoveResult reverseResult = movementRule.TryMove(state, robotId, reverseDir);

        if (reverseResult.Succeeded)
        {
            state.SetFacing(robotId, reverseDir);
            state.MoveEntity(robotId, reverseResult.To);

            if (reverseResult.To.Equals(patrol.CurrentTarget))
                patrol.AdvanceToNext();

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