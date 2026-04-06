using UnityEngine;
using MyGame2.Stage;

public class PatrolRule
{
    public MoveResult ResolveTurn(StageState state, int Id, MovementRule movementRule)
        {
            if (!state.TryGetEntity(Id, out EntityState entity) || !entity.IsAlive)
                return MoveResult.Blocked(Id, new GridPos(0, 0), new GridPos(0, 0), MoveBlockReason.DeadEntity);

            PatrolData patrol = entity.Get<PatrolData>();
            if (patrol == null || !patrol.HasWaypoints)
                return MoveResult.Blocked(Id, entity.Position, entity.Position, MoveBlockReason.InvalidDirection);

            Direction dir = patrol.GetDirectionFrom(entity.Position);
            if (dir == Direction.None)
            {
                patrol.AdvanceToNext();
                dir = patrol.GetDirectionFrom(entity.Position);
                if (dir == Direction.None)
                    return MoveResult.Blocked(Id, entity.Position, entity.Position, MoveBlockReason.InvalidDirection);
            }

            MoveResult result = movementRule.TryMove(state, Id, dir);

            if (result.Succeeded)
            {
                state.SetFacing(Id, dir);
                state.MoveEntity(Id, result.To);
                if (result.To.Equals(patrol.CurrentTarget))
                    patrol.AdvanceToNext();
                return result;
            }

            if (result.IsContactKill)
            {
                state.SetFacing(Id, dir);
                return result;
            }

            // 이동 실패 시 방향 반전
            patrol.Reverse();
            Direction reverseDir = patrol.GetDirectionFrom(entity.Position);
            if (reverseDir == Direction.None) return result;

            MoveResult rev = movementRule.TryMove(state, Id, reverseDir);
            if (rev.Succeeded)
            {
                state.SetFacing(Id, reverseDir);
                state.MoveEntity(Id, rev.To);
                if (rev.To.Equals(patrol.CurrentTarget))
                    patrol.AdvanceToNext();
                return rev;
            }
            if (rev.IsContactKill)
            {
                state.SetFacing(Id, reverseDir);
                return rev;
            }

            return result;
        }
}
