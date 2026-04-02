using System.Collections.Generic;

namespace MyGame2.Stage
{
    public sealed class StageSnapshot
    {
        private CellData[] _cells;

        private readonly Dictionary<int, EntityState> _entitiesById;
        private readonly List<int> _boxIds;
        private readonly List<int> _patrolCameraIds;
        private readonly List<int> _summonerIds;
        private readonly List<int> _chaserIds;

        private readonly List<int> _launcherIds;
        private readonly List<int> _projectileIds;
        private readonly List<int> _sawTrapIds;

        public CellData[] Cells { get { return _cells; } }
        public int ActivePlayerId { get; private set; }
        public int TurnIndex { get; private set; }
        public bool IsGameOver { get; private set; }
        public bool IsStageClear { get; private set; }
        public bool IsViewDirty { get; private set; }


        public Dictionary<int, EntityState> EntityDict { get { return _entitiesById; } }
        public IReadOnlyList<int> BoxIds { get { return _boxIds; } }
        public IReadOnlyList<int> PatrolCameraIds { get { return _patrolCameraIds; } }
        public IReadOnlyList<int> SummonerIds { get { return _summonerIds; } }
        public IReadOnlyList<int> ChaserIds { get { return _chaserIds; } }

        public IReadOnlyList<int> LauncherIds { get { return _launcherIds; } }
        public IReadOnlyList<int> ProjectileIds { get { return _projectileIds; } }
        public IReadOnlyList<int> SawTrapIds { get { return _sawTrapIds; } }

        public StageSnapshot(StageState state)
        {
            _cells = (CellData[])state.Cells.Clone();

            ActivePlayerId = state.ActivePlayerId;
            TurnIndex = state.TurnIndex;
            IsGameOver = state.IsGameOver;
            IsStageClear = state.IsStageClear;
            IsViewDirty = state.IsViewDirty;

            _boxIds = new List<int>(state.BoxIds);
            _patrolCameraIds = new List<int>(state.PatrolCameraIds);
            _summonerIds = new List<int>(state.SummonerIds);
            _chaserIds = new List<int>(state.ChaserIds);

            _launcherIds = new List<int>(state.LauncherIds);
            _projectileIds = new List<int>(state.ProjectileIds);
            _sawTrapIds = new List<int>(state.SawTrapIds);

            _entitiesById = new Dictionary<int, EntityState>(state.EntityDict.Count);

            foreach (KeyValuePair<int, EntityState> elem in state.EntityDict)
            {
                _entitiesById[elem.Key] = elem.Value.CopyFrom();
            }
        }
    }
}