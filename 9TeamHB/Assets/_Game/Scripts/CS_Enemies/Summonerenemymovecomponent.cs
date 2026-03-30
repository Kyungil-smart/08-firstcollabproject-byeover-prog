using System;
using UnityEngine;
using MyGame2.Stage;

public class SummonerEnemyMoveComponent : IComponentData, IUpdate, IDisposable
{
    public EntityFunctionSO Definition { get; }

    // 핵심 수정: StageState를 직접 캐싱하지 않고 SO 참조를 보유
    // 엔티티 생성 시점에는 Instance가 null이므로, 매 프레임 .Instance로 접근
    private readonly StageStateReferenceSO _stageStateRef;
    private readonly EntityState _entityState;
    private readonly FloatEventChannelSO _eventChannel;

    private EnemyAIState _currentState;
    private float _timer;
    private int _spawnedChaserId;
    private GridPos _chaserSpawnPos;

    private readonly float _moveInterval;
    private readonly float _alertDuration;
    private readonly EntitySO _chaserDefinition;

    private static readonly DetectionArea3x3 _detector = new DetectionArea3x3();
    private static readonly MovementRule _movementRule = new MovementRule(new PushRule());

    private static readonly Direction[] PatrolPriority =
    {
        Direction.Right, Direction.Down, Direction.Left, Direction.Up
    };

    public SummonerEnemyMoveComponent(
        SummonerEnemyMove_Fn definition, StageStateReferenceSO stageStateRef,
        EntityState entityState, FloatEventChannelSO eventChannel)
    {
        Definition = definition;
        _stageStateRef = stageStateRef;
        _entityState = entityState;

        _moveInterval = definition.MoveInterval;
        _alertDuration = definition.AlertDuration;
        _chaserDefinition = definition.ChaserDefinition;

        _eventChannel = eventChannel;
        _eventChannel.OnEventRaised += OnUpdate;

        _currentState = EnemyAIState.Patrol;
        _timer = 0f;
        _spawnedChaserId = StageState.InvalidEntityId;
    }

    public void OnUpdate(float dt)
    {
        // 매 프레임 Instance 접근 (생성 시점에는 null이었을 수 있음)
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
            Debug.Log($"[SummonerEnemy {_entityState.Id}] 감지! → Alert");
            return;
        }

        if (_timer < _moveInterval) return;
        _timer -= _moveInterval;

        DoPatrolMove(state);
    }

    private void DoPatrolMove(StageState state)
    {
        for (int i = 0; i < PatrolPriority.Length; i++)
        {
            Direction dir = PatrolPriority[i];
            MoveResult result = _movementRule.TryMove(state, _entityState.Id, dir);

            if (result.Succeeded)
            {
                state.SetFacing(_entityState.Id, dir);
                state.MoveEntity(_entityState.Id, result.To);
                state.SetViewDirty();
                return;
            }
        }
    }

    private void UpdateAlert(StageState state, float dt)
    {
        _timer += dt;

        if (_timer >= _alertDuration)
        {
            _currentState = EnemyAIState.Summon;
            _timer = 0f;
            Debug.Log($"[SummonerEnemy {_entityState.Id}] Alert 종료 → Summon");
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
        Debug.Log($"[SummonerEnemy {_entityState.Id}] 추격자 {_spawnedChaserId} 소환 → Frozen");
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

    private void ReturnToPatrol()
    {
        _currentState = EnemyAIState.Patrol;
        _timer = 0f;
        _spawnedChaserId = StageState.InvalidEntityId;
        Debug.Log($"[SummonerEnemy {_entityState.Id}] 추격자 소멸 → Patrol 복귀");
    }

    private GridPos FindSpawnPosition(StageState state, GridPos target)
    {
        if (state.IsInside(target))
        {
            CellData cell = state.GetCell(target);
            if (!cell.HasWall && !cell.IsOccupied)
                return target;
        }

        for (int i = 0; i < PatrolPriority.Length; i++)
        {
            GridPos adj = target.Move(PatrolPriority[i]);
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