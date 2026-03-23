using System.Collections.Generic;
using UnityEngine;

namespace MyGame2.Stage
{
    // 로봇을 실시간으로 자동 이동시킨다.
    // 플레이어 턴과 독립적으로 일정 간격마다 1칸씩 이동한다.
    // 경로는 Stage1Config 등에서 PatrolData로 주입해야 한다.
  
    public sealed class RobotAutoMover : MonoBehaviour
    {
        [Header("씬 참조")]
        [SerializeField] private StageManager stageManager;
        [SerializeField] private GameManager gameManager;

        [Header("이동 설정")]
        [Tooltip("로봇 자동 이동 간격 (초)")]
        [SerializeField] private float moveInterval = 0.2f;

        private RobotEnemy _robotEnemy;
        private MovementRule _movementRule;
        private DeathRule _deathRule;
        private float _timer;

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
            _timer = 0f;
        }

        private void Update()
        {
            if (!IsActive()) return;

            _timer += Time.deltaTime;
            if (_timer < moveInterval) return;

            _timer -= moveInterval;
            MoveAllRobots();
        }

        private bool IsActive()
        {
            if (stageManager == null || stageManager.CurrentState == null) return false;
            if (stageManager.CurrentState.IsGameOver || stageManager.CurrentState.IsStageClear) return false;
            if (gameManager != null && gameManager.CurrentState != GameFlowState.Playing) return false;
            return stageManager.CurrentState.RobotIds.Count > 0;
        }

        private void MoveAllRobots()
        {
            StageState state = stageManager.CurrentState;

            for (int i = 0; i < state.RobotIds.Count; i++)
            {
                if (state.IsGameOver) break;

                MoveResult result = _robotEnemy.ResolveTurn(state, state.RobotIds[i], _movementRule);

                if (result.IsContactKill)
                    _deathRule.ApplyContactKill(state, result);
            }

            // View 갱신 트리거
            stageManager.Events?.RaiseTurnExecuted(TurnOutcome.None());
        }
    }
}