using System;

namespace MyGame2.Stage
{
    // RobotEnemy 전용 순찰 데이터.
    // GridPos 웨이포인트 배열을 순서대로 이동한다.
    // 막히면 역방향으로 전환.
    public struct PatrolData
    {
        // 웨이포인트 좌표 배열
        public GridPos[] Waypoints;
        // 현재 목표 웨이포인트 인덱스
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

        // 현재 위치에서 목표를 향한 1칸 이동 방향을 계산한다.
        // 수평 우선. 도달했으면 None 반환.
        public Direction GetDirectionFrom(GridPos current)
        {
            if (!HasWaypoints) return Direction.None;
            GridPos target = CurrentTarget;

            int dx = target.X - current.X;
            int dy = target.Y - current.Y;

            if (dx == 0 && dy == 0) return Direction.None;

            // 수평 우선
            if (dx != 0) return dx > 0 ? Direction.Right : Direction.Left;
            return dy > 0 ? Direction.Down : Direction.Up;
        }

        // 다음 웨이포인트로 전진. 끝에 도달하면 순환.
        public void AdvanceToNext()
        {
            if (!HasWaypoints) return;
            TargetIndex += Step;
            if (TargetIndex >= Waypoints.Length) TargetIndex = 0;
            else if (TargetIndex < 0) TargetIndex = Waypoints.Length - 1;
        }

        // 진행 방향 반전 (막혔을 때)
        public void Reverse()
        {
            Step = -Step;
            AdvanceToNext();
        }

        // ── 하위 호환 (AnimalEnemy 등에서 HasRoute 접근) ──
        public bool HasRoute { get { return HasWaypoints; } }
        public Direction[] Route { get { return Array.Empty<Direction>(); } }
        public int Index { get { return TargetIndex; } set { TargetIndex = value; } }
    }
}