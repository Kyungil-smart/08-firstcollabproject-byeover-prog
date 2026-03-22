using System;
using System.Collections.Generic;

namespace MyGame2.Stage
{
    // 스테이지의 유일한 상태 소유자.
    // 모든 변이 메서드는 StageEvents를 통해 이벤트를 발행한다.

    public sealed class StageState
    {
        public const int InvalidEntityId = -1;

        private readonly CellData[] _cells;
        private readonly Dictionary<int, EntityState> _entitiesById;
        private readonly List<int> _playerIds;
        private readonly List<int> _boxIds;
        private readonly List<int> _cameraIds;
        private readonly List<int> _robotIds;
        private readonly List<int> _animalIds;

        private int _nextEntityId;

        private readonly StageEvents _events;

        public int Width { get; private set; }
        public int Height { get; private set; }
        public int ActivePlayerId { get; private set; }
        public int TurnIndex { get; private set; }
        public bool IsGameOver { get; private set; }
        public bool IsStageClear { get; private set; }

        public IReadOnlyList<int> PlayerIds { get { return _playerIds; } }
        public IReadOnlyList<int> BoxIds { get { return _boxIds; } }
        public IReadOnlyList<int> CameraIds { get { return _cameraIds; } }
        public IReadOnlyList<int> RobotIds { get { return _robotIds; } }
        public IReadOnlyList<int> AnimalIds { get { return _animalIds; } }
        public IEnumerable<EntityState> Entities { get { return _entitiesById.Values; } }
        public StageEvents Events { get { return _events; } }

        // 생성자

        private StageState(int width, int height, CellData[] cells, StageEvents events)
        {
            Width = width;
            Height = height;
            _cells = cells;
            _events = events;
            _entitiesById = new Dictionary<int, EntityState>(16);
            _playerIds = new List<int>(2);
            _boxIds = new List<int>(8);
            _cameraIds = new List<int>(8);
            _robotIds = new List<int>(8);
            _animalIds = new List<int>(8);
            _nextEntityId = 1;
            ActivePlayerId = InvalidEntityId;
            TurnIndex = 0;
            IsGameOver = false;
            IsStageClear = false;
        }

        // 팩토리

        public static StageState FromMapDefinition(MapDefinition definition, StageEvents events = null)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            CellFlags[] flags = definition.CloneCellFlags();
            CellData[] cells = new CellData[flags.Length];
            for (int i = 0; i < flags.Length; i++)
            {
                cells[i] = new CellData
                {
                    Flags = flags[i],
                    OccupantId = CellData.EmptyOccupantId
                };
            }

            StageState state = new StageState(definition.Width, definition.Height, cells, events);

            foreach (SpawnData spawn in definition.Spawns)
            {
                EntityState entity = CreateEntityTemplate(spawn);
                state.AddEntity(entity);
            }

            if (state._playerIds.Count != 2)
            {
                throw new InvalidOperationException("StageState requires exactly two players.");
            }

            state.ActivePlayerId = state._playerIds[0];
            return state;
        }

        // 읽기 전용 쿼리

        public bool IsInside(GridPos position)
        {
            return position.X >= 0 && position.X < Width &&
                   position.Y >= 0 && position.Y < Height;
        }

        public CellData GetCell(GridPos position)
        {
            return _cells[ToIndex(position)];
        }

        public bool HasGoal(GridPos position)
        {
            return GetCell(position).HasGoal;
        }

        public bool HasTrap(GridPos position)
        {
            return GetCell(position).HasTrap;
        }

        public int GetOccupantId(GridPos position)
        {
            return GetCell(position).OccupantId;
        }

        public bool TryGetEntity(int entityId, out EntityState entity)
        {
            return _entitiesById.TryGetValue(entityId, out entity);
        }

        public int GetPlayerIdBySlot(int playerSlot)
        {
            for (int i = 0; i < _playerIds.Count; i++)
            {
                EntityState player = _entitiesById[_playerIds[i]];
                if (player.PlayerSlot == playerSlot)
                {
                    return player.Id;
                }
            }

            return InvalidEntityId;
        }

        public bool IsAnyPlayerDead()
        {
            for (int i = 0; i < _playerIds.Count; i++)
            {
                if (_entitiesById[_playerIds[i]].IsAlive == false)
                {
                    return true;
                }
            }

            return false;
        }

        public GridPos GetNearestLivingPlayerPosition(GridPos from)
        {
            int bestDistance = int.MaxValue;
            GridPos bestPosition = from;

            for (int i = 0; i < _playerIds.Count; i++)
            {
                EntityState player = _entitiesById[_playerIds[i]];
                if (!player.IsAlive)
                {
                    continue;
                }

                int distance = Math.Abs(player.Position.X - from.X) +
                               Math.Abs(player.Position.Y - from.Y);

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestPosition = player.Position;
                }
            }

