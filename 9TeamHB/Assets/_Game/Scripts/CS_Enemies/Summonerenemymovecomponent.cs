using System;
using System.Collections.Generic;
using UnityEngine;
using MyGame2.Stage;

// B 감시자
// 1. 자기 몸 포함 3x3 감시
// 2. 플레이어 감지 시 0.5초 정지 후 추적형 감시자 1마리 소환
// 3. 같은 플레이어를 계속 보고 있어도 중복 소환 금지
// 4. 새로운 플레이어가 새롭게 감시 범위에 들어오면 추가 소환 가능
// 5. 추격자 소환 후 다시 순찰 복귀
// 6. 비행형이지만 부쉬 타일 진입 불가

public class SummonerEnemyMoveComponent : IComponentData, IUpdate, IDisposable
{
    public EntityFunctionSO Definition { get; }

    private readonly StageStateReferenceSO _stageStateRef;
    private readonly EntityState _entityState;
    private readonly FloatEventChannelSO _eventChannel;

    private EnemyAIState _currentState;
    private float _timer;

    private readonly float _moveInterval;
    private readonly float _alertDuration;
    private readonly int _padding;
    private readonly EntitySO _chaserDefinition;

    private int _rectMinX;
    private int _rectMaxX;
    private int _rectMinY;
    private int _rectMaxY;

    private readonly HashSet<int> _currentVisiblePlayers = new HashSet<int>();
    private readonly HashSet<int> _previousVisiblePlayers = new HashSet<int>();
    private readonly Queue<int> _pendingSummonPlayerIds = new Queue<int>();
    private int _alertTargetPlayerId = StageState.InvalidEntityId;

    public SummonerEnemyMoveComponent(
        SummonerEnemyMove_Fn definition,
        StageStateReferenceSO stageStateRef,
        EntityState entityState,
        FloatEventChannelSO eventChannel)
    {
        Definition = definition;
        _stageStateRef = stageStateRef;
        _entityState = entityState;

        _moveInterval = definition.MoveInterval;
        _alertDuration = definition.AlertDuration;
        _padding = definition.PatrolRadius;
        _chaserDefinition = definition.ChaserDefinition;

        _eventChannel = eventChannel;
        _eventChannel.OnEventRaised += OnUpdate;

        _currentState = EnemyAIState.Patrol;
        _timer = 0f;
    }

    public void OnUpdate(float dt)
    {
        StageState state = _stageStateRef.Instance;
        if (state == null) return;
        if (!_entityState.IsAlive) return;
        if (state.IsGameOver || state.IsStageClear) return;

        RefreshDetection(state);

        switch (_currentState)
        {
            case EnemyAIState.Patrol:
                UpdatePatrol(state, dt);
                break;

            case EnemyAIState.Alert:
                UpdateAlert(state, dt);
                break;
        }
        if (_currentState == EnemyAIState.Alert || _currentState == EnemyAIState.Chase)
        {
            _eventChannel.OnAlertAndChaseRaised(_alertTargetPlayerId);
        }
    }

    private void UpdatePatrol(StageState state, float dt)
    {
        if (_pendingSummonPlayerIds.Count > 0)
        {
            _alertTargetPlayerId = _pendingSummonPlayerIds.Dequeue();
            _currentState = EnemyAIState.Alert;
            _timer = 0f;
            return;
        }

        _timer += dt;
        if (_timer < _moveInterval) return;
        _timer -= _moveInterval;

        RecalculateRect(state);
        MoveAlongPerimeter(state);
    }

    private void UpdateAlert(StageState state, float dt)
    {
        _timer += dt;
        if (_timer < _alertDuration) return;

        if (_alertTargetPlayerId != StageState.InvalidEntityId)
        {
            SpawnChaser(state, _alertTargetPlayerId);
        }

        _alertTargetPlayerId = StageState.InvalidEntityId;
        _currentState = EnemyAIState.Patrol;
        _timer = 0f;
    }

