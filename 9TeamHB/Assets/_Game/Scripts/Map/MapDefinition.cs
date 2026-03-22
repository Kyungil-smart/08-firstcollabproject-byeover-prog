using System;
using System.Collections.Generic;

namespace MyGame2.Stage
{
    [Serializable]
    public sealed class MapDefinition
    {
        private readonly CellFlags[] _cellFlags;
        private readonly List<SpawnData> _spawns;

        public int Width { get; private set; }
        public int Height { get; private set; }
        public IReadOnlyList<SpawnData> Spawns { get { return _spawns; } }

        public MapDefinition(int width, int height, CellFlags[] cellFlags, List<SpawnData> spawns)
        {
            Width = width;
            Height = height;
            _cellFlags = cellFlags ?? throw new ArgumentNullException(nameof(cellFlags));
            _spawns = spawns ?? throw new ArgumentNullException(nameof(spawns));
        }

        public CellFlags GetCellFlags(GridPos position)
        {
            return _cellFlags[(position.Y * Width) + position.X];
        }

        public CellFlags[] CloneCellFlags()
        {
            return (CellFlags[])_cellFlags.Clone();
        }
    }
}