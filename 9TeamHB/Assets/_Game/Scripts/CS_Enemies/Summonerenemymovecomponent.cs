using System;
using System.Collections.Generic;
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
    private int _detectedPlayerId;

    private readonly float _moveInterval;
    private readonly float _alertDuration;
    private readonly EntitySO _chaserDefinition;

    // 플레이어 1,2 위치 기반 사각형 순찰 경로
    private GridPos[] _patrolWaypoints;
    private int _patrolIndex;
    private bool _patrolReady;

    private static readonly DetectionArea3x3 _detector = new DetectionArea3x3();
    private static readonly MovementRule _movementRule = new MovementRule(new PushRule());
    private static readonly GridPathfinder _pathfinder = new GridPathfinder();

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
        _detectedPlayerId = StageState.InvalidEntityId;
        _patrolReady = false;
    }

    public void OnUpdate(float dt)
    {
        StageState state = _stageStateRef.Instance;
        if (state == null) return;
        if (!_entityState.IsAlive) return;
        if (state.IsGameOver || state.IsStageClear) return;

        if (!_patrolReady)
        {
            BuildPatrolFromPlayers(state);
            _patrolReady = true;
        }

        switch (_currentState)
        {
            case EnemyAIState.Patrol: UpdatePatrol(state, dt); break;
            case EnemyAIState.Alert:  UpdateAlert(state, dt); break;
        }
    }

    private void BuildPatrolFromPlayers(StageState state)
    {
        // Slot 1, 2로 정확히 구분
        int p1Id = state.GetPlayerIdBySlot(1);
        int p2Id = state.GetPlayerIdBySlot(2);

        // 어느 한쪽이 없거나 죽었으면 폴백
        if (p1Id == StageState.InvalidEntityId || p2Id == StageState.InvalidEntityId ||
            !state.TryGetEntity(p1Id, out EntityState p1Entity) || !p1Entity.IsAlive ||
            !state.TryGetEntity(p2Id, out EntityState p2Entity) || !p2Entity.IsAlive)
        {
            _patrolWaypoints = new GridPos[] { _entityState.Position };
            _patrolIndex = 0;
            return;
        }

        GridPos p1Pos = p1Entity.Position;
        GridPos p2Pos = p2Entity.Position;

        // 두 플레이어를 감싸는 사각형 (패딩 1칸)
        int minX = Math.Min(p1Pos.X, p2Pos.X) - 1;
        int maxX = Math.Max(p1Pos.X, p2Pos.X) + 1;
        int minY = Math.Min(p1Pos.Y, p2Pos.Y) - 1;
        int maxY = Math.Max(p1Pos.Y, p2Pos.Y) + 1;

        // 맵 범위 클램프
        minX = Math.Max(minX, 0);
        maxX = Math.Min(maxX, state.Width - 1);
        minY = Math.Max(minY, 0);
        maxY = Math.Min(maxY, state.Height - 1);

        // 사각형 둘레를 시계방향 웨이포인트로
        List<GridPos> wp = new List<GridPos>();
        
        for (int x = minX; x <= maxX; x++)
            TryAddWaypoint(state, wp, x, minY);
        for (int y = minY + 1; y <= maxY; y++)
            TryAddWaypoint(state, wp, maxX, y);
        for (int x = maxX - 1; x >= minX; x--)
            TryAddWaypoint(state, wp, x, maxY);
        for (int y = maxY - 1; y > minY; y--)
            TryAddWaypoint(state, wp, minX, y);

        _patrolWaypoints = wp.Count > 0
            ? wp.ToArray()
            : new GridPos[] { _entityState.Position };

        _patrolIndex = FindNearestWaypointIndex(_entityState.Position);

        Debug.Log($"[SummonerEnemy {_entityState.Id}] P1={p1Pos} P2={p2Pos} " +
                  $"순찰 사각형 ({minX},{minY})~({maxX},{maxY}), {_patrolWaypoints.Length}개");
    }

    private void TryAddWaypoint(StageState state, List<GridPos> list, int x, int y)
    {
        GridPos p = new GridPos(x, y);
        if (!state.IsInside(p)) return;
        if (state.GetCell(p).HasWall) return;
        list.Add(p);
    }

    private int FindNearestWaypointIndex(GridPos pos)
    {
        int best = 0, bestDist = int.MaxValue;
        for (int i = 0; i < _patrolWaypoints.Length; i++)
        {
            int d = Math.Abs(_patrolWaypoints[i].X - pos.X) +
                    Math.Abs(_patrolWaypoints[i].Y - pos.Y);
            if (d < bestDist) { bestDist = d; best = i; }
        }
        return best;
    }
    
    //  Patrol — 웨이포인트 따라 이동 + A* 활용

    private void UpdatePatrol(StageState state, float dt)
    {
        _timer += dt;

        if (_detector.TryDetect(state, _entityState.Position,
                _entityState.Facing, true, out int playerId))
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
        if (_patrolWaypoints == null || _patrolWaypoints.Length <= 1) return;

        GridPos target = _patrolWaypoints[_patrolIndex];

        // 도착했으면 다음으로
        if (_entityState.Position.Equals(target))
        {
            _patrolIndex = (_patrolIndex + 1) % _patrolWaypoints.Length;
            target = _patrolWaypoints[_patrolIndex];
        }

        // A*로 다음 웨이포인트까지 한 칸 이동
        GridPos? nextStep = _pathfinder.GetNextStep(
            state, _entityState.Position, target, true);

        if (nextStep == null)
        {
            // 경로 없음 → 다음 웨이포인트로 건너뜀
            _patrolIndex = (_patrolIndex + 1) % _patrolWaypoints.Length;
            return;
        }

        GridPos next = nextStep.Value;
        Direction dir = GetAdjacentDirection(_entityState.Position, next);
        if (dir == Direction.None) { _patrolIndex = (_patrolIndex + 1) % _patrolWaypoints.Length; return; }

        MoveResult result = _movementRule.TryMove(state, _entityState.Id, dir);

        if (result.Succeeded)
        {
            state.SetFacing(_entityState.Id, dir);
            state.MoveEntity(_entityState.Id, result.To);
            state.SetViewDirty();

            if (result.To.Equals(target))
                _patrolIndex = (_patrolIndex + 1) % _patrolWaypoints.Length;
        }
        else
        {
            _patrolIndex = (_patrolIndex + 1) % _patrolWaypoints.Length;
        }
    }
    
    //  Alert → 소환 → 순찰 복귀

    private void UpdateAlert(StageState state, float dt)
    {
        _timer += dt;

        if (_timer >= _alertDuration)
        {
            SpawnChaser(state);
            _currentState = EnemyAIState.Patrol;
            _timer = 0f;
            _detectedPlayerId = StageState.InvalidEntityId;
        }
    }

    private void SpawnChaser(StageState state)
    {
        if (_chaserDefinition == null)
        {
            Debug.LogError($"[SummonerEnemy {_entityState.Id}] ChaserDefinition null!");
            return;
        }

        Direction facing = _entityState.Facing;
        if (facing == Direction.None) facing = Direction.Down;

        GridPos frontPos = _entityState.Position.Move(facing);
        GridPos spawnPos = FindSpawnPosition(state, frontPos);

        int chaserId = state.SpawnEntity(_chaserDefinition, spawnPos, facing);

        if (state.TryGetEntity(chaserId, out EntityState chaser))
        {
            foreach (var comp in chaser.Components)
            {
                if (comp is ChaserEnemyMoveComponent chaserComp)
                {
                    chaserComp.SetTargetPlayer(_detectedPlayerId);
                    break;
                }
            }
        }

        state.SetViewDirty();
    }

    private GridPos FindSpawnPosition(StageState state, GridPos target)
    {
        if (state.IsInside(target))
        {
            CellData cell = state.GetCell(target);
            if (!cell.HasWall && !cell.IsOccupied) return target;
        }

        Direction[] dirs = { Direction.Right, Direction.Down, Direction.Left, Direction.Up };
        for (int i = 0; i < dirs.Length; i++)
        {
            GridPos adj = _entityState.Position.Move(dirs[i]);
            if (!state.IsInside(adj)) continue;
            CellData c = state.GetCell(adj);
            if (!c.HasWall && !c.IsOccupied) return adj;
        }
        return target;
    }

    private static Direction GetAdjacentDirection(GridPos from, GridPos to)
    {
        int dx = to.X - from.X;
        int dy = to.Y - from.Y;
        if (dx == 1 && dy == 0) return Direction.Right;
        if (dx == -1 && dy == 0) return Direction.Left;
        if (dx == 0 && dy == 1) return Direction.Down;
        if (dx == 0 && dy == -1) return Direction.Up;
        if (Math.Abs(dx) >= Math.Abs(dy))
            return dx > 0 ? Direction.Right : Direction.Left;
        return dy > 0 ? Direction.Down : Direction.Up;
    }

    public void Dispose()
    {
        _eventChannel.OnEventRaised -= OnUpdate;
    }
}