using System;
using System.Collections.Generic;
using UnityEngine;
using MyGame2.Stage;

public class AnimalEnemyMoveComponent : IComponentData, IUpdate, IDisposable
{
    public EntityFunctionSO Definition { get; }

    private readonly StageStateReferenceSO _stageStateRef;
    private readonly EntityState _entityState;
    private readonly FloatEventChannelSO _eventChannel;

    private EnemyAIState _currentState;
    private float _timer;
    private int _detectedPlayerId;
    private GridPos _lastKnownPlayerPos;

    private readonly float _moveInterval;
    private readonly float _alertDuration;
    private readonly float _chaseSpeedMultiplier;
    private readonly float _lostSearchDuration;

    private static readonly DetectionArea3x3 _detector = new DetectionArea3x3();
    private static readonly GridPathfinder _pathfinder = new GridPathfinder();
    private static readonly MovementRule _movementRule = new MovementRule(new PushRule());
    private static readonly DeathRule _deathRule = new DeathRule();

    private static readonly Direction[] PatrolPriority =
    {
        Direction.Right, Direction.Down, Direction.Left, Direction.Up
    };

    public AnimalEnemyMoveComponent(
        AnimalEnemyMove_Fn definition, StageStateReferenceSO stageStateRef,
        EntityState entityState, FloatEventChannelSO eventChannel)
    {
        Definition = definition;
        _stageStateRef = stageStateRef;
        _entityState = entityState;

        _moveInterval = definition.MoveInterval;
        _alertDuration = definition.AlertDuration;
        _chaseSpeedMultiplier = definition.ChaseSpeedMultiplier;
        _lostSearchDuration = definition.LostSearchDuration;

        _eventChannel = eventChannel;
        _eventChannel.OnEventRaised += OnUpdate;

        _currentState = EnemyAIState.Patrol;
        _timer = 0f;
        _detectedPlayerId = StageState.InvalidEntityId;
    }

    public void OnUpdate(float dt)
    {
        StageState state = _stageStateRef.Instance;
        if (state == null) return;
        if (!_entityState.IsAlive) return;
        if (state.IsGameOver || state.IsStageClear) return;

        switch (_currentState)
        {
            case EnemyAIState.Patrol:       UpdatePatrol(state, dt); break;
            case EnemyAIState.Alert:        UpdateAlert(state, dt); break;
            case EnemyAIState.Chase:        UpdateChase(state, dt); break;
            case EnemyAIState.Lost:         UpdateLost(state, dt); break;
            case EnemyAIState.ReturnToZone: UpdateReturnToZone(state, dt); break;
        }

        if (_currentState == EnemyAIState.Alert || _currentState == EnemyAIState.Chase)
        {
            _eventChannel.OnAlertAndChaseRaised();
        }
    }

    private void UpdatePatrol(StageState state, float dt)
    {
        _timer += dt;

        if (_detector.TryDetect(state, _entityState.Position,
                _entityState.Facing, false, out int playerId))
        {
            _detectedPlayerId = playerId;
            _currentState = EnemyAIState.Alert;
            _timer = 0f;
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

            if (result.IsContactKill)
            {
                state.SetFacing(_entityState.Id, dir);
                _deathRule.ApplyContactKill(state, result);
                state.SetViewDirty();
                return;
            }
        }
    }

    private void UpdateAlert(StageState state, float dt)
    {
        _timer += dt;

        if (!_detector.TryDetect(state, _entityState.Position,
                _entityState.Facing, false, out int _))
        {
            _currentState = EnemyAIState.Patrol;
            _timer = 0f;
            _detectedPlayerId = StageState.InvalidEntityId;
            return;
        }

        if (_timer >= _alertDuration)
        {
            _currentState = EnemyAIState.Chase;
            _timer = 0f;

            if (state.TryGetEntity(_detectedPlayerId, out EntityState player))
                _lastKnownPlayerPos = player.Position;
        }
    }

    private void UpdateChase(StageState state, float dt)
    {
        _timer += dt;

        float chaseInterval = _moveInterval / _chaseSpeedMultiplier;
        if (_timer < chaseInterval) return;
        _timer -= chaseInterval;

        if (!state.TryGetEntity(_detectedPlayerId, out EntityState target) || !target.IsAlive)
        {
            TransitionToReturnToZone();
            return;
        }

        if (state.HasBush(target.Position))
        {
            _lastKnownPlayerPos = target.Position;
            _currentState = EnemyAIState.Lost;
            _timer = 0f;
            return;
        }

        _lastKnownPlayerPos = target.Position;
        GridPos? nextStep = _pathfinder.GetNextStep(
            state, _entityState.Position, target.Position, false);

        if (nextStep == null)
        {
            TransitionToReturnToZone();
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
        MoveResult moveResult = _movementRule.TryMove(state, _entityState.Id, moveDir);

        if (moveResult.Succeeded)
        {
            state.SetFacing(_entityState.Id, moveDir);
            state.MoveEntity(_entityState.Id, moveResult.To);
            state.SetViewDirty();
        }
        else if (moveResult.IsContactKill)
        {
            state.SetFacing(_entityState.Id, moveDir);
            _deathRule.ApplyContactKill(state, moveResult);
            state.SetViewDirty();
        }
    }

    private void UpdateLost(StageState state, float dt)
    {
        _timer += dt;

        if (state.TryGetEntity(_detectedPlayerId, out EntityState target) &&
            target.IsAlive && !state.HasBush(target.Position))
        {
            _currentState = EnemyAIState.Chase;
            _timer = 0f;
            return;
        }

        if (_timer >= _lostSearchDuration)
        {
            TransitionToReturnToZone();
        }
    }

    private void UpdateReturnToZone(StageState state, float dt)
    {
        _timer += dt;
        if (_timer < _moveInterval) return;
        _timer -= _moveInterval;

        if (_entityState.Position.Equals(_entityState.SpawnPosition))
        {
            _currentState = EnemyAIState.Patrol;
            _timer = 0f;
            _detectedPlayerId = StageState.InvalidEntityId;
            return;
        }

        GridPos? nextStep = _pathfinder.GetNextStep(
            state, _entityState.Position, _entityState.SpawnPosition, false);

        if (nextStep == null)
        {
            _currentState = EnemyAIState.Patrol;
            _timer = 0f;
            _detectedPlayerId = StageState.InvalidEntityId;
            return;
        }

        GridPos next = nextStep.Value;
        Direction moveDir = GetDirectionTo(_entityState.Position, next);
        MoveResult result = _movementRule.TryMove(state, _entityState.Id, moveDir);

        if (result.Succeeded)
        {
            state.SetFacing(_entityState.Id, moveDir);
            state.MoveEntity(_entityState.Id, result.To);
            state.SetViewDirty();

            if (_detector.TryDetect(state, _entityState.Position,
                    _entityState.Facing, false, out int playerId))
            {
                _detectedPlayerId = playerId;
                _currentState = EnemyAIState.Alert;
                _timer = 0f;
            }
        }
        else if (result.IsContactKill)
        {
            state.SetFacing(_entityState.Id, moveDir);
            _deathRule.ApplyContactKill(state, result);
            state.SetViewDirty();
        }
    }

    private void TransitionToReturnToZone()
    {
        _currentState = EnemyAIState.ReturnToZone;
        _timer = 0f;
        _detectedPlayerId = StageState.InvalidEntityId;
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