using System.Collections.Generic;

namespace MyGame2.Stage
{
    // 턴 실행의 유일한 진입점.
    // [턴 실행 순서]
    // 플레이어 이동 (Phase 2에서 상자 밀기 추가 예정)
    // 함정 밟기 판정
    // 카메라 회전 (시계방향 1단계)
    // 카메라 감지 판정
    // 사망 판정
    // 클리어 판정

    public sealed class TurnSystem
    {
        private readonly MovementRule _movementRule;
        private readonly CameraEnemy _cameraEnemy;
        private readonly DeathRule _deathRule;
        private readonly ClearRule _clearRule;
        private readonly DetectionRule _detectionRule;

        // 재사용 버퍼
        private readonly List<int> _detectedPlayersBuffer = new List<int>(4);

        public TurnSystem(
            MovementRule movementRule,
            CameraEnemy cameraEnemy,
            DetectionRule detectionRule,
            DeathRule deathRule,
            ClearRule clearRule)
        {
            _movementRule = movementRule;
            _cameraEnemy = cameraEnemy;
            _detectionRule = detectionRule;
            _deathRule = deathRule;
            _clearRule = clearRule;
        }

        // 내부 의존성을 자동 생성하는 팩토리.
        public static TurnSystem CreateDefault()
        {
            CameraEnemy cameraEnemy = new CameraEnemy();
            return new TurnSystem(
                new MovementRule(),
                cameraEnemy,
                new DetectionRule(cameraEnemy),
                new DeathRule(),
                new ClearRule());
        }

        // 플레이어 턴을 실행하고 결과를 반환한다.
        public TurnOutcome TryExecutePlayerTurn(StageState state, Direction direction)
        {
            if (state == null || state.IsGameOver || state.IsStageClear)
            {
                return TurnOutcome.Ignored(
                    MoveResult.Blocked(
                        StageState.InvalidEntityId,
                        new GridPos(0, 0), new GridPos(0, 0),
                        MoveBlockReason.DeadEntity));
            }

            int activePlayerId = state.ActivePlayerId;
            MoveResult playerMove = _movementRule.TryMove(state, activePlayerId, direction);

            if (!playerMove.Succeeded)
            {
                return TurnOutcome.Ignored(playerMove);
            }

            // 플레이어 이동
            state.MoveEntity(activePlayerId, playerMove.To);
            state.SetFacing(activePlayerId, direction);

            // 함정 밟기 판정
            if (state.HasTrap(playerMove.To))
            {
                state.KillEntity(activePlayerId);
                state.MarkGameOver();

                TurnOutcome trapDeath = TurnOutcome.Create(
                    playerMove,
                    System.Array.Empty<MoveResult>(),
                    System.Array.Empty<MoveResult>(),
                    System.Array.Empty<int>(),
                    true, false);

                state.AdvanceTurn();
                state.Events?.RaiseTurnExecuted(trapDeath);
                return trapDeath;
            }

            // 카메라 회전
            state.RotateAllCameras();

            // 카메라 감지
            _detectedPlayersBuffer.Clear();
            _detectionRule.DetectPlayers(state, _detectedPlayersBuffer);

            if (_detectedPlayersBuffer.Count > 0)
            {
                _deathRule.ApplyCameraDetections(state, _detectedPlayersBuffer);
            }

            // 클리어 판정
            bool stageClear = false;
            if (!state.IsGameOver)
            {
                stageClear = _clearRule.Evaluate(state);
            }

            state.AdvanceTurn();

            TurnOutcome outcome = TurnOutcome.Create(
                playerMove,
                System.Array.Empty<MoveResult>(),
                System.Array.Empty<MoveResult>(),
                new List<int>(_detectedPlayersBuffer),
                state.IsGameOver,
                stageClear);

            state.Events?.RaiseTurnExecuted(outcome);
            return outcome;
        }
    }
}
