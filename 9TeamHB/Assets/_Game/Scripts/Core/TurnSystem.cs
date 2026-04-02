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

            // 판정 (상태 변이 없음)
            if (state.IsPlayerOnLockedDoor)
                direction = Direction.None; // 문이 잠기면 방향을 지워 이동 막음
            MoveResult moveResult = _movementRule.TryMove(state, playerId, direction);

            if (!moveResult.CanMove)
            {
                // 부서지는 상자 체크
                if (state.TryGetEntity(playerId, out EntityState player))
                {
                    GridPos target = player.Position.Move(direction);
                    if (state.IsInside(target))
                    {
                        int occupantId = state.GetOccupantId(target);
                        if (occupantId != StageState.InvalidEntityId
                            && state.TryGetEntity(occupantId, out EntityState box))
                        {
                            // 벽/상자에 막힌 부서지는 상자 -> 파괴
                            if (_pushRule.ShouldBreak(state, playerId, occupantId, direction))
                            {
                                // IsBreaking 플래그만 세움 (애니메이션 후 제거는 Visual이 처리)
                                if (box.Has<BreakableData>())
                                {
                                    BreakableData bd = box.Get<BreakableData>();
                                    bd.IsBreaking = true;
                                    box.Set(bd);
                                }
                                _pushRule.ExecuteBreak(state, occupantId);
                                return FinishTurn(state, playerId, moveResult, direction);
                            }

                            // 톱날 범위로 밀리는 부서지는/얼음 상자 -> 파괴
                            if (_pushRule.ShouldBreakBySaw(state, playerId, occupantId, direction))
                            {
                                _pushRule.ExecuteSawBreak(state, occupantId, direction);
                                return FinishTurn(state, playerId, moveResult, direction);
                            }
                        }
                    }
                }

                return TurnOutcome.Ignored(moveResult);
            }

            // 상자 밀기 실행 (PushAndMove인 경우)
            if (moveResult.IsPushAndMove)
            {
                _pushRule.ExecutePush(state, moveResult.TargetEntityId, direction);
            }
            
            if (moveResult.Type == MoveResultType.OpenDoor)
            {
                //문 열기
                state.OpenDoor(moveResult.MoverId, moveResult.To);
            }

            // 플레이어 이동 실행
            state.TryMoveEntity(playerId, moveResult.To);
            state.SetFacing(playerId, direction);

            // 틈새 위의 부서지는 상자 밟기 체크
            CheckBreakableOnCrack(state, moveResult.To);

            // 히든 함정 판정 (일반 함정보다 먼저)
            if (state.HasHiddenTrap(moveResult.To))
            {
                state.RevealHiddenTrap(moveResult.To);
                state.MarkGameOver();

                state.Events?.RaiseHiddenTrapPlayerKill(playerId, moveResult.To);

                state.AdvanceTurn();

                TurnOutcome hiddenTrapDeath = TurnOutcome.Create(
                    moveResult,
                    Array.Empty<MoveResult>(),
                    Array.Empty<MoveResult>(),
                    Array.Empty<int>(),
                    true, false);

                state.Events?.RaiseTurnExecuted(hiddenTrapDeath);
                return hiddenTrapDeath;
            }

            // 일반 함정 밟기 판정
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

            // 바닥형 함정 판정
            // 엔티티 기반: 각 SawTrap의 커버 범위에 플레이어가 있는지 체크
            // 앵커 셀에 Active 플래그가 켜져 있으면 (버튼/스위치로 비활성화) 안전
            if (state.IsInSawTrapRange(moveResult.To))
            {
                state.KillEntity(playerId);
                state.MarkGameOver();
                state.AdvanceTurn();

                TurnOutcome sawDeath = TurnOutcome.Create(
                    moveResult,
                    Array.Empty<MoveResult>(),
                    Array.Empty<MoveResult>(),
                    Array.Empty<int>(),
                    true, false);

                state.Events?.RaiseTurnExecuted(sawDeath);
                return sawDeath;
            }

            // 카메라 회전
            state.RotateAllCameras();

            // 카메라 감지
            _detectedBuffer.Clear();
            _detectionRule.DetectPlayers(state, _detectedBuffer);

            if (_detectedBuffer.Count > 0)
                _deathRule.ApplyCameraDetections(state, _detectedBuffer);

            // 클리어 판정
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

        // 상자 파괴 후 턴 마무리
        private TurnOutcome FinishTurn(StageState state, int playerId,
            MoveResult originalMove, Direction direction)
        {
            state.SetFacing(playerId, direction);
            state.RotateAllCameras();

            _detectedBuffer.Clear();
            _detectionRule.DetectPlayers(state, _detectedBuffer);
            if (_detectedBuffer.Count > 0)
                _deathRule.ApplyCameraDetections(state, _detectedBuffer);

            bool stageClear = false;
            if (!state.IsGameOver)
                stageClear = _clearRule.Evaluate(state);

            state.AdvanceTurn();

            TurnOutcome outcome = TurnOutcome.Create(
                originalMove,
                Array.Empty<MoveResult>(),
                Array.Empty<MoveResult>(),
                new List<int>(_detectedBuffer),
                state.IsGameOver, stageClear);

            state.Events?.RaiseTurnExecuted(outcome);
            return outcome;
        }

        // 플레이어가 틈새 위의 부서지는 상자를 밟았는지 체크
        private void CheckBreakableOnCrack(StageState state, GridPos playerPos)
        {
            foreach (EntityState e in state.Entities)
            {
                if (!e.IsAlive || !e.IsBox) continue;
                if (e.Position.X != playerPos.X || e.Position.Y != playerPos.Y) continue;
                if (!e.Has<BreakableData>()) continue;

                // IsBreaking 플래그 설정
                BreakableData bd = e.Get<BreakableData>();
                bd.IsBreaking = true;
                e.Set(bd);

                e.IsAlive = false;
                e.IsBlocking = false;
                state.SetViewDirty();

                // 틈새 복원
                CellData crackCell = state.GetCell(playerPos);
                if (crackCell.HasCrack && crackCell.HasActive)
                {
                    state.ClearCellActive(playerPos);
                }

                break;
            }
        }
    }
}