            return bestPosition;
        }

        // 변이 메서드

        public void SetActivePlayer(int entityId)
        {
            if (entityId == InvalidEntityId || !_entitiesById.ContainsKey(entityId))
            {
                return;
            }

            if (ActivePlayerId == entityId)
            {
                return;
            }

            ActivePlayerId = entityId;
            _events?.RaiseActivePlayerChanged(entityId);
        }

        public void MoveEntity(int entityId, GridPos destination)
        {
            if (!_entitiesById.TryGetValue(entityId, out EntityState entity))
            {
                throw new KeyNotFoundException($"Entity id {entityId} was not found.");
            }

            GridPos from = entity.Position;
            ClearOccupant(from);
            entity.Position = destination;
            SetOccupant(destination, entity.Id);

            _events?.RaiseEntityMoved(entityId, from, destination);
        }

        public void SetFacing(int entityId, Direction facing)
        {
            if (!_entitiesById.TryGetValue(entityId, out EntityState entity))
            {
                return;
            }

            if (entity.Facing == facing)
            {
                return;
            }

            entity.Facing = facing;
            _events?.RaiseFacingChanged(entityId, facing);
        }

        public bool KillEntity(int entityId)
        {
            if (!_entitiesById.TryGetValue(entityId, out EntityState entity))
            {
                return false;
            }

            if (!entity.IsAlive)
            {
                return false;
            }

            entity.IsAlive = false;
            ClearOccupant(entity.Position);

            _events?.RaiseEntityKilled(entityId);
            return true;
        }

        // 함정을 비활성화한다 (상자가 덮었을 때).
        public void DisableTrap(GridPos position)
        {
            if (!IsInside(position))
            {
                return;
            }

            int index = ToIndex(position);
            CellData cell = _cells[index];

            if (!cell.HasTrap)
            {
                return;
            }

            cell.Flags &= ~CellFlags.Trap;
            _cells[index] = cell;
        }

        // 모든 카메라를 시계 방향으로 1단계 회전한다.
        public void RotateAllCameras()
        {
            for (int i = 0; i < _cameraIds.Count; i++)
            {
                if (_entitiesById.TryGetValue(_cameraIds[i], out EntityState camera) && camera.IsAlive)
                {
                    Direction newFacing = camera.Facing.RotateClockwise();
                    camera.Facing = newFacing;
                    _events?.RaiseFacingChanged(camera.Id, newFacing);
                }
            }
        }

        // 로봇 순찰 인덱스를 1칸 전진시킨다 (다른 스테이지용).
        public void AdvancePatrolIndex(int entityId)
        {
            if (!_entitiesById.TryGetValue(entityId, out EntityState entity))
            {
                return;
            }

            if (entity.PatrolRoute == null || entity.PatrolRoute.Length == 0)
            {
                return;
            }

            entity.PatrolIndex = (entity.PatrolIndex + 1) % entity.PatrolRoute.Length;
        }

        public void MarkGameOver()
        {
            if (IsGameOver)
            {
                return;
            }

            IsGameOver = true;
            _events?.RaiseGameOver();
        }

        public void MarkStageClear()
        {
            if (IsStageClear)
            {
                return;
            }

            IsStageClear = true;
            _events?.RaiseStageClear();
        }

        public void AdvanceTurn()
        {
            TurnIndex++;
            _events?.RaiseTurnAdvanced(TurnIndex);
        }

        // 내부 유틸리티

        private int AddEntity(EntityState entity)
        {
            entity.Id = _nextEntityId++;
            _entitiesById.Add(entity.Id, entity);

            if (entity.IsBlocking)
            {
                SetOccupant(entity.Position, entity.Id);
            }

            switch (entity.Kind)
            {
                case EntityKind.Player:
                    _playerIds.Add(entity.Id);
                    _playerIds.Sort(ComparePlayersBySlot);
                    break;

                case EntityKind.Box:
                    _boxIds.Add(entity.Id);
                    break;

                case EntityKind.CameraEnemy:
                    _cameraIds.Add(entity.Id);
                    break;

                case EntityKind.RobotEnemy:
                    _robotIds.Add(entity.Id);
                    break;

                case EntityKind.AnimalEnemy:
                    _animalIds.Add(entity.Id);
                    break;
            }

            return entity.Id;
        }

        private static EntityState CreateEntityTemplate(SpawnData spawn)
        {
            EntityState entity = new EntityState(
                InvalidEntityId, spawn.Kind, spawn.Position,
                spawn.Facing, spawn.PlayerSlot);

            switch (spawn.Kind)
            {
                case EntityKind.Player:
                    entity.IsBlocking = true;
                    entity.BlocksCameraSight = false;
                    break;

                case EntityKind.Box:
                    entity.IsBlocking = true;
                    entity.BlocksCameraSight = true;
                    entity.BoxOwnership = spawn.BoxOwnership;
                    break;

                case EntityKind.CameraEnemy:
                    entity.IsBlocking = true;
                    entity.BlocksCameraSight = false;
                    entity.DetectionPattern = spawn.DetectionPattern;
                    break;

                case EntityKind.RobotEnemy:
                    entity.IsBlocking = true;
                    entity.BlocksCameraSight = true;
                    entity.PatrolRoute = spawn.PatrolRoute ?? Array.Empty<Direction>();
                    break;

                case EntityKind.AnimalEnemy:
                    entity.IsBlocking = true;
                    entity.BlocksCameraSight = false;
                    break;
            }

            return entity;
        }

        private void SetOccupant(GridPos position, int entityId)
        {
            int index = ToIndex(position);
            CellData cell = _cells[index];
            cell.OccupantId = entityId;
            _cells[index] = cell;
        }

        private void ClearOccupant(GridPos position)
        {
            int index = ToIndex(position);
            CellData cell = _cells[index];
            cell.OccupantId = CellData.EmptyOccupantId;
            _cells[index] = cell;
        }

        private int ToIndex(GridPos position)
        {
            return (position.Y * Width) + position.X;
        }

        private int ComparePlayersBySlot(int leftId, int rightId)
        {
            EntityState left = _entitiesById[leftId];
            EntityState right = _entitiesById[rightId];
            return left.PlayerSlot.CompareTo(right.PlayerSlot);
        }
    }
}