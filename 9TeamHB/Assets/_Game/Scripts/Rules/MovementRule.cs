namespace MyGame2.Stage
{
    // 이동 가능 여부를 판정하는 규칙.
    // 상자가 있는 셀로 이동 시 PushRule을 통해 밀기를 시도한다.
    public sealed class MovementRule
    {
        private readonly PushRule _pushRule;

        public MovementRule()
        {
            _pushRule = new PushRule();
        }

        public MovementRule(PushRule pushRule)
        {
            _pushRule = pushRule;
        }

        public MoveResult TryMove(StageState state, int moverId, Direction direction)
        {
            if (direction == Direction.None)
            {
                return MoveResult.Blocked(moverId,
                    new GridPos(0, 0), new GridPos(0, 0),
                    MoveBlockReason.InvalidDirection);
            }

            if (!state.TryGetEntity(moverId, out EntityState mover))
            {
                return MoveResult.Blocked(moverId,
                    new GridPos(0, 0), new GridPos(0, 0),
                    MoveBlockReason.DeadEntity);
            }

            GridPos from = mover.Position;

            if (!mover.IsAlive)
            {
                return MoveResult.Blocked(moverId, from, from,
                    MoveBlockReason.DeadEntity);
            }

            GridPos target = from.Move(direction);

            if (!state.IsInside(target))
            {
                return MoveResult.Blocked(moverId, from, target,
                    MoveBlockReason.OutOfBounds);
            }

            CellData cell = state.GetCell(target);

            if (cell.HasWall)
            {
                return MoveResult.Blocked(moverId, from, target,
                    MoveBlockReason.BlockedByWall);
            }

            if (cell.IsOccupied)
            {
                // 플레이어가 상자를 밀려는 경우
                if (mover.IsPlayer &&
                    state.TryGetEntity(cell.OccupantId, out EntityState occupant) &&
                    occupant.IsBox && occupant.IsAlive)
                {
                    bool pushed = _pushRule.TryPush(state, moverId, occupant.Id, direction);
                    if (pushed)
                    {
                        // 상자가 밀렸으므로 해당 셀이 비었음 → 이동 성공
                        return MoveResult.Success(moverId, from, target);
                    }
                }

                return MoveResult.Blocked(moverId, from, target,
                    MoveBlockReason.BlockedByEntity);
            }

            return MoveResult.Success(moverId, from, target);
        }
    }
}