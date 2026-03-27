using UnityEngine;

namespace MyGame2.Stage
{
    public sealed class MovementRule
    {
        private readonly PushRule _pushRule;

        public MovementRule(PushRule pushRule)
        {
            _pushRule = pushRule;
        }

        public MoveResult TryMove(StageState state, int moverId, Direction direction)
        {
            if (direction == Direction.None)
                return MoveResult.Blocked(moverId, new GridPos(0, 0), new GridPos(0, 0),
                    MoveBlockReason.InvalidDirection);

            if (!state.TryGetEntity(moverId, out EntityState mover))
                return MoveResult.Blocked(moverId, new GridPos(0, 0), new GridPos(0, 0),
                    MoveBlockReason.DeadEntity);

            GridPos from = mover.Position;
            if (!mover.IsAlive)
                return MoveResult.Blocked(moverId, from, from, MoveBlockReason.DeadEntity);

            GridPos target = from.Move(direction);
            if (!state.IsInside(target))
                return MoveResult.Blocked(moverId, from, target, MoveBlockReason.OutOfBounds);

            CellData cell = state.GetCell(target);
            if (cell.HasWall)
                return MoveResult.Blocked(moverId, from, target, MoveBlockReason.BlockedByWall);

            // 부쉬: 감시자/적은 진입 불가, 플레이어만 가능
            if (cell.HasBush && !mover.IsPlayer)
                return MoveResult.Blocked(moverId, from, target, MoveBlockReason.BlockedByWall);

            if (cell.IsOccupied)
            {
                if (state.TryGetEntity(cell.OccupantId, out EntityState occupant) && occupant.IsAlive)
                {
                    if (mover.IsLethalMover && occupant.IsPlayer)
                        return MoveResult.ContactKill(moverId, occupant.Id, from, target);

                    if (mover.IsPlayer && occupant.IsPushable)
                    {
                        if (_pushRule.CanPush(state, moverId, occupant.Id, direction))
                            return MoveResult.PushAndMove(moverId, occupant.Id, from, target);
                    }
                }
                return MoveResult.Blocked(moverId, from, target, MoveBlockReason.BlockedByEntity);
            }

            return MoveResult.Success(moverId, from, target);
        }
    }
}