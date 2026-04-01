using System;
using System.Collections.Generic;
using System.Linq;

namespace MyGame2.Stage
{
    public sealed class EntitySnapshot
    {
        public int Id;
        public EntityKind Kind;
        public GridPos Position;
        public GridPos SpawnPosition;
        public Direction Facing;
        public bool IsAlive;
        public bool IsBlocking;
        public bool BlocksCameraSight;

        private readonly Dictionary<Type, IComponentData> _components
            = new Dictionary<Type, IComponentData>(4);

        public EntitySnapshot(EntityState state)
        {
            Id = state.Id;
            Kind = state.Kind;
            Position = state.Position;
            SpawnPosition = state.SpawnPosition;
            Facing = state.Facing;
            IsAlive = state.IsAlive;
            IsBlocking = state.IsBlocking;
            BlocksCameraSight = state.BlocksCameraSight;

            _components = new Dictionary<Type, IComponentData>(state.Components.Count());

            foreach (KeyValuePair<Type, IComponentData> elem in state.ComponentDict)
            {
                _components[elem.Key] = elem.Value;
            }
        }
    }
}