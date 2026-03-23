using System;

namespace MyGame2.Stage
{
    // [종류별 사용 필드]
    // - Player: PlayerSlot, IsAlive
    // - Box: BoxOwnership, IsAlive(=상자 존재 여부)
    // - CameraEnemy: DetectionPattern, Facing(=감시 방향), IsAlive
    // - RobotEnemy: PatrolRoute, PatrolIndex, Facing (다른 스테이지용)
    // - AnimalEnemy: Facing (다른 스테이지용)
    [Serializable]
    public sealed class EntityState
    {
        public int Id;
        public EntityKind Kind;
        public GridPos Position;
        public GridPos SpawnPosition;
        public Direction Facing;
        public bool IsAlive;

        // 이 엔티티가 셀을 점유하는가? (이동 차단)
        public bool IsBlocking;

        // 이 엔티티가 카메라 시야를 차단하는가?
        public bool BlocksCameraSight;

        // 플레이어 슬롯 번호 (1 또는 2). Player가 아니면 0.
        public int PlayerSlot;

        // 상자 소유권. Box가 아니면 무시.
        public BoxType BoxOwnership;

        // 카메라 감지 패턴 타입. CameraEnemy가 아니면 무시.
        public CameraType DetectionPattern;

        // 로봇 순찰 경로 (다른 스테이지용). RobotEnemy가 아니면 빈 배열.
        public Direction[] PatrolRoute;

        // 로봇 순찰 경로의 현재 인덱스. RobotEnemy가 아니면 0.
        public int PatrolIndex;

        public EntityState(int id, EntityKind kind, GridPos position, Direction facing, int playerSlot)
        {
            Id = id;
            Kind = kind;
            Position = position;
            SpawnPosition = position;
            Facing = facing;
            PlayerSlot = playerSlot;
            IsAlive = true;
            IsBlocking = true;
            BlocksCameraSight = false;
            BoxOwnership = BoxType.Shared;
            DetectionPattern = CameraType.LineShort;
            PatrolRoute = Array.Empty<Direction>();
            PatrolIndex = 0;
        }

        // 플레이어인가?
        public bool IsPlayer
        {
            get { return Kind == EntityKind.Player; }
        }

        // 상자인가?
        public bool IsBox
        {
            get { return Kind == EntityKind.Box; }
        }

        // 카메라인가?
        public bool IsCamera
        {
            get { return Kind == EntityKind.CameraEnemy; }
        }

        // 이동하는 적인가? (로봇, 동물 — 다른 스테이지용)
        public bool IsMovingEnemy
        {
            get { return Kind == EntityKind.RobotEnemy || Kind == EntityKind.AnimalEnemy; }
        }

        // 접촉 시 플레이어를 죽이는 엔티티인가? (다른 스테이지용)
        public bool IsLethalMover
        {
            get { return Kind == EntityKind.RobotEnemy || Kind == EntityKind.AnimalEnemy; }
        }

        // 이 상자를 해당 플레이어가 밀 수 있는가?
        // Box가 아니면 항상 false.
        public bool CanBePushedBy(int playerSlot)
        {
            if (Kind != EntityKind.Box)
            {
                return false;
            }

            switch (BoxOwnership)
            {
                case BoxType.Shared:
                    return true;
                case BoxType.Player1Only:
                    return playerSlot == 1;
                case BoxType.Player2Only:
                    return playerSlot == 2;
                default:
                    return false;
            }
        }
    }
}