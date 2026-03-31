using System;
using System.Collections.Generic;

namespace MyGame2.Stage
{
    public sealed class TurnSystem
    {
        private readonly MovementRule _movementRule;
        private readonly PushRule _pushRule;
        private readonly CameraEnemy _cameraEnemy;
        private readonly DetectionRule _detectionRule;
        private readonly DeathRule _deathRule;
        private readonly ClearRule _clearRule;

        private readonly List<int> _detectedBuffer = new List<int>(4);

        public TurnSystem(
            MovementRule movementRule,
            PushRule pushRule,
            CameraEnemy cameraEnemy,
            DetectionRule detectionRule,
            DeathRule deathRule,
            ClearRule clearRule)
        {
            _movementRule = movementRule;
            _pushRule = pushRule;
            _cameraEnemy = cameraEnemy;
            _detectionRule = detectionRule;
            _deathRule = deathRule;
            _clearRule = clearRule;
        }

        public TurnOutcome TryExecutePlayerTurn(StageState state, Direction direction)
        {
            if (state == null || state.IsGameOver || state.IsStageClear)
                return TurnOutcome.Ignored(MoveResult.Blocked(
                    StageState.InvalidEntityId,
                    new GridPos(0, 0), new GridPos(0, 0),
                    MoveBlockReason.DeadEntity));

            int playerId = state.ActivePlayerId;

            // 1. 판정 (상태 변이 없음)
            if (state.IsPlayerOnLockedDoor)
                direction = Direction.None; // 문이 잠기면 방향을 지워 이동 막음
            MoveResult moveResult = _movementRule.TryMove(state, playerId, direction);

            if (!moveResult.CanMove)
                return TurnOutcome.Ignored(moveResult);

            // 2. 상자 밀기 실행 (PushAndMove인 경우)
            if (moveResult.IsPushAndMove)
            {
                _pushRule.ExecutePush(state, moveResult.TargetEntityId, direction);
            }

            // 3. 플레이어 이동 실행
            state.TryMoveEntity(playerId, moveResult.To);
            state.SetFacing(playerId, direction);

            // 4. 함정 밟기 판정
            if (state.HasTrap(moveResult.To))
            {
                state.KillEntity(playerId);
                state.MarkGameOver();
                state.AdvanceTurn();

                TurnOutcome trapDeath = TurnOutcome.Create(
                    moveResult,
                    Array.Empty<MoveResult>(),
                    Array.Empty<MoveResult>(),
                    Array.Empty<int>(),
                    true, false);

                state.Events?.RaiseTurnExecuted(trapDeath);
                return trapDeath;
            }

            // 5. 카메라 회전
            state.RotateAllCameras();

            // 6. 카메라 감지
            _detectedBuffer.Clear();
            _detectionRule.DetectPlayers(state, _detectedBuffer);

            if (_detectedBuffer.Count > 0)
                _deathRule.ApplyCameraDetections(state, _detectedBuffer);

            // 7. 클리어 판정
            bool stageClear = false;
            if (!state.IsGameOver)
                stageClear = _clearRule.Evaluate(state);

            state.AdvanceTurn();

            TurnOutcome outcome = TurnOutcome.Create(
                moveResult,
                Array.Empty<MoveResult>(),
                Array.Empty<MoveResult>(),
                new List<int>(_detectedBuffer),
                state.IsGameOver,
                stageClear);

            state.Events?.RaiseTurnExecuted(outcome);
            return outcome;
        }
    }
}
