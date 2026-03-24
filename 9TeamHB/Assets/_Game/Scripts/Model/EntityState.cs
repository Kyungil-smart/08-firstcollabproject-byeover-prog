using System;
using System.Collections.Generic;

namespace MyGame2.Stage
{
    // ECS-lite 엔티티.
    // 공통 필드(Position, Facing 등)는 직접 보유하고,
    // 종류별 데이터는 Dictionary<Type, IComponentData>로 자유롭게 부착/제거한다.
    
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

        // 컴포넌트 저장소

        private readonly Dictionary<Type, IComponentData> _components
            = new Dictionary<Type, IComponentData>(4);

        // 컴포넌트 API

        public bool Has<T>() where T : IComponentData
        {
            return _components.ContainsKey(typeof(T));
        }

        public T Get<T>() where T : IComponentData
        {
            if (_components.TryGetValue(typeof(T), out IComponentData data))
                return (T)data;
            return default;
        }

        public void Set<T>(T data) where T : IComponentData
        {
            _components[typeof(T)] = data;
        }

        public bool Remove<T>() where T : IComponentData
        {
            return _components.Remove(typeof(T));
        }

        // 편의 프로퍼티

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