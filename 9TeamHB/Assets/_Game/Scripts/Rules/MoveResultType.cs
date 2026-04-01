namespace MyGame2.Stage
{
    public enum MoveResultType
    {
        None = 0,
        Success = 1,
        Blocked = 2,
        ContactKill = 3,
        PushAndMove = 4,
        OpenDoor = 5
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

        public bool Succeeded { get { return Type == MoveResultType.Success; } }
        public bool IsContactKill { get { return Type == MoveResultType.ContactKill; } }
        public bool IsPushAndMove { get { return Type == MoveResultType.PushAndMove; } }
        public bool IsOpenDoor { get { return Type == MoveResultType.OpenDoor; } }
        // 이동 가능한가?
        public bool CanMove { get { return Succeeded || IsPushAndMove || IsOpenDoor; } }

        private MoveResult(MoveResultType type, MoveBlockReason reason,
            int moverId, int targetId, GridPos from, GridPos to)
        {
            Type = type; BlockReason = reason; MoverId = moverId;
            TargetEntityId = targetId; From = from; To = to;
        }

        public static MoveResult Success(int moverId, GridPos from, GridPos to)
        {
            return new MoveResult(MoveResultType.Success, MoveBlockReason.None,
                moverId, StageState.InvalidEntityId, from, to);
        }

        public static MoveResult Blocked(int moverId, GridPos from, GridPos to, MoveBlockReason reason)
        {
            return new MoveResult(MoveResultType.Blocked, reason,
                moverId, StageState.InvalidEntityId, from, to);
        }

        public static MoveResult ContactKill(int moverId, int targetId, GridPos from, GridPos to)
        {
            return new MoveResult(MoveResultType.ContactKill, MoveBlockReason.None,
                moverId, targetId, from, to);
        }

        // 상자 밀기 후 이동 가능. targetBoxId = 밀릴 상자 ID.
        public static MoveResult PushAndMove(int moverId, int targetBoxId, GridPos from, GridPos to)
        {
            return new MoveResult(MoveResultType.PushAndMove, MoveBlockReason.None,
                moverId, targetBoxId, from, to);
        }

        public static MoveResult OpenDoor(int moverId, GridPos from, GridPos to)
        {
            return new MoveResult(MoveResultType.OpenDoor, MoveBlockReason.None,
                moverId, StageState.InvalidEntityId, from, to);
        }
    }
}
