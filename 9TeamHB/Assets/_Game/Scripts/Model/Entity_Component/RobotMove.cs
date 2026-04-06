using System;
using UnityEngine;
using MyGame2.Stage;

public class RobotMove : IUpdate, IDisposable, IComponentData
{
    private RobotEnemy _robotEnemy;
    private MovementRule _movementRule;
    private DeathRule _deathRule;
    private StageState StageState{get{ return _stageStateRef.Instance;}}
    
    private EnemyAIState _currentState;
    private float _timer;

    private readonly float _moveInterval;
    private readonly float _alertDuration;
    private readonly float _chaseSpeedMultiplier;
    
    private readonly StageStateReferenceSO _stageStateRef;
    private readonly EntityState _entityState;
    private readonly FloatEventChannelSO _eventChannel;
    
    private int _alertTargetPlayerId;

    public RobotMove(
        EntityState entityState, 
        StageStateReferenceSO stageStateRef, 
        FloatEventChannelSO eventChannel,
        float moveInterval, float alertDuration, float chaseSpeedMultiplier)
    {
        _stageStateRef = stageStateRef;
        _entityState = entityState;
        _eventChannel = eventChannel;
        _eventChannel.OnEventRaised += OnUpdate;
        
        _moveInterval = moveInterval;
        _alertDuration = alertDuration;
        _chaseSpeedMultiplier = chaseSpeedMultiplier;

        _currentState = EnemyAIState.Patrol;
        _timer = 0f;
    }

    
    public void OnUpdate(float dt)
    {
        switch (_currentState)
        {
            case EnemyAIState.Patrol:
                UpdatePatrol(dt);
                break;
            case EnemyAIState.Alert:
                UpdateAlert(dt);
                break;
            case EnemyAIState.Chase:
                UpdateChase(dt);
                break;
        }
    }

    private void UpdatePatrol(float dt)
    {
        _timer += dt;

        // 감지
        if (_robotEnemy.TryDetect(StageState, _entityState.Id, out _alertTargetPlayerId, out bool fromeBehind))
        {
            if (fromeBehind)
            {
                PatrolData patrol = _entityState.Get<PatrolData>();
                if (patrol != null) patrol.Reverse();
            }
            
            _currentState = EnemyAIState.Alert;
            _timer = 0f;
            // 감지 전환시 코드
        }
        
        // 이동
        if(_timer < _moveInterval) return;
        _timer -= _moveInterval;
        DoMove();
    }
    
    // Alert 타이머 찰때까지 대기
    private void UpdateAlert(float dt)
    {
        // 경고 UI 출력
        _eventChannel.OnAlertAndChaseRaised(_alertTargetPlayerId);
        
        _timer += dt;

        if (_timer >= _alertDuration)
        {
            _currentState = EnemyAIState.Chase;
            _timer = 0f;
        }
    }
    
    //Chase 속도가 빨라진 채로 경로 반복
    private void UpdateChase(float dt)
    {
        _timer += dt;
        float chaseInterval = _moveInterval / _chaseSpeedMultiplier;
        if (_timer < chaseInterval) return;
        _timer -= chaseInterval;
        DoMove();
    }

    private void DoMove()
    {
        MoveResult result = _robotEnemy.ResolveTurn(StageState, _entityState.Id, _movementRule);
        
        if (result.IsContactKill)
        {
            _deathRule.ApplyContactKill(StageState, result);
        }
    }

    public void Dispose()
    {
        _eventChannel.OnEventRaised -= OnUpdate;
    }
}