    private void RefreshDetection(StageState state)
    {
        _currentVisiblePlayers.Clear();

        GridPos center = _entityState.Position;

        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                GridPos pos = new GridPos(center.X + dx, center.Y + dy);
                if (!state.IsInside(pos)) continue;

                CellData cell = state.GetCell(pos);
                if (cell.HasWall) continue;
                if (cell.HasBush) continue;
                if (!cell.IsOccupied) continue;

                if (!state.TryGetEntity(cell.OccupantId, out EntityState occ)) continue;
                if (!occ.IsPlayer || !occ.IsAlive) continue;

                _currentVisiblePlayers.Add(occ.Id);
            }
        }

        foreach (int playerId in _currentVisiblePlayers)
        {
            if (_previousVisiblePlayers.Contains(playerId))
                continue;

            if (!ContainsPendingPlayer(playerId) &&
                _alertTargetPlayerId != playerId)
            {
                _pendingSummonPlayerIds.Enqueue(playerId);
            }
        }

        _previousVisiblePlayers.Clear();
        foreach (int playerId in _currentVisiblePlayers)
        {
            _previousVisiblePlayers.Add(playerId);
        }
    }

    private bool ContainsPendingPlayer(int playerId)
    {
        foreach (int queued in _pendingSummonPlayerIds)
        {
            if (queued == playerId)
                return true;
        }
        return false;
    }

    private void RecalculateRect(StageState state)
    {
        int p1Id = state.GetPlayerIdBySlot(1);
        int p2Id = state.GetPlayerIdBySlot(2);

        GridPos p1 = _entityState.Position;
        GridPos p2 = _entityState.Position;

        if (p1Id != StageState.InvalidEntityId &&
            state.TryGetEntity(p1Id, out EntityState e1) && e1.IsAlive)
            p1 = e1.Position;

        if (p2Id != StageState.InvalidEntityId &&
            state.TryGetEntity(p2Id, out EntityState e2) && e2.IsAlive)
            p2 = e2.Position;

        _rectMinX = Math.Max(1, Math.Min(p1.X, p2.X) - _padding);
        _rectMaxX = Math.Min(state.Width - 2, Math.Max(p1.X, p2.X) + _padding);
        _rectMinY = Math.Max(1, Math.Min(p1.Y, p2.Y) - _padding);
        _rectMaxY = Math.Min(state.Height - 2, Math.Max(p1.Y, p2.Y) + _padding);
    }

    private void MoveAlongPerimeter(StageState state)
    {
        GridPos pos = _entityState.Position;

        if (!IsOnPerimeter(pos))
        {
            GridPos nearest = FindNearestPerimeterCell(state, pos);
            MoveToward(state, nearest);
            return;
        }

        GridPos next = GetNextPerimeterCell(state, pos);
        FlyTo(state, next);
    }

    private bool IsOnPerimeter(GridPos pos)
    {
        if (pos.X < _rectMinX || pos.X > _rectMaxX ||
            pos.Y < _rectMinY || pos.Y > _rectMaxY)
            return false;

        return pos.X == _rectMinX || pos.X == _rectMaxX ||
               pos.Y == _rectMinY || pos.Y == _rectMaxY;
    }

    private GridPos GetNextPerimeterCell(StageState state, GridPos pos)
    {
        if (pos.Y == _rectMinY && pos.X < _rectMaxX)
            return TryPerimeter(state, pos.X + 1, pos.Y, pos);
        if (pos.X == _rectMaxX && pos.Y < _rectMaxY)
            return TryPerimeter(state, pos.X, pos.Y + 1, pos);
        if (pos.Y == _rectMaxY && pos.X > _rectMinX)
            return TryPerimeter(state, pos.X - 1, pos.Y, pos);
        if (pos.X == _rectMinX && pos.Y > _rectMinY)
            return TryPerimeter(state, pos.X, pos.Y - 1, pos);

        if (pos.X == _rectMinX && pos.Y == _rectMinY)
            return TryPerimeter(state, pos.X + 1, pos.Y, pos);
        if (pos.X == _rectMaxX && pos.Y == _rectMinY)
            return TryPerimeter(state, pos.X, pos.Y + 1, pos);
        if (pos.X == _rectMaxX && pos.Y == _rectMaxY)
            return TryPerimeter(state, pos.X - 1, pos.Y, pos);
        if (pos.X == _rectMinX && pos.Y == _rectMaxY)
            return TryPerimeter(state, pos.X, pos.Y - 1, pos);

        return pos;
    }

    private GridPos TryPerimeter(StageState state, int x, int y, GridPos fallback)
    {
        GridPos next = new GridPos(x, y);
        if (!state.IsInside(next)) return fallback;
        if (state.GetCell(next).HasWall) return fallback;
        if (state.GetCell(next).HasBush) return fallback; // 부쉬 진입 불가
        return next;
    }

    private GridPos FindNearestPerimeterCell(StageState state, GridPos from)
    {
        GridPos best = from;
        int bestDist = int.MaxValue;

        for (int x = _rectMinX; x <= _rectMaxX; x++)
        {
            Check(state, from, x, _rectMinY, ref best, ref bestDist);
            Check(state, from, x, _rectMaxY, ref best, ref bestDist);
        }
        for (int y = _rectMinY + 1; y < _rectMaxY; y++)
        {
            Check(state, from, _rectMinX, y, ref best, ref bestDist);
            Check(state, from, _rectMaxX, y, ref best, ref bestDist);
        }

        return best;
    }

    private void Check(StageState state, GridPos from, int x, int y, ref GridPos best, ref int bestDist)
    {
        GridPos p = new GridPos(x, y);
        if (!state.IsInside(p)) return;
        if (state.GetCell(p).HasWall) return;
        if (state.GetCell(p).HasBush) return; // 부쉬 제외

        int d = Math.Abs(p.X - from.X) + Math.Abs(p.Y - from.Y);
        if (d < bestDist) { bestDist = d; best = p; }
    }

    private void MoveToward(StageState state, GridPos target)
    {
        if (_entityState.Position.Equals(target)) return;
        Direction dir = Dir(_entityState.Position, target);
        if (dir == Direction.None) return;
        GridPos next = _entityState.Position.Move(dir);
        FlyTo(state, next);
    }

    // 비행 이동: 점유 시스템 우회 (상자 위를 지나가도 상자 점유 안 건드림)
    private void FlyTo(StageState state, GridPos target)
    {
        if (!state.IsInside(target)) return;
        CellData cell = state.GetCell(target);
        if (cell.HasWall) return;
        if (cell.HasBush) return;
        if (_entityState.Position.Equals(target)) return;

        Direction dir = Dir(_entityState.Position, target);
        if (dir == Direction.None) return;

        state.SetFacing(_entityState.Id, dir);
        // MoveEntity 대신 Position 직접 변경 (점유 시스템 우회)
        _entityState.Position = target;
        state.SetViewDirty();
    }

    private void SpawnChaser(StageState state, int targetPlayerId)
    {
        if (_chaserDefinition == null) return;
        if (targetPlayerId == StageState.InvalidEntityId) return;

        Direction facing = _entityState.Facing;
        if (facing == Direction.None) facing = Direction.Down;

        GridPos frontPos = _entityState.Position.Move(facing);
        GridPos spawnPos = FindOpen(state, frontPos);

        int chaserId = state.SpawnEntity(_chaserDefinition, spawnPos, facing);
        ChaserTargetRegistry.Register(chaserId, targetPlayerId);

        state.SetViewDirty();
    }

    private GridPos FindOpen(StageState state, GridPos target)
    {
        if (state.IsInside(target))
        {
            CellData cell = state.GetCell(target);
            if (!cell.HasWall && !cell.IsOccupied && !cell.HasBush)
                return target;
        }

        Direction[] dirs = { Direction.Right, Direction.Down, Direction.Left, Direction.Up };
        for (int i = 0; i < dirs.Length; i++)
        {
            GridPos around = _entityState.Position.Move(dirs[i]);
            if (!state.IsInside(around)) continue;
            CellData c = state.GetCell(around);
            if (!c.HasWall && !c.IsOccupied && !c.HasBush)
                return around;
        }

        return _entityState.Position;
    }

    private static Direction Dir(GridPos from, GridPos to)
    {
        int dx = to.X - from.X;
        int dy = to.Y - from.Y;

        if (Math.Abs(dx) + Math.Abs(dy) == 1)
        {
            if (dx == 1) return Direction.Right;
            if (dx == -1) return Direction.Left;
            if (dy == 1) return Direction.Down;
            if (dy == -1) return Direction.Up;
        }

        if (Math.Abs(dx) >= Math.Abs(dy))
            return dx > 0 ? Direction.Right : Direction.Left;

        return dy > 0 ? Direction.Down : Direction.Up;
    }

    public void Dispose()
    {
        _eventChannel.OnEventRaised -= OnUpdate;
    }
}