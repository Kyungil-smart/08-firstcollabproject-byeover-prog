using System;

namespace MyGame2.Stage
{
    // RobotEnemy 전용 순찰 데이터.
    // class로 선언 — Dictionary에서 꺼내서 바로 수정 가능.
    public class PatrolData : IComponentData
    {
        public GridPos[] Waypoints;
        public int TargetIndex;
        // 1 = 순방향, -1 = 역방향
        public int Step;

        public PatrolData(GridPos[] waypoints)
        {
            Waypoints = waypoints ?? Array.Empty<GridPos>();
            TargetIndex = waypoints != null && waypoints.Length > 1 ? 1 : 0;
            Step = 1;
        }

        public bool HasWaypoints { get { return Waypoints != null && Waypoints.Length > 1; } }

        public GridPos CurrentTarget
        {
            get
            {
                if (!HasWaypoints) return new GridPos(0, 0);
                return Waypoints[TargetIndex];
            }
        }

        public Direction GetDirectionFrom(GridPos current)
        {
            if (!HasWaypoints) return Direction.None;
            GridPos target = CurrentTarget;

            int dx = target.X - current.X;
            int dy = target.Y - current.Y;

            if (dx == 0 && dy == 0) return Direction.None;
            if (dx != 0) return dx > 0 ? Direction.Right : Direction.Left;
            return dy > 0 ? Direction.Down : Direction.Up;
        }

        public void AdvanceToNext()
        {
            if (!HasWaypoints) return;
            TargetIndex += Step;
            if (TargetIndex >= Waypoints.Length) TargetIndex = 0;
            else if (TargetIndex < 0) TargetIndex = Waypoints.Length - 1;
        }

        public void Reverse()
        {
            Step = -Step;
            AdvanceToNext();
        }

        // 하위 호환
        public bool HasRoute { get { return HasWaypoints; } }
    }
}