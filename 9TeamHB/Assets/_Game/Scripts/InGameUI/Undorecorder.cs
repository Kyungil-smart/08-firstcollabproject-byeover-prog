using System.Collections.Generic;
using UnityEngine;

namespace MyGame2.Stage
{
    // 턴마다 스냅샷을 기록하고, Undo 중 되감기를 실행하는 시스템.
    // StageManager 이벤트를 구독하며, 스테이지 전환 시 자동 초기화.

    public sealed class UndoRecorder : MonoBehaviour
    {
        [Header("씬 참조")]
        [SerializeField] private StageManager stageManager;

        [Header("되감기 설정")]
        [Tooltip("되감기 한 스텝 간격 (초, unscaled)")]
        [SerializeField] private float replayInterval = 0.12f;

        // 스냅샷 스택: [0]=초기, [1]=턴1 이후, [2]=턴2 이후 ...
        private readonly Stack<StageSnapshot> _snapshots = new Stack<StageSnapshot>(64);
        private float _replayTimer;
        private bool _isRestoring; // Restore 중 TurnExecuted 재진입 방지

        // 생명주기
        
        private void OnEnable()
        {
            if (stageManager == null) return;
            stageManager.Events.StageLoaded  += OnStageLoaded;
            stageManager.Events.TurnExecuted += OnTurnExecuted;
        }

        private void OnDisable()
        {
            if (stageManager == null) return;
            stageManager.Events.StageLoaded  -= OnStageLoaded;
            stageManager.Events.TurnExecuted -= OnTurnExecuted;
        }

        private void Update()
        {
            StageState state = stageManager != null ? stageManager.CurrentState : null;
            if (state == null) return;

            // Undo 중이 아니면 타이머만 리셋
            if (!state.IsUndoProcessing)
            {
                _replayTimer = 0f;
                return;
            }

            // 되감기 실행
            _replayTimer += Time.unscaledDeltaTime;
            if (_replayTimer >= replayInterval)
            {
                _replayTimer -= replayInterval;
                ReplayStep();
            }
        }
        
        // 새 스테이지 로드 → 스택 비우고 초기 스냅샷 기록
        private void OnStageLoaded(int stageIndex)
        {
            _snapshots.Clear();
            _replayTimer = 0f;

            // 초기 상태 기록
            StageState state = stageManager.CurrentState;
            if (state != null)
                _snapshots.Push(StageSnapshot.Capture(state));
        }

        // 턴 실행 완료 → 스냅샷 기록
        private void OnTurnExecuted(TurnOutcome outcome)
        {
            // Restore 중 발생하는 TurnExecuted는 무시
            if (_isRestoring) return;

            // 실제 턴이 실행된 경우만 기록 (view-only 갱신 제외)
            if (!outcome.Executed) return;

            StageState state = stageManager.CurrentState;
            if (state == null) return;

            _snapshots.Push(StageSnapshot.Capture(state));
        }

        // 되감기

        private void ReplayStep()
        {
            // 초기 스냅샷(맨 아래 1개)은 유지 — 더 이상 되감을 수 없음
            if (_snapshots.Count <= 1) return;

            // 현재 상태 스냅샷 버리기
            _snapshots.Pop();

            // 이전 상태로 복원
            StageSnapshot prev = _snapshots.Peek();

            _isRestoring = true;
            stageManager.CurrentState.Restore(prev);

            // View 동기화 트리거
            stageManager.Events?.RaiseTurnExecuted(TurnOutcome.None());
            _isRestoring = false;
        }
    }
}