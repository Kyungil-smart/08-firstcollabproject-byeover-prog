namespace MyGame2.Stage
{
    public enum MoveResultType
    {
        None = 0,
        Success = 1,
        Blocked = 2,
        ContactKill = 3
    }

    public enum MoveBlockReason
    {
        None = 0,
        InvalidDirection = 1,
        DeadEntity = 2,
        OutOfBounds = 3,
        BlockedByWall = 4,
        BlockedByEntity = 5
    }

    public readonly struct MoveResult
    {
        public readonly MoveResultType Type;
        public readonly MoveBlockReason BlockReason;
        public readonly int MoverId;
        public readonly int TargetEntityId;
        public readonly GridPos From;
        public readonly GridPos To;

        public bool Succeeded
        {
            get { return Type == MoveResultType.Success; }
        }

        public bool IsContactKill
        {
            get { return Type == MoveResultType.ContactKill; }
        }

        private MoveResult(
            MoveResultType type,
            MoveBlockReason blockReason,
            int moverId,
            int targetEntityId,
            GridPos from,
            GridPos to)
        {
            Type = type;
            BlockReason = blockReason;
            MoverId = moverId;
            TargetEntityId = targetEntityId;
            From = from;
            To = to;
        }

        public static MoveResult Success(int moverId, GridPos from, GridPos to)
        {
            return new MoveResult(MoveResultType.Success, MoveBlockReason.None, moverId, StageState.InvalidEntityId, from, to);
        }

        public static MoveResult Blocked(int moverId, GridPos from, GridPos to, MoveBlockReason reason)
        {
            return new MoveResult(MoveResultType.Blocked, reason, moverId, StageState.InvalidEntityId, from, to);
        }

        public static MoveResult ContactKill(int moverId, int targetEntityId, GridPos from, GridPos to)
        {
            return new MoveResult(MoveResultType.ContactKill, MoveBlockReason.None, moverId, targetEntityId, from, to);
        }
    }
}
