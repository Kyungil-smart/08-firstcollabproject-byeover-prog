using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyGame2.Stage
{
    public sealed class StageState
    {
        public const int InvalidEntityId = -1;

        private readonly CellData[] _cells;
        private readonly CellFlags[] _originalFlags;
        private readonly Dictionary<int, EntityState> _entitiesById;
        private readonly List<int> _playerIds;
        private readonly List<int> _boxIds;
        private readonly List<int> _cameraIds;
        private readonly List<int> _robotIds;
        private readonly List<int> _animalIds;
        private readonly List<int> _patrolCameraIds;
        private readonly List<int> _summonerIds;
        private readonly List<int> _chaserIds;
        private readonly List<int> _launcherIds;
        private readonly List<int> _projectileIds;
        private readonly List<int> _sawTrapIds;
        // 셀 페어 (스위치-문 등)
        private readonly Dictionary<GridPos, GridPos> _cellPairs = new Dictionary<GridPos, GridPos>();

        private int _nextEntityId;
        private readonly StageEvents _events;

        public int Width { get; private set; }
        public int Height { get; private set; }
        public int ActivePlayerId { get; private set; }
        public int TurnIndex { get; private set; }
        public bool IsGameOver { get; private set; }
        public bool IsStageClear { get; private set; }
        // Undo 시스템
        public bool IsUndoProcessing { get; set; }
        // RobotAutoMover용 — 게임 진행 가능 상태인지
        public bool IsUpdatable() { return !IsGameOver && !IsStageClear && !IsUndoProcessing; }
        // Undo 스냅샷용 — 셀 배열 직접 접근
        public CellData[] Cells { get { return _cells; } }
        // Undo 스냅샷용 — 엔티티 딕셔너리 직접 접근
        public Dictionary<int, EntityState> EntityDict { get { return _entitiesById; } }

        public void UndoEnter()
        {
            IsUndoProcessing = true;
        }

        public void UndoLeave()
        {
            IsUndoProcessing = false;
        }

        public void Restore(StageSnapshot snapshot)
        {
            if (snapshot == null) return;

            // 셀 복원
            System.Array.Copy(snapshot.Cells, _cells, _cells.Length);

            // 엔티티 복원
            _entitiesById.Clear();
            foreach (var kvp in snapshot.EntityDict)
                _entitiesById[kvp.Key] = kvp.Value;

            // 상태 복원
            ActivePlayerId = snapshot.ActivePlayerId;
            TurnIndex = snapshot.TurnIndex;
            IsGameOver = snapshot.IsGameOver;
            IsStageClear = snapshot.IsStageClear;
            IsViewDirty = snapshot.IsViewDirty;

            // 리스트 복원
            RebuildEntityLists();

            SetViewDirty();
        }

        // 엔티티 딕셔너리에서 종류별 ID 리스트 재구축
        private void RebuildEntityLists()
        {
            _playerIds.Clear();
            _boxIds.Clear();
            _cameraIds.Clear();
            _robotIds.Clear();
            _animalIds.Clear();
            _patrolCameraIds.Clear();
            _summonerIds.Clear();
            _chaserIds.Clear();
            _launcherIds.Clear();
            _projectileIds.Clear();
            _sawTrapIds.Clear();

            foreach (var kvp in _entitiesById)
            {
                EntityState e = kvp.Value;
                switch (e.Kind)
                {
                    case EntityKind.Player:
                        _playerIds.Add(e.Id);
                        break;
                    case EntityKind.Box:                  _boxIds.Add(e.Id); break;
                    case EntityKind.CameraEnemy:          _cameraIds.Add(e.Id); break;
                    case EntityKind.RobotEnemy:           _robotIds.Add(e.Id); break;
                    case EntityKind.AnimalEnemy:          _animalIds.Add(e.Id); break;
                    case EntityKind.PatrolCameraEnemy:    _patrolCameraIds.Add(e.Id); break;
                    case EntityKind.SummonerEnemy:        _summonerIds.Add(e.Id); break;
                    case EntityKind.ChaserEnemy:          _chaserIds.Add(e.Id); break;
                    case EntityKind.ProjectileLauncher:   _launcherIds.Add(e.Id); break;
                    case EntityKind.Projectile:           _projectileIds.Add(e.Id); break;
                    case EntityKind.SawTrapEnemy:         _sawTrapIds.Add(e.Id); break;
                    case EntityKind.DoorEntity:
                    case EntityKind.LeverEntity:
                    case EntityKind.ButtonEntity:
                        break;
                }
            }

            _playerIds.Sort((a, b) =>
                _entitiesById[a].Get<PlayerData>().Slot
                    .CompareTo(_entitiesById[b].Get<PlayerData>().Slot));
        }

        public IReadOnlyList<int> PlayerIds { get { return _playerIds; } }
        public IReadOnlyList<int> BoxIds { get { return _boxIds; } }
        public IReadOnlyList<int> CameraIds { get { return _cameraIds; } }
        public IReadOnlyList<int> RobotIds { get { return _robotIds; } }
        public IReadOnlyList<int> AnimalIds { get { return _animalIds; } }
        public IReadOnlyList<int> PatrolCameraIds { get { return _patrolCameraIds; } }
        public IReadOnlyList<int> SummonerIds { get { return _summonerIds; } }
        public IReadOnlyList<int> ChaserIds { get { return _chaserIds; } }
        public IReadOnlyList<int> LauncherIds { get { return _launcherIds; } }
        public IReadOnlyList<int> ProjectileIds { get { return _projectileIds; } }
        public IReadOnlyList<int> SawTrapIds { get { return _sawTrapIds; } }
        public IEnumerable<EntityState> Entities { get { return _entitiesById.Values; } }
        public StageEvents Events { get { return _events; } }
        public bool IsViewDirty { get; private set; }

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
            _patrolCameraIds = new List<int>(4);
            _summonerIds = new List<int>(4);
            _chaserIds = new List<int>(4);
            _launcherIds = new List<int>(4);
            _projectileIds = new List<int>(16);
            _sawTrapIds = new List<int>(8);
            _cellPairs = new Dictionary<GridPos, GridPos>(16);
            _nextEntityId = 1;
            ActivePlayerId = InvalidEntityId;
            IsViewDirty = false;
        }

        // 팩토리

        public static StageState FromMapDefinition(MapDefinition definition, StageEvents events = null)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            CellFlags[] flags = definition.CloneCellFlags();
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
            return new EntityState(spawn.Def, spawn.Position, spawn.Facing);
        }

        // 읽기 전용 쿼리

        public bool IsInside(GridPos pos)
        {
            return pos.X >= 0 && pos.X < Width && pos.Y >= 0 && pos.Y < Height;
        }

        public CellData GetCell(GridPos pos) { return _cells[ToIndex(pos)]; }
        public bool HasGoal(GridPos pos) { return GetCell(pos).HasGoal; }
        public bool HasTrap(GridPos pos) { return GetCell(pos).HasTrap; }
        public bool HasCrackNotCovered(GridPos pos) { return GetCell(pos).HasCrack && !GetCell(pos).HasActive; }
        public bool HasBush(GridPos pos) { return GetCell(pos).HasBush; }
        public bool HasHiddenTrap(GridPos pos) { return GetCell(pos).HasHiddenTrap; }
        public bool HasDestroyTrap(GridPos pos) { return GetCell(pos).HasDestroyTrap; }
        public bool HasSawTrapActive(GridPos pos) { return GetCell(pos).IsSawTrapActive; }
        
        // 각 SawTrap 엔티티의 앵커 위치 + Facing 방향으로 Size칸 범위를 검사
        // 앵커 셀에 Active 플래그가 켜져 있으면 (버튼/스위치로 비활성화) 안전
        public bool IsInSawTrapRange(GridPos pos)
        {
            for (int i = 0; i < _sawTrapIds.Count; i++)
            {
                if (!_entitiesById.TryGetValue(_sawTrapIds[i], out EntityState trap)) continue;
                if (!trap.IsAlive) continue;

                CellData anchorCell = GetCell(trap.Position);
                if (anchorCell.HasActive) continue;

                SawTrapData data = trap.Get<SawTrapData>();
                GridPos cursor = trap.Position;

                for (int step = 0; step < data.Size; step++)
                {
                    if (cursor == pos) return true;
                    cursor = cursor.Move(trap.Facing);
                }
            }
            return false;
        }

        // 특정 위치를 커버하는 SawTrap 엔티티를 찾아 Facing 방향을 반환 (쪼개짐 연출용)
        public Direction GetSawTrapFacingAt(GridPos pos)
        {
            for (int i = 0; i < _sawTrapIds.Count; i++)
            {
                if (!_entitiesById.TryGetValue(_sawTrapIds[i], out EntityState trap)) continue;
                if (!trap.IsAlive) continue;

                CellData anchorCell = GetCell(trap.Position);
                if (anchorCell.HasActive) continue;

                SawTrapData data = trap.Get<SawTrapData>();
                GridPos cursor = trap.Position;

                for (int step = 0; step < data.Size; step++)
                {
                    if (cursor == pos) return trap.Facing;
                    cursor = cursor.Move(trap.Facing);
                }
            }
            return Direction.Right;
        }
        public int GetOccupantId(GridPos pos) { return GetCell(pos).OccupantId; }

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
                if (p.Get<PlayerData>().Slot == slot) return p.Id;
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

        // 셀 페어 (스위치↔문 등)
        public void SetCellPair(GridPos a, GridPos b)
        {
            _cellPairs[a] = b;
            _cellPairs[b] = a;
        }
        public bool TryGetCellPair(GridPos a, out GridPos b)
        {
            return _cellPairs.TryGetValue(a, out b);
        }

        // 잠긴 문 위에 있는지 판정
        public bool IsPlayerOnLockedDoor
        {
            get
            {
                TryGetEntity(ActivePlayerId, out EntityState player);
                return GetCell(player.Position).IsClosedDoor;
            }
        }

        // 변이 메서드

        public void SetActivePlayer(int entityId)
        {
            if (entityId == InvalidEntityId || !_entitiesById.ContainsKey(entityId)) return;
            if (ActivePlayerId == entityId) return;
            ActivePlayerId = entityId;
            _events?.RaiseActivePlayerChanged(entityId);
        }

        public bool TryMoveEntity(int entityId, GridPos destination)
        {
            if (!_entitiesById.TryGetValue(entityId, out EntityState entity)) return false;
            if (!entity.IsAlive) return false;

            GridPos from = entity.Position;
            ClearOccupant(from);

            // 버튼 점유 해제 처리
            if (GetCell(from).HasSignalButton && !GetCell(from).IsSticky)
            {
                DeactivePairCell(from);
            }

            // 상자가 함정에서 벗어나면 함정 재생
            if (entity.IsBox && OriginalHasTrap(from))
                RestoreTrap(from);

            // 예정지가 버튼계열이면 페어 활성화
            if (GetCell(destination).HasSignalButton)
            {
                ActivePairCell(destination);
            }

            entity.Position = destination;
            SetOccupant(destination, entity.Id);
            _events?.RaiseEntityMoved(entityId, from, destination);
            return true;
        }

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

        // 히든 함정 발동
        public void RevealHiddenTrap(GridPos position)
        {
            if (!IsInside(position)) return;
            int idx = ToIndex(position);
            CellData cell = _cells[idx];
            if (!cell.HasHiddenTrap) return;

            cell.Flags &= ~CellFlags.HiddenTrap;
            cell.Flags |= CellFlags.Trap;
            _cells[idx] = cell;

            _events?.RaiseHiddenTrapRevealed(position);
        }

        public void DisableTrap(GridPos position)
        {
            if (!IsInside(position)) return;
            int idx = ToIndex(position);
            CellData cell = _cells[idx];
            if (!cell.HasTrap) return;
            cell.Flags &= ~CellFlags.Trap;
            _cells[idx] = cell;
        }

        private void RestoreTrap(GridPos position)
        {
            if (!IsInside(position)) return;
            int idx = ToIndex(position);
            CellData cell = _cells[idx];
            cell.Flags |= CellFlags.Trap;
            _cells[idx] = cell;
        }

        private void EnableTrap(GridPos position)
        {
            if (!IsInside(position)) return;
            int idx = ToIndex(position);
            CellData cell = _cells[idx];
            cell.Flags |= CellFlags.Trap;
            _cells[idx] = cell;
        }

        // 감시영역 함정화 (CCTV / PatrolCamera 적발 후 사용)
        public void TrapifyCells(List<GridPos> cells)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                GridPos pos = cells[i];
                if (!IsInside(pos)) continue;

                EnableTrap(pos);

                int idx = ToIndex(pos);
                CellData cell = _cells[idx];
                if (cell.IsOccupied &&
                    TryGetEntity(cell.OccupantId, out EntityState occupant) &&
                    occupant.IsPlayer && occupant.IsAlive)
                {
                    KillEntity(occupant.Id);
                    MarkGameOver();
                }
            }
            SetViewDirty();
        }

        // 틈새에 상자가 올라갔을 때
        public void SetCrackMovable(GridPos position, int boxId)
        {
            if (!IsInside(position)) return;
            int idx = ToIndex(position);
            CellData cell = _cells[idx];
            if (!cell.HasCrack) return;
            cell.Flags |= CellFlags.Active;
            cell.OccupantId = -1;
            _cells[idx] = cell;

            if (TryGetEntity(boxId, out EntityState box))
            {
                if (!box.Has<Fallable>())
                {
                    return;
                }
                box.Get<Fallable>().StartFallAnimation(this);
            }
        }

        // 문 활성화
        public void OpenDoor(int moverId, GridPos position)
        {
            if (!IsInside(position)) return;
            int idx = ToIndex(position);
            CellData cell = _cells[idx];
            cell.Flags |= (CellFlags.Active | CellFlags.OpenFixed);
            _cells[idx] = cell;

            if (TryGetEntity(moverId, out EntityState mover))
            {
                mover.Get<PocketData>().TryUseKey();
            }
        }

        // 페어 셀 활성화
        private void ActivePairCell(GridPos position)
        {
            if (!_cellPairs.TryGetValue(position, out GridPos pair)) return;
            if (!IsInside(pair)) return;
            int idx = ToIndex(pair);
            CellData pairCell = _cells[idx];
            if (!pairCell.HasActive)
            {
                pairCell.Flags |= CellFlags.Active;
            }
            _cells[idx] = pairCell;
        }

        // 페어 셀 비활성화
        private void DeactivePairCell(GridPos position)
        {
            if (!_cellPairs.TryGetValue(position, out GridPos pair)) return;

            int idx = ToIndex(pair);
            CellData pairCell = _cells[idx];
            if (pairCell.HasActive && !pairCell.IsOpenFixed)
            {
                pairCell.Flags &= ~CellFlags.Active;
            }
            _cells[idx] = pairCell;
        }

        public void RotateAllCameras()
        {
            for (int i = 0; i < _cameraIds.Count; i++)
            {
                if (!_entitiesById.TryGetValue(_cameraIds[i], out EntityState cam)) continue;
                if (!cam.IsAlive) continue;

                CameraData data = cam.Get<CameraData>();
                if (data.Pattern == CameraType.Fixed3x3) continue;

                Direction next = data.ReverseRotation
                    ? cam.Facing.RotateClockwise().RotateClockwise().RotateClockwise()
                    : cam.Facing.RotateClockwise();

                cam.Facing = next;
                _events?.RaiseFacingChanged(cam.Id, next);
            }
        }

        public void AdvancePatrolIndex(int entityId)
        {
            if (!_entitiesById.TryGetValue(entityId, out EntityState entity)) return;
            PatrolData patrol = entity.Get<PatrolData>();
            if (patrol == null || !patrol.HasWaypoints) return;
            patrol.AdvanceToNext();
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

        public void SetViewDirty() { IsViewDirty = true; }
        public void ClearViewDirty() { IsViewDirty = false; }

        public int SpawnEntity(EntitySO definition, GridPos position, Direction facing)
        {
            EntityState entity = new EntityState(definition, position, facing);
            return AddEntity(entity);
        }

        public bool RemoveEntity(int entityId)
        {
            if (!_entitiesById.TryGetValue(entityId, out EntityState entity)) return false;
            entity.IsAlive = false;
            ClearOccupant(entity.Position);

            switch (entity.Kind)
            {
                case EntityKind.Box:                  _boxIds.Remove(entityId); break;
                case EntityKind.ChaserEnemy:          _chaserIds.Remove(entityId); break;
                case EntityKind.SummonerEnemy:        _summonerIds.Remove(entityId); break;
                case EntityKind.PatrolCameraEnemy:    _patrolCameraIds.Remove(entityId); break;
                case EntityKind.ProjectileLauncher:   _launcherIds.Remove(entityId); break;
                case EntityKind.Projectile:           _projectileIds.Remove(entityId); break;
                case EntityKind.SawTrapEnemy:          _sawTrapIds.Remove(entityId); break;
            }

            _entitiesById.Remove(entityId);
            _events?.RaiseEntityKilled(entityId);
            return true;
        }

        // 내부

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
                        _entitiesById[a].Get<PlayerData>().Slot
                            .CompareTo(_entitiesById[b].Get<PlayerData>().Slot));
                    break;
                case EntityKind.Box:                  _boxIds.Add(entity.Id); break;
                case EntityKind.CameraEnemy:          _cameraIds.Add(entity.Id); break;
                case EntityKind.RobotEnemy:           _robotIds.Add(entity.Id); break;
                case EntityKind.AnimalEnemy:           _animalIds.Add(entity.Id); break;
                case EntityKind.PatrolCameraEnemy:     _patrolCameraIds.Add(entity.Id); break;
                case EntityKind.SummonerEnemy:         _summonerIds.Add(entity.Id); break;
                case EntityKind.ChaserEnemy:           _chaserIds.Add(entity.Id); break;
                case EntityKind.ProjectileLauncher:    _launcherIds.Add(entity.Id); break;
                case EntityKind.Projectile:            _projectileIds.Add(entity.Id); break;
                case EntityKind.SawTrapEnemy:           _sawTrapIds.Add(entity.Id); break;
                    case EntityKind.DoorEntity:
                    case EntityKind.LeverEntity:
                    case EntityKind.ButtonEntity:
                        break;
            }
            return entity.Id;
        }

        private void SetOccupant(GridPos pos, int id)
        {
            int i = ToIndex(pos);
            CellData c = _cells[i];
            c.OccupantId = id;
            _cells[i] = c;
        }

        private void ClearOccupant(GridPos pos)
        {
            int i = ToIndex(pos);
            CellData c = _cells[i];
            c.OccupantId = CellData.EmptyOccupantId;
            _cells[i] = c;
        }

        private int ToIndex(GridPos pos) { return (pos.Y * Width) + pos.X; }
    }
}