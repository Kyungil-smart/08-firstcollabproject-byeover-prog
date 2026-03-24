using System;
using System.Collections.Generic;

namespace MyGame2.Stage
{
    // ECS-lite 엔티티.
    // 공통 필드(Position, Facing 등)는 직접 보유하고,
    // 종류별 데이터는 Dictionary<Type, IComponentData>로 자유롭게 부착/제거한다.
    //
    // [사용법]
    // entity.Has<PlayerData>()           → 컴포넌트 존재 여부
    // entity.Get<PlayerData>()           → 컴포넌트 조회 (없으면 default)
    // entity.Set(new PlayerData(1))      → 컴포넌트 추가/교체
    // entity.Remove<PlayerData>()        → 컴포넌트 제거
    //
    // [새 엔티티 기믹 추가]
    // 1. IComponentData 구현하는 struct/class 만들기
    // 2. 팩토리에 Set() 호출 추가
    // 3. EntityState 자체는 수정 불필요
    //
    [Serializable]
    public sealed class EntityState
    {
        // ── 공통 필드 (모든 엔티티) ──

        public int Id;
        public EntityKind Kind;
        public GridPos Position;
        public GridPos SpawnPosition;
        public Direction Facing;
        public bool IsAlive;
        public bool IsBlocking;
        public bool BlocksCameraSight;

        public bool IsPlayer { get { return Kind == EntityKind.Player; } }
        public bool IsBox { get { return Kind == EntityKind.Box; } }
        public bool IsCamera { get { return Kind == EntityKind.CameraEnemy; } }
        public bool IsRobot { get { return Kind == EntityKind.RobotEnemy; } }
        public bool IsAnimal { get { return Kind == EntityKind.AnimalEnemy; } }
        public bool IsMovingEnemy { get { return IsRobot || IsAnimal; } }
        public bool IsLethalMover { get { return IsRobot || IsAnimal; } }

        // ── 팩토리 ──

        public static EntityState CreatePlayer(GridPos position, Direction facing, int slot)
        {
            var e = new EntityState
            {
                Kind = EntityKind.Player,
                Position = position, SpawnPosition = position,
                Facing = facing, IsAlive = true,
                IsBlocking = true, BlocksCameraSight = false
            };
            e.Set(new PlayerData(slot));
            return e;
        }

        public static EntityState CreateBox(GridPos position, BoxType ownership)
        {
            var e = new EntityState
            {
                Kind = EntityKind.Box,
                Position = position, SpawnPosition = position,
                Facing = Direction.None, IsAlive = true,
                IsBlocking = true, BlocksCameraSight = true
            };
            e.Set(new BoxData(ownership));
            return e;
        }

        public static EntityState CreateCamera(GridPos position, Direction facing,
            CameraType pattern, bool reverseRotation = false)
        {
            var e = new EntityState
            {
                Kind = EntityKind.CameraEnemy,
                Position = position, SpawnPosition = position,
                Facing = facing, IsAlive = true,
                IsBlocking = true, BlocksCameraSight = false
            };
            e.Set(new CameraData(pattern, reverseRotation));
            return e;
        }

        public static EntityState CreateRobot(GridPos position, Direction facing,
            GridPos[] waypoints = null)
        {
            var e = new EntityState
            {
                Kind = EntityKind.RobotEnemy,
                Position = position, SpawnPosition = position,
                Facing = facing, IsAlive = true,
                IsBlocking = true, BlocksCameraSight = true
            };
            e.Set(new PatrolData(waypoints));
            return e;
        }

        public static EntityState CreateAnimal(GridPos position, Direction facing)
        {
            return new EntityState
            {
                Kind = EntityKind.AnimalEnemy,
                Position = position, SpawnPosition = position,
                Facing = facing, IsAlive = true,
                IsBlocking = true, BlocksCameraSight = false
            };
        }

        private EntityState() { }
    }

    
}