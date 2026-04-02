using System;
using UnityEngine;
using MyGame2.Stage;

// 추적형 감시자
// 1. 소환 즉시 타겟 playerId 기반으로 실시간 추적
// 2. A*로 장애물 우회
// 3. 플레이어 닿으면 즉사
// 4. 함정 닿으면 사망
// 5. 플레이어가 부쉬에 숨으면 "어디갔지?" 문구 띄우고 3초 뒤 퇴장
// 6. 감시자는 부쉬 타일 진입 불가

public class ChaserEnemyMoveComponent : IComponentData, IUpdate, IDisposable
{
    public EntityFunctionSO Definition { get; }

    private readonly StageStateReferenceSO _stageStateRef;
    private readonly EntityState _entityState;
    private readonly FloatEventChannelSO _eventChannel;

    private EnemyAIState _currentState;
    private float _timer;
    private int _targetPlayerId;
    private bool _removed;
    private bool _targetResolved;

    private readonly float _moveInterval;
    private readonly float _lostSearchDuration;

    private const float SpawnDelay = 0.5f;
    private const float DespawnDelay = 0.5f;

    private static readonly GridPathfinder _pathfinder = new GridPathfinder();

    public ChaserEnemyMoveComponent(
        ChaserEnemyMove_Fn definition,
        StageStateReferenceSO stageStateRef,
        EntityState entityState,
        FloatEventChannelSO eventChannel)
    {
        Definition = definition;
        _stageStateRef = stageStateRef;
        _entityState = entityState;

        _moveInterval = definition.MoveInterval;
        _lostSearchDuration = definition.LostSearchDuration;

        _eventChannel = eventChannel;
        _eventChannel.OnEventRaised += OnUpdate;

        _currentState = EnemyAIState.Spawn;
        _timer = 0f;
        _targetPlayerId = StageState.InvalidEntityId;
        _removed = false;
        _targetResolved = false;
    }

    public void SetTargetPlayer(int playerId)
    {
        _targetPlayerId = playerId;
        _targetResolved = true;
    }

    public void OnUpdate(float dt)
    {
        if (_removed) return;

        StageState state = _stageStateRef.Instance;
        if (state == null) return;
        if (!_entityState.IsAlive)
        {
            DoRemove(state);
            return;
        }

        if (state.IsGameOver || state.IsStageClear) return;

        if (!_targetResolved)
        {
            ResolveTarget(state);
            _targetResolved = true;
        }

        switch (_currentState)
        {
            case EnemyAIState.Spawn:
                UpdateSpawn(state, dt);
                break;

            case EnemyAIState.Chase:
                UpdateChase(state, dt);
                break;

            case EnemyAIState.Lost:
                UpdateLost(state, dt);
                break;

            case EnemyAIState.Despawn:
                UpdateDespawn(state, dt);
                break;
        }

        if (_currentState == EnemyAIState.Chase)
        {
            _eventChannel.OnAlertAndChaseRaised(_targetPlayerId);
        }
    }

    private void ResolveTarget(StageState state)
    {
        if (_targetPlayerId != StageState.InvalidEntityId)
            return;

        int registeredTarget = ChaserTargetRegistry.GetTarget(_entityState.Id);
        if (registeredTarget != StageState.InvalidEntityId)
        {
            _targetPlayerId = registeredTarget;
            return;
        }

        _targetPlayerId = FindNearestPlayer(state);
    }

    // Spawn — 0.5초 등장 [5-6-1]

    private void UpdateSpawn(StageState state, float dt)
    {
        _timer += dt;

        if (_targetPlayerId != StageState.InvalidEntityId &&
            state.TryGetEntity(_targetPlayerId, out EntityState t) &&
            t.IsAlive &&
            state.HasBush(t.Position))
        {
            EnterLost(state);
            return;
        }

        if (_timer >= SpawnDelay)
        {
            _currentState = EnemyAIState.Chase;
            _timer = 0f;
        }
    }

    // Chase — 매 틱 타겟의 현재 위치로 A* 재계산

