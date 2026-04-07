using System;
using System.Collections.Generic;
using UnityEngine;
using MyGame2.Stage;

public class SummonerMove : IComponentData, IUpdate, IDisposable
{
    private readonly StageStateReferenceSO _stageStateRef;
    private readonly EntityState _entityState;
    private readonly FloatEventChannelSO _eventChannel;

    private EnemyAIState _currentState;
    private float _timer;
    private StageState StageState{get{return _stageStateRef.Instance;}}

    private readonly float _moveInterval;
    private readonly float _alertDuration;
    
    private readonly EntitySO _chaserDefinition;
    
    private readonly HashSet<int> _currentVisiblePlayers = new HashSet<int>();
    private readonly HashSet<int> _previousVisiblePlayers = new HashSet<int>();
    private readonly Queue<int> _pendingSummonPlayerIds = new Queue<int>();
    private int _alertTargetPlayerId = StageState.InvalidEntityId;

    public SummonerMove(
        StageStateReferenceSO stageStateRef,
        EntityState entityState,
        FloatEventChannelSO eventChannel,
        float moveInterval, float alertDuration,
        EntitySO chaserDefinition)
    {
        _stageStateRef = stageStateRef;
        _entityState = entityState;
        _eventChannel = eventChannel;
        _eventChannel.OnEventRaised += OnUpdate;
        
        _moveInterval = moveInterval;
        _alertDuration = alertDuration;
        
        _chaserDefinition = chaserDefinition;

        _currentState = EnemyAIState.Patrol;
        _timer = 0f;
    }
    
    public void OnUpdate(float dt)
    {
        if (!_entityState.IsAlive) return;
        if(StageState.IsGameOver || StageState.IsStageClear) return;
        
        RefreshDetection();
        
        switch (_currentState)
        {
            case EnemyAIState.Patrol:
                UpdatePatrol(dt);
                break;
            case EnemyAIState.Alert:
                UpdateAlert(dt);
                break;
        }
    }

    void UpdatePatrol(float dt)
    {
        
        //감지
        if (_pendingSummonPlayerIds.Count > 0)
        {
            _alertTargetPlayerId = _pendingSummonPlayerIds.Dequeue();
            _currentState = EnemyAIState.Alert;
            _timer = 0f;
            _eventChannel.OnAlertAndChaseRaised(_alertTargetPlayerId);
            return;
        }
        
        //이동
        _timer += dt;
        if (_timer < _moveInterval) return;
        _timer -= _moveInterval;
        DoMove();
    }

    void UpdateAlert(float dt)
    {
        _timer += dt;
        if (_timer < _alertDuration) return;

        if (_alertTargetPlayerId != StageState.InvalidEntityId)
        {
            SpawnChaser(_alertTargetPlayerId);
        }

        _alertTargetPlayerId = StageState.InvalidEntityId;
        _currentState = EnemyAIState.Patrol;
        _timer = 0f;
    }

    void DoMove()
    {
        PatrolData patrol = _entityState.Get<PatrolData>();
        if (patrol == null || !patrol.HasWaypoints) return;
        Direction dir = patrol.GetDirectionFrom(_entityState.Position);

        if (dir == Direction.None)
        {
            patrol.AdvanceToNext();
            dir = patrol.GetDirectionFrom(_entityState.Position);
            if (dir == Direction.None) return;
        }
        GridPos next = _entityState.Position.Move(dir);
        if (StageState.GetCell(next).HasBush)
        {
            patrol.Reverse();
            next = _entityState.Position.Move(patrol.GetDirectionFrom(_entityState.Position));
        }
        MoveToward(next);
    }

    void MoveToward(GridPos target)
    {
        if (_entityState.Position.Equals(target)) return;
        Direction dir = Dir(_entityState.Position, target);
        if (dir == Direction.None) return;
        GridPos next = _entityState.Position.Move(dir);
        FlyTo(next);
    }

    void FlyTo(GridPos target)
    {
        if(!StageState.IsInside(target)) return;
        CellData cell = StageState.GetCell(target);
        
        if (cell.HasWall) return;
        if (cell.HasBush) return;
        if (_entityState.Position.Equals(target)) return;

        Direction dir = Dir(_entityState.Position, target);
        if (dir == Direction.None) return;
        
        StageState.SetFacing(_entityState.Id, dir);
        // MoveEntity 대신 Position 직접 변경 (점유 시스템 우회)
        _entityState.Position = target;
        StageState.SetViewDirty();
    }

    private void RefreshDetection()
    {
        _currentVisiblePlayers.Clear();

        GridPos center = _entityState.Position;

        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                GridPos pos = new GridPos(center.X + dx, center.Y + dy);
                if (!StageState.IsInside(pos)) continue;

                CellData cell = StageState.GetCell(pos);
                if (cell.HasWall) continue;
                if (cell.HasBush) continue;
                if (!cell.IsOccupied) continue;

                if (!StageState.TryGetEntity(cell.OccupantId, out EntityState occ)) continue;
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

    private void SpawnChaser(int targetPlayerId)
    {
        if (_chaserDefinition == null) return;
        if (targetPlayerId == StageState.InvalidEntityId) return;

        Direction facing = _entityState.Facing;
        if (facing == Direction.None) facing = Direction.Down;

        GridPos frontPos = _entityState.Position.Move(facing);
        GridPos spawnPos = FindOpen(StageState, frontPos);
        
        int chaserId = StageState.SpawnEntity(_chaserDefinition, spawnPos, facing);
        ChaserTargetRegistry.Register(chaserId, targetPlayerId);
        
        StageState.SetViewDirty();
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
    private Direction Dir(GridPos from, GridPos to)
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
