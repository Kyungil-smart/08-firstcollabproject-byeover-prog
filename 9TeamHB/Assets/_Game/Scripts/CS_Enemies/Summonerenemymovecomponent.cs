using System;
using UnityEngine;
using MyGame2.Stage;

public class SummonerEnemyMoveComponent : IComponentData, IUpdate, IDisposable
{
    public EntityFunctionSO Definition { get; }

    private readonly StageStateReferenceSO _stageStateRef;
    private readonly EntityState _entityState;
    private readonly FloatEventChannelSO _eventChannel;

    private EnemyAIState _currentState;
    private float _timer;
    private int _spawnedChaserId;
    private GridPos _chaserSpawnPos;

    private readonly float _moveInterval;
    private readonly float _alertDuration;
    private readonly int _patrolRadius;
    private readonly EntitySO _chaserDefinition;
    private readonly GridPos _spawnPosition;

    private static readonly DetectionArea3x3 _detector = new DetectionArea3x3();
    private static readonly MovementRule _movementRule = new MovementRule(new PushRule());

    public SummonerEnemyMoveComponent(
        SummonerEnemyMove_Fn definition, StageStateReferenceSO stageStateRef,
        EntityState entityState, FloatEventChannelSO eventChannel)
    {
        Definition = definition;
        _stageStateRef = stageStateRef;
        _entityState = entityState;
        _spawnPosition = entityState.Position; // 스폰 위치 기록

        _moveInterval = definition.MoveInterval;
        _alertDuration = definition.AlertDuration;
        _patrolRadius = definition.PatrolRadius;
        _chaserDefinition = definition.ChaserDefinition;

        _eventChannel = eventChannel;
        _eventChannel.OnEventRaised += OnUpdate;

        _currentState = EnemyAIState.Patrol;
        _timer = 0f;
        _spawnedChaserId = StageState.InvalidEntityId;
    }

    public void OnUpdate(float dt)
    {
        StageState state = _stageStateRef.Instance;
        if (state == null) return;
        if (!_entityState.IsAlive) return;
        if (state.IsGameOver || state.IsStageClear) return;

        switch (_currentState)
        {
            case EnemyAIState.Patrol:  UpdatePatrol(state, dt); break;
            case EnemyAIState.Alert:   UpdateAlert(state, dt); break;
            case EnemyAIState.Summon:  UpdateSummon(state, dt); break;
            case EnemyAIState.Frozen:  UpdateFrozen(state, dt); break;
        }
    }
    
    //  Patrol — 우측벽 따라가기 + 순찰 범위 제한
    
    private void UpdatePatrol(StageState state, float dt)
    {
        _timer += dt;

        if (_detector.TryDetect(state, _entityState.Position,
                _entityState.Facing, true, out int playerId))
        {
            if (state.TryGetEntity(playerId, out EntityState player))
                _chaserSpawnPos = player.Position;

            _currentState = EnemyAIState.Alert;
            _timer = 0f;
            return;
        }

        if (_timer < _moveInterval) return;
        _timer -= _moveInterval;

        DoPatrolMove(state);
    }

    // 우측벽 따라가기 (Right-Hand Rule) + 순찰 반경 제한
    // 이동 결과가 스폰 위치에서 patrolRadius를 초과하면 해당 방향 건너뜀
    
    private void DoPatrolMove(StageState state)
    {
        Direction facing = _entityState.Facing;
        if (facing == Direction.None) facing = Direction.Right;

        Direction right = facing.RotateClockwise();
        Direction left = facing.RotateClockwise().RotateClockwise().RotateClockwise();
        Direction back = facing.Opposite();

        Direction[] tryOrder = { right, facing, left, back };

        for (int i = 0; i < tryOrder.Length; i++)
        {
            Direction dir = tryOrder[i];

            // 이동 목표 칸이 순찰 범위 안인지 먼저 체크
            GridPos target = _entityState.Position.Move(dir);
            if (!IsWithinPatrolZone(target))
                continue;

            MoveResult result = _movementRule.TryMove(state, _entityState.Id, dir);

            if (result.Succeeded)
            {
                state.SetFacing(_entityState.Id, dir);
                state.MoveEntity(_entityState.Id, result.To);
                state.SetViewDirty();
                return;
            }
        }
        // 사방 다 범위 밖이거나 막힘
    }

    // 스폰 위치 기준 맨해튼 거리 체크
    private bool IsWithinPatrolZone(GridPos pos)
    {
        int dist = Math.Abs(pos.X - _spawnPosition.X) + Math.Abs(pos.Y - _spawnPosition.Y);
        return dist <= _patrolRadius;
    }
    
    //  Alert

    private void UpdateAlert(StageState state, float dt)
    {
        _timer += dt;

        if (_timer >= _alertDuration)
        {
            _currentState = EnemyAIState.Summon;
            _timer = 0f;
        }
    }
    
    private void UpdateSummon(StageState state, float dt)
    {
        if (_chaserDefinition == null)
        {
            Debug.LogError($"[SummonerEnemy {_entityState.Id}] ChaserDefinition이 null!");
            _currentState = EnemyAIState.Patrol;
            _timer = 0f;
            return;
        }

        GridPos spawnPos = FindSpawnPosition(state, _chaserSpawnPos);
        _spawnedChaserId = state.SpawnEntity(_chaserDefinition, spawnPos, Direction.None);

        if (state.TryGetEntity(_spawnedChaserId, out EntityState chaser))
        {
            var alertData = chaser.Get<EnemyAlertData>();
            if (alertData != null)
                alertData.OwnerSummonerId = _entityState.Id;
        }

        _currentState = EnemyAIState.Frozen;
        _timer = 0f;
        state.SetViewDirty();
    }

    private void UpdateFrozen(StageState state, float dt)
    {
        if (_spawnedChaserId == StageState.InvalidEntityId)
        {
            ReturnToPatrol();
            return;
        }

        if (!state.TryGetEntity(_spawnedChaserId, out EntityState chaser) ||
            !chaser.IsAlive)
        {
            ReturnToPatrol();
        }
    }
    
    //  유틸리티
    
    private void ReturnToPatrol()
    {
        _currentState = EnemyAIState.Patrol;
        _timer = 0f;
        _spawnedChaserId = StageState.InvalidEntityId;
    }

    private GridPos FindSpawnPosition(StageState state, GridPos target)
    {
        if (state.IsInside(target))
        {
            CellData cell = state.GetCell(target);
            if (!cell.HasWall && !cell.IsOccupied)
                return target;
        }

        Direction[] dirs = { Direction.Right, Direction.Down, Direction.Left, Direction.Up };
        for (int i = 0; i < dirs.Length; i++)
        {
            GridPos adj = target.Move(dirs[i]);
            if (!state.IsInside(adj)) continue;
            CellData adjCell = state.GetCell(adj);
            if (!adjCell.HasWall && !adjCell.IsOccupied)
                return adj;
        }

        return target;
    }

    public void NotifyChaserDespawned()
    {
        _spawnedChaserId = StageState.InvalidEntityId;
    }

    public void Dispose()
    {
        _eventChannel.OnEventRaised -= OnUpdate;
    }
}