    private void UpdateChase(StageState state, float dt)
    {
        _timer += dt;
        if (_timer < _moveInterval) return;
        _timer -= _moveInterval;

        if (!state.TryGetEntity(_targetPlayerId, out EntityState target) || !target.IsAlive)
        {
            BeginDespawn(state, "타겟 사망");
            return;
        }

        // 부쉬 진입 -> Lost [5-6-3]
        if (state.HasBush(target.Position))
        {
            EnterLost(state);
            return;
        }

        GridPos? nextStep = _pathfinder.GetNextStep(
            state, _entityState.Position, target.Position, false);

        // 경로 없음 -> 소멸 [5-7-2]
        if (nextStep == null)
        {
            BeginDespawn(state, "경로 없음");
            return;
        }

        GridPos next = nextStep.Value;
        Direction moveDir = Dir(_entityState.Position, next);
        if (moveDir == Direction.None)
        {
            BeginDespawn(state, "방향 없음");
            return;
        }

        CellData nextCell = state.GetCell(next);

        // 부쉬 타일 진입 불가
        if (nextCell.HasBush)
            return;

        // 플레이어 접촉 -> 즉사
        if (nextCell.IsOccupied &&
            state.TryGetEntity(nextCell.OccupantId, out EntityState occ) &&
            occ.IsPlayer && occ.IsAlive)
        {
            state.SetFacing(_entityState.Id, moveDir);
            state.KillEntity(occ.Id);
            state.MarkGameOver();
            state.SetViewDirty();
            return;
        }

        // 벽/점유 -> 1턴 대기
        if (nextCell.HasWall || nextCell.IsOccupied)
            return;

        state.SetFacing(_entityState.Id, moveDir);
        state.MoveEntity(_entityState.Id, next);

        // 함정 [5-7-5]
        if (state.HasTrap(next))
        {
            BeginDespawn(state, "함정");
            return;
        }

        state.SetViewDirty();
    }

    // Lost 진입 — "어디갔지?" 이벤트 발행
    private void EnterLost(StageState state)
    {
        _currentState = EnemyAIState.Lost;
        _timer = 0f;

        state.Events?.RaiseEnemyWorldMessage(_entityState.Id, "어디갔지?", _lostSearchDuration);
    }

    private void UpdateLost(StageState state, float dt)
    {
        _timer += dt;

        // 부쉬 벗어남 -> 즉시 Chase [5-6-3-2]
        if (state.TryGetEntity(_targetPlayerId, out EntityState target) &&
            target.IsAlive &&
            !state.HasBush(target.Position))
        {
            _currentState = EnemyAIState.Chase;
            _timer = 0f;
            return;
        }

        // 시간 초과 -> 소멸 [5-6-3-3, 5-7-1]
        if (_timer >= _lostSearchDuration)
        {
            BeginDespawn(state, "부쉬 은신 유지");
        }
    }

    // Despawn — 퇴장 이벤트 발행 + 대기 후 제거

    private void BeginDespawn(StageState state, string reason)
    {
        Debug.Log($"[ChaserEnemy {_entityState.Id}] -> Despawn ({reason})");
        _currentState = EnemyAIState.Despawn;
        _timer = 0f;

        // 퇴장 모션 시작 이벤트
        state.Events?.RaiseEnemyDespawnStarted(_entityState.Id);
    }

    private void UpdateDespawn(StageState state, float dt)
    {
        _timer += dt;
        if (_timer >= DespawnDelay)
        {
            DoRemove(state);
        }
    }

    private void DoRemove(StageState state)
    {
        if (_removed) return;

        _removed = true;
        _eventChannel.OnEventRaised -= OnUpdate;
        state.RemoveEntity(_entityState.Id);
        state.SetViewDirty();
    }

    // 유틸리티

    private int FindNearestPlayer(StageState state)
    {
        GridPos myPos = _entityState.Position;
        int bestId = StageState.InvalidEntityId;
        int bestDist = int.MaxValue;

        for (int i = 0; i < state.PlayerIds.Count; i++)
        {
            int pid = state.PlayerIds[i];
            if (!state.TryGetEntity(pid, out EntityState p) || !p.IsAlive)
                continue;

            int d = Math.Abs(p.Position.X - myPos.X) + Math.Abs(p.Position.Y - myPos.Y);
            if (d < bestDist)
            {
                bestDist = d;
                bestId = pid;
            }
        }

        return bestId;
    }

    private static Direction Dir(GridPos from, GridPos to)
    {
        int dx = to.X - from.X;
        int dy = to.Y - from.Y;

        if (dx > 0) return Direction.Right;
        if (dx < 0) return Direction.Left;
        if (dy > 0) return Direction.Down;
        if (dy < 0) return Direction.Up;
        return Direction.None;
    }

    public void Dispose()
    {
        if (!_removed)
        {
            _eventChannel.OnEventRaised -= OnUpdate;
        }
    }
}