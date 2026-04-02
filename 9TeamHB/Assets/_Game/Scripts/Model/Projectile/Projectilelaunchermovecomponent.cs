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
        CellData spawnCell = state.GetCell(spawnPos);
        if (spawnCell.HasWall) return;

        // 수정: 앞 칸에 상자/엔티티가 이미 있으면 발사 안 함
        if (spawnCell.IsOccupied)
        {
            // 플레이어가 서 있으면 즉사 처리만 하고 투사체는 안 만듦
            if (state.TryGetEntity(spawnCell.OccupantId, out EntityState occupant))
            {
                if (occupant.IsPlayer && occupant.IsAlive)
                {
                    state.KillEntity(occupant.Id);
                    state.MarkGameOver();
                    state.SetViewDirty();
                }
            }
            // 상자/다른 엔티티가 있으면 발사 자체를 하지 않음
            return;
        }

        // 투사체 소환
        int projectileId = state.SpawnEntity(_projectileDefinition, spawnPos, facing);

        // 투사체에 이동 속도 전달
        ProjectileSpeedRegistry.Register(projectileId, _projectileSpeed);

        state.SetViewDirty();
    }

    public void Dispose()
    {
        _eventChannel.OnEventRaised -= OnUpdate;
    }
}