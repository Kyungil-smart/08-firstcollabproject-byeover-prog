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

        private List<KeyFollower> _keys = null;
        public List<KeyFollower> Keys { get { return _keys; } }

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

            if (state.Has<PocketData>() == true)
            {
                PocketData pocketData = state.Get<PocketData>();
                _keys = new List<KeyFollower>();
                _keys.AddRange(pocketData.Keys);
            }

            _components = new Dictionary<Type, IComponentData>(state.Components.Count());

            foreach (KeyValuePair<Type, IComponentData> elem in state.ComponentDict)
            {
                _components[elem.Key] = elem.Value;
            }
        }
    }
}