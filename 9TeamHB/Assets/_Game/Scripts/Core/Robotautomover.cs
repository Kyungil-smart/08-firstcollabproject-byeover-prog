using System.Collections.Generic;
using UnityEngine;

namespace MyGame2.Stage
{
    // 로봇을 실시간으로 자동 이동시킨다.

    public sealed class RobotAutoMover : MonoBehaviour
    {
        [Header("씬 참조")]
        [SerializeField] private StageManager stageManager;
        [SerializeField] private GameManager gameManager;

        [Header("이동 설정")]
        [Tooltip("일반 순찰 이동 간격 (초)")]
        [SerializeField] private float moveInterval = 0.2f;

        [Tooltip("감지 후 정지 시간 (초)")]
        [SerializeField] private float alertDuration = 0.5f;

        [Tooltip("경계 모드 속도 배율")]
        [SerializeField] private float chaseSpeedMultiplier = 2f;

        private RobotEnemy _robotEnemy;
        private MovementRule _movementRule;
        private DeathRule _deathRule;

        private readonly Dictionary<int, RobotAIState> _states = new Dictionary<int, RobotAIState>();
        private readonly Dictionary<int, float> _timers = new Dictionary<int, float>();

        private void Awake()
        {
            _robotEnemy = new RobotEnemy();
            _movementRule = new MovementRule(new PushRule());
            _deathRule = new DeathRule();
        }

        private void OnEnable()
        {
            if (stageManager != null)
                stageManager.Events.StageLoaded += OnStageLoaded;
        }

        private void OnDisable()
        {
            if (stageManager != null)
                stageManager.Events.StageLoaded -= OnStageLoaded;
        }

        private void OnStageLoaded(int stageIndex)
        {
            _states.Clear();
            _timers.Clear();

            StageState state = stageManager.CurrentState;
            if (state == null) return;

            for (int i = 0; i < state.RobotIds.Count; i++)
            {
                int id = state.RobotIds[i];
                _states[id] = RobotAIState.Patrol;
                _timers[id] = 0f;
            }
        }

        private void Update()
        {
            if (!IsActive()) return;

            StageState state = stageManager.CurrentState;
            float dt = Time.deltaTime;
            bool viewDirty = false;

            for (int i = 0; i < state.RobotIds.Count; i++)
            {
                if (!state.IsUpdatable())
                {
                    break;
                }

                int robotId = state.RobotIds[i];
                EnsureRobotTracked(robotId);

                switch (_states[robotId])
                {
                    case RobotAIState.Patrol:
                        viewDirty |= UpdatePatrol(state, robotId, dt);
                        break;
                    case RobotAIState.Alert:
                        viewDirty |= UpdateAlert(state, robotId, dt);
                        break;
                    case RobotAIState.Chase:
                        viewDirty |= UpdateChase(state, robotId, dt);
                        break;
                }
            }

            if (viewDirty)
                stageManager.Events?.RaiseTurnExecuted(TurnOutcome.None());
        }

        // Patrol

        private bool UpdatePatrol(StageState state, int robotId, float dt)
        {
            _timers[robotId] += dt;

            if (_robotEnemy.TryDetect(state, robotId, out int _, out bool fromBehind))
            {
                // 뒤에서 감지 → Get<PatrolData>()로 접근하여 방향 전환
                if (fromBehind && state.TryGetEntity(robotId, out EntityState robot))
                {
                    PatrolData patrol = robot.Get<PatrolData>();
                    if (patrol != null)
                        patrol.Reverse();
                }

                _states[robotId] = RobotAIState.Alert;
                _timers[robotId] = 0f;
                return false;
            }

            if (_timers[robotId] < moveInterval) return false;
            _timers[robotId] -= moveInterval;

            return DoMove(state, robotId);
        }

        // Alert

        private bool UpdateAlert(StageState state, int robotId, float dt)
        {
            _timers[robotId] += dt;

            if (_timers[robotId] >= alertDuration)
            {
                _states[robotId] = RobotAIState.Chase;
                _timers[robotId] = 0f;
            }

            return false;
        }

        // Chase

        private bool UpdateChase(StageState state, int robotId, float dt)
        {
            _timers[robotId] += dt;

            float chaseInterval = moveInterval / chaseSpeedMultiplier;
            if (_timers[robotId] < chaseInterval) return false;
            _timers[robotId] -= chaseInterval;

            return DoMove(state, robotId);
        }

        // 공통 이동

        private bool DoMove(StageState state, int robotId)
        {
            MoveResult result = _robotEnemy.ResolveTurn(state, robotId, _movementRule);

            if (result.IsContactKill)
                _deathRule.ApplyContactKill(state, result);

            return true;
        }

        // 유틸리티

        private void EnsureRobotTracked(int robotId)
        {
            if (_states.ContainsKey(robotId))
            {
                return;
            }
            _states[robotId] = RobotAIState.Patrol;
            _timers[robotId] = 0f;
        }

        private bool IsActive()
        {
            if (stageManager == null || stageManager.CurrentState == null) return false;
            if (!stageManager.CurrentState.IsUpdatable()) return false;
            if (gameManager != null && gameManager.CurrentState != GameFlowState.Playing) return false;
            return stageManager.CurrentState.RobotIds.Count > 0;
        }
    }
}