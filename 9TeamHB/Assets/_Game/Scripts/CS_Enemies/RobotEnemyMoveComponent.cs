using System;
using UnityEngine;
using MyGame2.Stage;

public enum RobotAIState
{
    Patrol,  // 일반 순찰 (기본 속도)
    Alert,   // 감지! 0.5초 정지
    Chase    // 같은 경로 + 2배속 순찰 (영구)
}
public class RobotEnemyMoveComponent : IComponentData, IUpdate, IDisposable
{
    public EntityFunctionSO Definition { get; }
    // State 캐싱
    private readonly StageState _stageState; // 맵정보
    private readonly EntityState _entityState; //컴포넌트 소유자
    private readonly FloatEventChannelSO _eventChannel; // 이벤트 채널
    
    // 상태 변수들 (State)
    private RobotAIState _currentAIState;
    private float _timer;

    // 설정값 캐싱 (기능 SO에서 주입)
    private readonly float _moveInterval;
    private readonly float _alertDuration;
    private readonly float _chaseSpeedMultiplier;

    // 룰
    private static readonly RobotEnemy _robotEnemy = new RobotEnemy();
    private static readonly MovementRule _movementRule = new MovementRule(new PushRule());
    private static readonly DeathRule _deathRule = new DeathRule();

    // 생성자: SO(정의)로부터 설정값을 받아 초기화
    public RobotEnemyMoveComponent
        (RobotEnemyMove_Fn definition, StageState stageState, EntityState entityState,
            FloatEventChannelSO eventChannel)
    {
        Definition = definition;
        _stageState = stageState;  
        _entityState = entityState;
        
        // SO로부터 설정값을 복사
        _moveInterval = definition.MoveInterval;
        _alertDuration = definition.AlertDuration;
        _chaseSpeedMultiplier = definition.ChaseSpeedMultiplier;
        
        // 이벤트 구독
        _eventChannel = eventChannel;
        _eventChannel.OnEventRaised += OnUpdate;

        // 상태 초기화
        _currentAIState = RobotAIState.Patrol;
        _timer = 0f;
    }

    // 업데이트 로직 
    public void OnUpdate(float dt)
    {
        switch (_currentAIState)
        {
            case RobotAIState.Patrol:
                UpdatePatrol(dt);
                break;
            case RobotAIState.Alert:
                UpdateAlert(dt);
                break;
            case RobotAIState.Chase:
                UpdateChase(dt);
                break;
        }
    }

    // --- 기존 RobotAutoMover의 로직 ---
    private void UpdatePatrol(float dt)
    {
        _timer += dt;
        if (_robotEnemy.TryDetect(_stageState, _entityState.Id, out int _, out bool _))
        {
            _currentAIState = RobotAIState.Alert;
            _timer = 0f;
            return;
        }

        if (_timer < _moveInterval) return;
        _timer -= _moveInterval;
        DoMove();
    }

    private void UpdateAlert(float dt)
    {
        _timer += dt;
        if (_timer >= _alertDuration)
        {
            _currentAIState = RobotAIState.Chase;
            _timer = 0f;
        }
    }
    
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
        MoveResult result = _robotEnemy.ResolveTurn(_stageState, _entityState.Id, _movementRule);
        if (result.IsContactKill)
        {
            _deathRule.ApplyContactKill(_stageState, result);
        }
        _stageState.SetViewDirty();
    }

    public void Dispose()
    {
        // 이벤트 구독 해제
        _eventChannel.OnEventRaised -= OnUpdate;
    }
}