using System.Collections.Generic;

namespace MyGame2.Stage
{
    public sealed class StageSnapshot
    {
        private CellData[] _cells;

        private readonly Dictionary<int, EntityState> _entitiesById;
        private readonly List<int> _patrolCameraIds;
        private readonly List<int> _summonerIds;
        private readonly List<int> _chaserIds;

        public CellData[] Cells { get { return _cells; } }
        public int ActivePlayerId { get; private set; }
        public int TurnIndex { get; private set; }
        public bool IsGameOver { get; private set; }
        public bool IsStageClear { get; private set; }
        public bool IsViewDirty { get; private set; }


        public Dictionary<int, EntityState> EntityDict { get { return _entitiesById; } }
        public IReadOnlyList<int> PatrolCameraIds { get { return _patrolCameraIds; } }
        public IReadOnlyList<int> SummonerIds { get { return _summonerIds; } }
        public IReadOnlyList<int> ChaserIds { get { return _chaserIds; } }


        public StageSnapshot(StageState state)
        {
            _cells = (CellData[])state.Cells.Clone();

            ActivePlayerId = state.ActivePlayerId;
            TurnIndex = state.TurnIndex;
            IsGameOver = state.IsGameOver;
            IsStageClear = state.IsStageClear;
            IsViewDirty = state.IsViewDirty;

            _patrolCameraIds = new List<int>(state.PatrolCameraIds);
            _summonerIds = new List<int>(state.SummonerIds);
            _chaserIds = new List<int>(state.ChaserIds);

            _entitiesById = new Dictionary<int, EntityState>(state.EntityDict.Count);

            foreach (KeyValuePair<int, EntityState> elem in state.EntityDict)
            {
                _entitiesById[elem.Key] = elem.Value.CopyFrom();
            }
        }
    }
}