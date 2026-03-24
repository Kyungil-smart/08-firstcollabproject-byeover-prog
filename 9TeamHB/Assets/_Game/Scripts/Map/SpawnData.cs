using System;

namespace MyGame2.Stage
{
    // 맵 파싱 시 생성되는 엔티티 스폰 정보.
    [Serializable]
    public readonly struct SpawnData
    {
        public readonly EntityKind Kind;
        public readonly GridPos Position;
        public readonly Direction Facing;
        public readonly int PlayerSlot;
        public readonly BoxType BoxOwnership;
        public readonly CameraType DetectionPattern;
        public readonly Direction[] PatrolRoute;
        // true면 카메라가 반시계방향 회전
        public readonly bool ReverseRotation;

        public SpawnData(
            EntityKind kind, GridPos position, Direction facing,
            int playerSlot = 0,
            BoxType boxOwnership = BoxType.Shared,
            CameraType detectionPattern = CameraType.LineShort,
            Direction[] patrolRoute = null,
            bool reverseRotation = false)
        {
            Kind = kind;
            Position = position;
            Facing = facing;
            PlayerSlot = playerSlot;
            BoxOwnership = boxOwnership;
            DetectionPattern = detectionPattern;
            PatrolRoute = patrolRoute ?? Array.Empty<Direction>();
            ReverseRotation = reverseRotation;
        }
    }
}