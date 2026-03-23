using System;

namespace MyGame2.Stage
{
    // [종류별 유효 데이터]
    // Player      → Player (PlayerData)
    // Box         → Box (BoxData)
    ///CameraEnemy → Camera (CameraData)
    // RobotEnemy  → Patrol (PatrolData)
    // AnimalEnemy → (추가 데이터 없음)
   
    [Serializable]
    public sealed class EntityState
    {
        // 공통 필드 (모든 엔티티)
        
        public int Id;
        public EntityKind Kind;
        public GridPos Position;
        public GridPos SpawnPosition;
        public Direction Facing;
        public bool IsAlive;
        public bool IsBlocking;
        public bool BlocksCameraSight;
        
        // 종류별 데이터 (해당 Kind일 때만 유효)
        public PlayerData Player;
        public BoxData Box;
        public CameraData Camera;
        public PatrolData Patrol;
        
        public bool IsPlayer { get { return Kind == EntityKind.Player; } }
        public bool IsBox { get { return Kind == EntityKind.Box; } }
        public bool IsCamera { get { return Kind == EntityKind.CameraEnemy; } }
        public bool IsRobot { get { return Kind == EntityKind.RobotEnemy; } }
        public bool IsAnimal { get { return Kind == EntityKind.AnimalEnemy; } }
        public bool IsMovingEnemy { get { return IsRobot || IsAnimal; } }
        public bool IsLethalMover { get { return IsRobot || IsAnimal; } }
        
        // 팩토리
        
        public static EntityState CreatePlayer(GridPos position, Direction facing, int slot)
        {
            return new EntityState
            {
                Kind = EntityKind.Player,
                Position = position,
                SpawnPosition = position,
                Facing = facing,
                IsAlive = true,
                IsBlocking = true,
                BlocksCameraSight = false,
                Player = new PlayerData(slot)
            };
        }

        public static EntityState CreateBox(GridPos position, BoxType ownership)
        {
            return new EntityState
            {
                Kind = EntityKind.Box,
                Position = position,
                SpawnPosition = position,
                Facing = Direction.None,
                IsAlive = true,
                IsBlocking = true,
                BlocksCameraSight = true,
                Box = new BoxData(ownership)
            };
        }

        public static EntityState CreateCamera(GridPos position, Direction facing, CameraType pattern)
        {
            return new EntityState
            {
                Kind = EntityKind.CameraEnemy,
                Position = position,
                SpawnPosition = position,
                Facing = facing,
                IsAlive = true,
                IsBlocking = true,
                BlocksCameraSight = false,
                Camera = new CameraData(pattern)
            };
        }

        public static EntityState CreateRobot(GridPos position, Direction facing, Direction[] patrolRoute)
        {
            return new EntityState
            {
                Kind = EntityKind.RobotEnemy,
                Position = position,
                SpawnPosition = position,
                Facing = facing,
                IsAlive = true,
                IsBlocking = true,
                BlocksCameraSight = true,
                Patrol = new PatrolData(patrolRoute)
            };
        }

        public static EntityState CreateAnimal(GridPos position, Direction facing)
        {
            return new EntityState
            {
                Kind = EntityKind.AnimalEnemy,
                Position = position,
                SpawnPosition = position,
                Facing = facing,
                IsAlive = true,
                IsBlocking = true,
                BlocksCameraSight = false
            };
        }
        
        private EntityState() { }
    }
}
