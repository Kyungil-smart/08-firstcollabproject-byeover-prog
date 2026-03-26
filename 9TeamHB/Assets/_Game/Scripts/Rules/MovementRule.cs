namespace MyGame2.Stage
{
    // 이동 가능 여부를 판정한다. 상태를 변경하지 않는다.
    // 상자가 있는 셀로 이동 시 PushAndMove 결과를 반환하고,
    ///실제 밀기/이동은 TurnSystem이 수행한다.
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

            if (cell.IsOccupied)
            {
                if (state.TryGetEntity(cell.OccupantId, out EntityState occupant) && occupant.IsAlive)
                {
                    // 적이 플레이어 위치로 이동 → ContactKill
                    if (mover.IsLethalMover && occupant.IsPlayer)
                        return MoveResult.ContactKill(moverId, occupant.Id, from, target);

                    // 플레이어가 상자를 밀려는 경우 → 판정만
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
