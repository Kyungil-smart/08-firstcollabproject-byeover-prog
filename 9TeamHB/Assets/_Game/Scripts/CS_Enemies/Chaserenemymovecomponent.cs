using System;
using UnityEngine;
using MyGame2.Stage;

public class ChaserEnemyMoveComponent : IComponentData, IUpdate, IDisposable
{
    public EntityFunctionSO Definition { get; }

    private readonly StageStateReferenceSO _stageStateRef;
    private readonly EntityState _entityState;
    private readonly FloatEventChannelSO _eventChannel;

    private EnemyAIState _currentState;
    private float _timer;
    private int _targetPlayerId;

    private readonly float _moveInterval;
    private readonly float _lostSearchDuration;

    private static readonly GridPathfinder _pathfinder = new GridPathfinder();
    private static readonly MovementRule _movementRule = new MovementRule(new PushRule());
    private static readonly DeathRule _deathRule = new DeathRule();

    public ChaserEnemyMoveComponent(
        ChaserEnemyMove_Fn definition, StageStateReferenceSO stageStateRef,
        EntityState entityState, FloatEventChannelSO eventChannel)
    {
        Definition = definition;
        _stageStateRef = stageStateRef;
        _entityState = entityState;

        _moveInterval = definition.MoveInterval;
        _lostSearchDuration = definition.LostSearchDuration;

        _eventChannel = eventChannel;
        _eventChannel.OnEventRaised += OnUpdate;

        _currentState = EnemyAIState.Chase;
        _timer = 0f;
        _targetPlayerId = StageState.InvalidEntityId;
    }

    // B감시자가 소환 직후 호출 — 감지된 플레이어를 타겟으로 지정
    public void SetTargetPlayer(int playerId)
    {
        _targetPlayerId = playerId;
    }

    public void OnUpdate(float dt)
    {
        StageState state = _stageStateRef.Instance;
        if (state == null) return;
        if (!_entityState.IsAlive) return;
        if (state.IsGameOver || state.IsStageClear) return;

        // 타겟 미설정 시 가장 가까운 플레이어 (폴백)
        if (_targetPlayerId == StageState.InvalidEntityId)
            _targetPlayerId = FindNearestPlayer(state);

        // 소환 시 플레이어가 부쉬 → 즉시 소멸
        if (_targetPlayerId != StageState.InvalidEntityId &&
            _currentState == EnemyAIState.Chase && _timer == 0f)
        {
            if (state.TryGetEntity(_targetPlayerId, out EntityState t) &&
                state.HasBush(t.Position))
            {
                ExecuteDespawn(state, "소환 시 부쉬");
                return;
            }
        }

        switch (_currentState)
        {
            case EnemyAIState.Chase:   UpdateChase(state, dt); break;
            case EnemyAIState.Lost:    UpdateLost(state, dt); break;
            case EnemyAIState.Despawn: ExecuteDespawn(state, "소멸"); break;
        }
    }

    private void UpdateChase(StageState state, float dt)
    {
        _timer += dt;
        if (_timer < _moveInterval) return;
        _timer -= _moveInterval;

        // 타겟 생존 확인
        if (!state.TryGetEntity(_targetPlayerId, out EntityState target) || !target.IsAlive)
        {
            _currentState = EnemyAIState.Despawn;
            return;
        }

        // 부쉬 체크
        if (state.HasBush(target.Position))
        {
            _currentState = EnemyAIState.Lost;
            _timer = 0f;
            return;
        }

        // A* 다음 칸
        GridPos? nextStep = _pathfinder.GetNextStep(
            state, _entityState.Position, target.Position, false);

        if (nextStep == null)
        {
            _currentState = EnemyAIState.Despawn;
            return;
        }

        GridPos next = nextStep.Value;

        // 접촉 즉사
        CellData nextCell = state.GetCell(next);
        if (nextCell.IsOccupied &&
            state.TryGetEntity(nextCell.OccupantId, out EntityState occ) &&
            occ.IsPlayer && occ.IsAlive)
        {
            Direction killDir = GetDirectionTo(_entityState.Position, next);
            state.SetFacing(_entityState.Id, killDir);
            state.KillEntity(occ.Id);
            state.MarkGameOver();
            state.SetViewDirty();
            return;
        }

        // 이동
        Direction moveDir = GetDirectionTo(_entityState.Position, next);
        MoveResult result = _movementRule.TryMove(state, _entityState.Id, moveDir);

        if (result.Succeeded)
        {
            state.SetFacing(_entityState.Id, moveDir);
            state.MoveEntity(_entityState.Id, result.To);

            if (state.HasTrap(result.To))
            {
                ExecuteDespawn(state, "함정");
                return;
            }

            state.SetViewDirty();
        }
        else if (result.IsContactKill)
        {
            state.SetFacing(_entityState.Id, moveDir);
            _deathRule.ApplyContactKill(state, result);
            state.SetViewDirty();
        }
        else
        {
            _currentState = EnemyAIState.Despawn;
        }
    }

    private void UpdateLost(StageState state, float dt)
    {
        _timer += dt;

        if (state.TryGetEntity(_targetPlayerId, out EntityState target) &&
            target.IsAlive && !state.HasBush(target.Position))
        {
            _currentState = EnemyAIState.Chase;
            _timer = 0f;
            return;
        }

        if (_timer >= _lostSearchDuration)
            _currentState = EnemyAIState.Despawn;
    }

    private void ExecuteDespawn(StageState state, string reason)
    {
        Debug.Log($"[ChaserEnemy {_entityState.Id}] 소멸 ({reason})");
        _eventChannel.OnEventRaised -= OnUpdate;
        state.RemoveEntity(_entityState.Id);
        state.SetViewDirty();
    }

    private int FindNearestPlayer(StageState state)
    {
        GridPos myPos = _entityState.Position;
        int bestId = StageState.InvalidEntityId;
        int bestDist = int.MaxValue;
        for (int i = 0; i < state.PlayerIds.Count; i++)
        {
            int pid = state.PlayerIds[i];
            if (!state.TryGetEntity(pid, out EntityState p) || !p.IsAlive) continue;
            int d = Math.Abs(p.Position.X - myPos.X) + Math.Abs(p.Position.Y - myPos.Y);
            if (d < bestDist) { bestDist = d; bestId = pid; }
        }
        return bestId;
    }

    private static Direction GetDirectionTo(GridPos from, GridPos to)
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
        _eventChannel.OnEventRaised -= OnUpdate;
    }
}