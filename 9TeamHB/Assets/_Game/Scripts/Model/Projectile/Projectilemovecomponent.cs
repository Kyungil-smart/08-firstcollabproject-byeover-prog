using System;
using UnityEngine;
using MyGame2.Stage;

// 투사체 이동 컴포넌트
// facing 방향으로 매 틱 1칸씩 이동
// 벽 -> 소멸
// 플레이어 -> 즉사 + 소멸
// 상자 -> 소멸 (부서지는 상자면 상자도 파괴)
// 추적형 감시자 -> 감시자 소멸 + 투사체 소멸 [5-7-4]
// 맵 밖 -> 소멸

public class ProjectileMoveComponent : IComponentData, IUpdate, IDisposable
{
    public EntityFunctionSO Definition { get; }

    private readonly StageStateReferenceSO _stageStateRef;
    private readonly EntityState _entityState;
    private readonly FloatEventChannelSO _eventChannel;

    private float _moveInterval;
    private float _timer;
    private bool _removed;
    private bool _speedResolved;

    public ProjectileMoveComponent(
        ProjectileMove_Fn definition,
        StageStateReferenceSO stageStateRef,
        EntityState entityState,
        FloatEventChannelSO eventChannel)
    {
        Definition = definition;
        _stageStateRef = stageStateRef;
        _entityState = entityState;

        _moveInterval = definition.DefaultMoveInterval;

        _eventChannel = eventChannel;
        _eventChannel.OnEventRaised += OnUpdate;

        _timer = 0f;
        _removed = false;
        _speedResolved = false;
    }

    public void OnUpdate(float dt)
    {
        if (_removed) return;

        StageState state = _stageStateRef.Instance;
        if (state == null) return;
        if (!_entityState.IsAlive) { DoRemove(state); return; }

        // 최초 1회: 발사기가 등록한 속도 읽기
        if (!_speedResolved)
        {
            _moveInterval = ProjectileSpeedRegistry.GetSpeed(_entityState.Id, _moveInterval);
            _speedResolved = true;
        }

        _timer += dt;
        if (_timer < _moveInterval) return;
        _timer -= _moveInterval;

        MoveAndCheck(state);
    }

    private void MoveAndCheck(StageState state)
    {
        Direction dir = _entityState.Facing;
        if (dir == Direction.None) { DoRemove(state); return; }

        GridPos next = _entityState.Position.Move(dir);

        // 맵 밖 -> 소멸
        if (!state.IsInside(next))
        {
            DoRemove(state);
            return;
        }

        CellData nextCell = state.GetCell(next);

        // 벽 -> 소멸
        if (nextCell.HasWall)
        {
            DoRemove(state);
            return;
        }

        // 점유 엔티티 충돌 판정
        if (nextCell.IsOccupied &&
            state.TryGetEntity(nextCell.OccupantId, out EntityState occupant))
        {
            // 플레이어 -> 즉사
            if (occupant.IsPlayer && occupant.IsAlive)
            {
                state.KillEntity(occupant.Id);
                state.MarkGameOver();
                DoRemove(state);
                return;
            }

            // 추적형 감시자 -> 감시자 소멸 [5-7-4]
            if (occupant.IsChaserEnemy && occupant.IsAlive)
            {
                state.RemoveEntity(occupant.Id);
                DoRemove(state);
                return;
            }

            // 상자 -> 투사체 소멸
            // (부서지는 상자 구현되면 여기서 상자 파괴 로직 추가)
            if (occupant.IsBox)
            {
                // TODO: 부서지는 상자면 state.RemoveEntity(occupant.Id)
                DoRemove(state);
                return;
            }

            // 기타 엔티티 (발사기 등) -> 소멸
            if (occupant.IsBlocking)
            {
                DoRemove(state);
                return;
            }
        }

        // 이동 (투사체는 isBlocking=false이므로 점유 시스템 사용 안 함)
        _entityState.Position = next;
        state.SetViewDirty();
    }

    private void DoRemove(StageState state)
    {
        if (_removed) return;
        _removed = true;
        _eventChannel.OnEventRaised -= OnUpdate;
        state.RemoveEntity(_entityState.Id);
        state.SetViewDirty();
    }

    public void Dispose()
    {
        if (!_removed)
            _eventChannel.OnEventRaised -= OnUpdate;
    }
}