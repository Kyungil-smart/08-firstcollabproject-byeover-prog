using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyGame2.Stage
{
    // 스테이지의 유일한 상태 소유자.
    // 모든 변이 메서드는 StageEvents를 통해 이벤트를 발행한다.

    public sealed class StageState
    {
        public const int InvalidEntityId = -1;

        private readonly CellData[] _cells;
        // 원본 셀 플래그 (함정 재활성화용). 맵 로드 시 복사해둔다.
        private readonly CellFlags[] _originalFlags;
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

        private StageState(int width, int height, CellData[] cells,
            CellFlags[] originalFlags, StageEvents events)
        {
            Width = width;
            Height = height;
            _cells = cells;
            _originalFlags = originalFlags;
            _events = events;
            _entitiesById = new Dictionary<int, EntityState>(16);
            _playerIds = new List<int>(2);
            _boxIds = new List<int>(8);
            _cameraIds = new List<int>(8);
            _robotIds = new List<int>(8);
            _animalIds = new List<int>(8);
            _nextEntityId = 1;
            ActivePlayerId = InvalidEntityId;
        }

        // 팩토리

        public static StageState FromMapDefinition(MapDefinition definition, StageEvents events = null)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            CellFlags[] flags = definition.CloneCellFlags();
            // 원본 플래그 보관 (함정 재활성화용)
            CellFlags[] originalFlags = (CellFlags[])flags.Clone();

            CellData[] cells = new CellData[flags.Length];
            for (int i = 0; i < flags.Length; i++)
            {
                cells[i] = new CellData
                {
                    Flags = flags[i],
                    OccupantId = CellData.EmptyOccupantId
                };
            }

            StageState state = new StageState(
                definition.Width, definition.Height, cells, originalFlags, events);

            foreach (SpawnData spawn in definition.Spawns)
            {
                EntityState entity = CreateEntity(spawn);
                state.AddEntity(entity);
            }

            Debug.Assert(state._playerIds.Count == 2,
                "StageState requires exactly two players.");

            state.ActivePlayerId = state._playerIds[0];
            return state;
        }

        private static EntityState CreateEntity(SpawnData spawn)
        {
            switch (spawn.Kind)
            {
                case EntityKind.Player:
                    return EntityState.CreatePlayer(spawn.Position, spawn.Facing, spawn.PlayerSlot);
                case EntityKind.Box:
                    return EntityState.CreateBox(spawn.Position, spawn.BoxOwnership);
                case EntityKind.CameraEnemy:
                    return EntityState.CreateCamera(spawn.Position, spawn.Facing,
                        spawn.DetectionPattern, spawn.ReverseRotation);
                case EntityKind.RobotEnemy:
                    return EntityState.CreateRobot(spawn.Position, spawn.Facing);
                case EntityKind.AnimalEnemy:
                    return EntityState.CreateAnimal(spawn.Position, spawn.Facing);
                default:
                    throw new ArgumentException($"Unknown EntityKind: {spawn.Kind}");
            }
        }

        // 읽기 전용 쿼리

        public bool IsInside(GridPos pos)
        {
            return pos.X >= 0 && pos.X < Width && pos.Y >= 0 && pos.Y < Height;
        }

        public CellData GetCell(GridPos pos) { return _cells[ToIndex(pos)]; }
        public bool HasGoal(GridPos pos) { return GetCell(pos).HasGoal; }
        public bool HasTrap(GridPos pos) { return GetCell(pos).HasTrap; }
        public int GetOccupantId(GridPos pos) { return GetCell(pos).OccupantId; }

        // 원본에 함정이 있었는지 확인 (재활성화 판단용)
        public bool OriginalHasTrap(GridPos pos)
        {
            return (_originalFlags[ToIndex(pos)] & CellFlags.Trap) != 0;
        }

        public bool TryGetEntity(int entityId, out EntityState entity)
        {
            return _entitiesById.TryGetValue(entityId, out entity);
        }

        public int GetPlayerIdBySlot(int slot)
        {
            for (int i = 0; i < _playerIds.Count; i++)
            {
                EntityState p = _entitiesById[_playerIds[i]];
                if (p.Player.Slot == slot) return p.Id;
            }
            return InvalidEntityId;
        }

        public bool IsAnyPlayerDead()
        {
            for (int i = 0; i < _playerIds.Count; i++)
                if (!_entitiesById[_playerIds[i]].IsAlive) return true;
            return false;
        }

        public GridPos GetNearestLivingPlayerPosition(GridPos from)
        {
            int best = int.MaxValue;
            GridPos bestPos = from;
            for (int i = 0; i < _playerIds.Count; i++)
            {
                EntityState p = _entitiesById[_playerIds[i]];
                if (!p.IsAlive) continue;
                int d = Math.Abs(p.Position.X - from.X) + Math.Abs(p.Position.Y - from.Y);
                if (d < best) { best = d; bestPos = p.Position; }
            }
            return bestPos;
        }

        // 변이 메서드

        public void SetActivePlayer(int entityId)
        {
            if (entityId == InvalidEntityId || !_entitiesById.ContainsKey(entityId)) return;
            if (ActivePlayerId == entityId) return;
            ActivePlayerId = entityId;
            _events?.RaiseActivePlayerChanged(entityId);
        }

        // 엔티티를 이동시킨다.
        // 상자가 함정 셀에서 벗어나면 함정을 자동 재활성화한다.
        public bool TryMoveEntity(int entityId, GridPos destination)
        {
            if (!_entitiesById.TryGetValue(entityId, out EntityState entity)) return false;
            if (!entity.IsAlive) return false;

            GridPos from = entity.Position;
            ClearOccupant(from);

            // 상자가 떠난 셀이 원래 함정이었으면 재활성화
            if (entity.IsBox && OriginalHasTrap(from))
            {
                RestoreTrap(from);
            }

            entity.Position = destination;
            SetOccupant(destination, entity.Id);
            _events?.RaiseEntityMoved(entityId, from, destination);
            return true;
        }

        // 하위 호환용
        public void MoveEntity(int entityId, GridPos destination)
        {
            TryMoveEntity(entityId, destination);
        }

        public void SetFacing(int entityId, Direction facing)
        {
            if (!_entitiesById.TryGetValue(entityId, out EntityState entity)) return;
            if (entity.Facing == facing) return;
            entity.Facing = facing;
            _events?.RaiseFacingChanged(entityId, facing);
        }

        public bool KillEntity(int entityId)
        {
            if (!_entitiesById.TryGetValue(entityId, out EntityState entity)) return false;
            if (!entity.IsAlive) return false;
            entity.IsAlive = false;
            ClearOccupant(entity.Position);
            _events?.RaiseEntityKilled(entityId);
            return true;
        }

        // 함정을 비활성화한다 (상자가 위에 올라갔을 때).
        public void DisableTrap(GridPos position)
        {
            if (!IsInside(position)) return;
            int idx = ToIndex(position);
            CellData cell = _cells[idx];
            if (!cell.HasTrap) return;
            cell.Flags &= ~CellFlags.Trap;
            _cells[idx] = cell;
        }

        // 함정을 재활성화한다 (상자가 치워졌을 때).
        private void RestoreTrap(GridPos position)
        {
            if (!IsInside(position)) return;
            int idx = ToIndex(position);
            CellData cell = _cells[idx];
            cell.Flags |= CellFlags.Trap;
            _cells[idx] = cell;
        }

        // 모든 카메라를 회전한다. Fixed3x3은 회전하지 않는다.
        public void RotateAllCameras()
        {
            for (int i = 0; i < _cameraIds.Count; i++)
            {
                if (!_entitiesById.TryGetValue(_cameraIds[i], out EntityState cam)) continue;
                if (!cam.IsAlive) continue;

                // 고정형 카메라는 회전 안 함
                if (cam.Camera.Pattern == CameraType.Fixed3x3) continue;

                Direction next = cam.Camera.ReverseRotation
                    ? cam.Facing.RotateClockwise().RotateClockwise().RotateClockwise()
                    : cam.Facing.RotateClockwise();

                cam.Facing = next;
                _events?.RaiseFacingChanged(cam.Id, next);
            }
        }

        // 로봇 순찰 인덱스를 1칸 전진 (다른 스테이지용)
        public void AdvancePatrolIndex(int entityId)
        {
            if (!_entitiesById.TryGetValue(entityId, out EntityState entity)) return;
            if (!entity.Patrol.HasRoute) return;
            entity.Patrol.AdvanceToNext();
        }

        public void MarkGameOver()
        {
            if (IsGameOver) return;
            IsGameOver = true;
            _events?.RaiseGameOver();
        }

        public void MarkStageClear()
        {
            if (IsStageClear) return;
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
            if (entity.IsBlocking) SetOccupant(entity.Position, entity.Id);

            switch (entity.Kind)
            {
                case EntityKind.Player:
                    _playerIds.Add(entity.Id);
                    _playerIds.Sort((a, b) =>
                        _entitiesById[a].Player.Slot.CompareTo(_entitiesById[b].Player.Slot));
                    break;
                case EntityKind.Box:         _boxIds.Add(entity.Id); break;
                case EntityKind.CameraEnemy: _cameraIds.Add(entity.Id); break;
                case EntityKind.RobotEnemy:  _robotIds.Add(entity.Id); break;
                case EntityKind.AnimalEnemy: _animalIds.Add(entity.Id); break;
            }
            return entity.Id;
        }

        private void SetOccupant(GridPos pos, int id)
        {
            int i = ToIndex(pos); CellData c = _cells[i]; c.OccupantId = id; _cells[i] = c;
        }

        private void ClearOccupant(GridPos pos)
        {
            int i = ToIndex(pos); CellData c = _cells[i];
            c.OccupantId = CellData.EmptyOccupantId; _cells[i] = c;
        }

        private int ToIndex(GridPos pos) { return (pos.Y * Width) + pos.X; }
    }
}