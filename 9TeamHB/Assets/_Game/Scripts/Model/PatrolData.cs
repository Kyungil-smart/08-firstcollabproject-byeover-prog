using System;

namespace MyGame2.Stage
{
    // RobotEnemy 전용 순찰 데이터. Kind == RobotEnemy일 때만 유효.
    public struct PatrolData
    {
        public Direction[] Route;
        public int Index;

        public PatrolData(Direction[] route)
        {
            Route = route ?? Array.Empty<Direction>();
            Index = 0;
        }

        public bool HasRoute { get { return Route != null && Route.Length > 0; } }

        // 현재 방향을 반환하고 인덱스를 전진.
        public Direction Advance()
        {
            if (!HasRoute) return Direction.None;
            Direction dir = Route[Index % Route.Length];
            Index = (Index + 1) % Route.Length;
            return dir;
        }
    }
}