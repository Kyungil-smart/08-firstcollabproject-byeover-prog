using System;
using UnityEngine;
using MyGame2.Stage;

// 투사체 발사기 컴포넌트
// 주기적으로 facing 방향에 투사체 엔티티를 소환
// 발사기 자체는 벽이라 이동 안 함

public class ProjectileLauncherMoveComponent : IComponentData, IUpdate, IDisposable
{
    public EntityFunctionSO Definition { get; }

    private readonly StageStateReferenceSO _stageStateRef;
    private readonly EntityState _entityState;
    private readonly FloatEventChannelSO _eventChannel;

    private readonly float _fireInterval;
    private readonly float _projectileSpeed;
    private readonly EntitySO _projectileDefinition;
    private float _timer;

    public ProjectileLauncherMoveComponent(
        ProjectileLauncherMove_Fn definition,
        StageStateReferenceSO stageStateRef,
        EntityState entityState,
        FloatEventChannelSO eventChannel)
    {
        Definition = definition;
        _stageStateRef = stageStateRef;
        _entityState = entityState;

        _fireInterval = definition.FireInterval;
        _projectileSpeed = definition.ProjectileSpeed;
        _projectileDefinition = definition.ProjectileDefinition;

        _eventChannel = eventChannel;
        _eventChannel.OnEventRaised += OnUpdate;

        _timer = 0f;
    }

    public void OnUpdate(float dt)
    {
        StageState state = _stageStateRef.Instance;
        if (state == null) return;
        if (!_entityState.IsAlive) return;
        if (state.IsGameOver || state.IsStageClear) return;

        _timer += dt;
        if (_timer < _fireInterval) return;
        _timer -= _fireInterval;

        FireProjectile(state);
    }

    private void FireProjectile(StageState state)
    {
        if (_projectileDefinition == null) return;

        Direction facing = _entityState.Facing;
        if (facing == Direction.None) return;

        // 발사 위치: 발사기 바로 앞 1칸
        GridPos spawnPos = _entityState.Position.Move(facing);

        // 앞 칸이 맵 밖이거나 벽이면 발사 안 함
        if (!state.IsInside(spawnPos)) return;
        if (state.GetCell(spawnPos).HasWall) return;

        // 투사체 소환
        int projectileId = state.SpawnEntity(_projectileDefinition, spawnPos, facing);

        // 투사체에 이동 속도 전달
        ProjectileSpeedRegistry.Register(projectileId, _projectileSpeed);

        // 소환 즉시 해당 칸의 충돌 판정
        CheckImmediateCollision(state, spawnPos, projectileId);

        state.SetViewDirty();
    }

    // 소환 위치에 이미 플레이어/상자가 있으면 즉시 처리
    private void CheckImmediateCollision(StageState state, GridPos pos, int projectileId)
    {
        CellData cell = state.GetCell(pos);
        if (!cell.IsOccupied) return;

        if (!state.TryGetEntity(cell.OccupantId, out EntityState occupant)) return;

        if (occupant.IsPlayer && occupant.IsAlive)
        {
            state.KillEntity(occupant.Id);
            state.MarkGameOver();
            state.RemoveEntity(projectileId);
        }
        else if (occupant.IsBox)
        {
            // 상자에 닿으면 투사체 소멸 (부서지는 상자면 상자도 파괴)
            state.RemoveEntity(projectileId);
        }
    }

    public void Dispose()
    {
        _eventChannel.OnEventRaised -= OnUpdate;
    }
}