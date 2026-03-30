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
    private bool _pendingDespawn;

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

        // 소환 직후
        _currentState = EnemyAIState.Chase;
        _timer = 0f;
        _targetPlayerId = StageState.InvalidEntityId;
        _pendingDespawn = false;
    }

    public void OnUpdate(float dt)
    {
        StageState state = _stageStateRef.Instance;
        if (state == null) return;
        if (!_entityState.IsAlive) return;
        if (state.IsGameOver || state.IsStageClear) return;

        // 최초 프레임: 대상 설정 + 부쉬 체크
        if (_targetPlayerId == StageState.InvalidEntityId)
        {
            _targetPlayerId = FindNearestPlayer(state);
            if (_targetPlayerId != StageState.InvalidEntityId &&
                state.TryGetEntity(_targetPlayerId, out EntityState t) &&
                state.HasBush(t.Position))
            {
                _pendingDespawn = true;
                Debug.Log($"[ChaserEnemy {_entityState.Id}] 소환 시 플레이어 부쉬 → 즉시 소멸");
            }
        }

        if (_pendingDespawn)
        {
            ExecuteDespawn(state);
            return;
        }

        switch (_currentState)
        {
            case EnemyAIState.Chase:   UpdateChase(state, dt); break;
            case EnemyAIState.Lost:    UpdateLost(state, dt); break;
            case EnemyAIState.Despawn: ExecuteDespawn(state); break;
        }
    }

    private void UpdateChase(StageState state, float dt)
    {
        _timer += dt;
        if (_timer < _moveInterval) return;
        _timer -= _moveInterval;

        if (!state.TryGetEntity(_targetPlayerId, out EntityState target) || !target.IsAlive)
        {
            _targetPlayerId = FindNearestPlayer(state);
            if (_targetPlayerId == StageState.InvalidEntityId)
            {
                TransitionToDespawn("대상 없음");
                return;
            }
            state.TryGetEntity(_targetPlayerId, out target);
        }

        if (state.HasBush(target.Position))
        {
            _currentState = EnemyAIState.Lost;
            _timer = 0f;
            Debug.Log($"[ChaserEnemy {_entityState.Id}] 플레이어 부쉬 → Lost");
            return;
        }

        GridPos? nextStep = _pathfinder.GetNextStep(
            state, _entityState.Position, target.Position, false);

        if (nextStep == null)
        {
            TransitionToDespawn("경로 없음");
            return;
        }

        GridPos next = nextStep.Value;

        CellData nextCell = state.GetCell(next);
        if (nextCell.IsOccupied &&
            state.TryGetEntity(nextCell.OccupantId, out EntityState occupant) &&
            occupant.IsPlayer && occupant.IsAlive)
        {
            Direction killDir = GetDirectionTo(_entityState.Position, next);
            state.SetFacing(_entityState.Id, killDir);
            state.KillEntity(occupant.Id);
            state.MarkGameOver();
            state.SetViewDirty();
            return;
        }

        Direction moveDir = GetDirectionTo(_entityState.Position, next);
        MoveResult result = _movementRule.TryMove(state, _entityState.Id, moveDir);

        if (result.Succeeded)
        {
            state.SetFacing(_entityState.Id, moveDir);
            state.MoveEntity(_entityState.Id, result.To);

            if (state.HasTrap(result.To))
            {
                TransitionToDespawn("함정타일 밟음");
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
            TransitionToDespawn("이동 불가");
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
            Debug.Log($"[ChaserEnemy {_entityState.Id}] 부쉬 탈출 → Chase 재개");
            return;
        }

        if (_timer >= _lostSearchDuration)
        {
            TransitionToDespawn("부쉬 은신 유지");
        }
    }

    private void ExecuteDespawn(StageState state)
    {
        Debug.Log($"[ChaserEnemy {_entityState.Id}] 소멸 처리");

        // 소환자에게 통보
        var alertData = _entityState.Get<EnemyAlertData>();
        if (alertData != null && alertData.OwnerSummonerId != StageState.InvalidEntityId)
        {
            if (state.TryGetEntity(alertData.OwnerSummonerId, out EntityState summoner))
            {
                foreach (var comp in summoner.Components)
                {
                    if (comp is SummonerEnemyMoveComponent summonerComp)
                    {
                        summonerComp.NotifyChaserDespawned();
                        break;
                    }
                }
            }
        }

        _eventChannel.OnEventRaised -= OnUpdate;
        state.RemoveEntity(_entityState.Id);
        state.SetViewDirty();
    }

    private void TransitionToDespawn(string reason)
    {
        _currentState = EnemyAIState.Despawn;
        _timer = 0f;
        Debug.Log($"[ChaserEnemy {_entityState.Id}] → Despawn ({reason})");
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