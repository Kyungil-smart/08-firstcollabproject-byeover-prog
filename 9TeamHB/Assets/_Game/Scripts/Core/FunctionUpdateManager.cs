using System.Collections.Generic;
using UnityEngine;

namespace MyGame2.Stage
{
    // IUpdate를 가진 모든 EntityFunctionSO를 업데이트하는 중앙 관리자
    public sealed class FunctionUpdateManager : MonoBehaviour
    {
        [SerializeField] private StageManager stageManager;
        [SerializeField] private GameManager gameManager;
        [SerializeField] FloatEventChannelSO deltaTimeEvent;

        // IUpdate 이벤트를 구독하는 컴포넌트 목록 (이벤트 채널의 구독자 리스트)
        private readonly List<IUpdate> _subscribers = new List<IUpdate>();

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
            _subscribers.Clear();
            StageState state = stageManager.CurrentState;
            if (state == null) return;
            
        }

        private void Update()
        {
            if (!IsActive()) return;

            StageState state = stageManager.CurrentState;
            float dt = Time.deltaTime;
            state.ClearViewDirty();

            // 모든 구독자에게 OnUpdate 호출
            deltaTimeEvent.RaiseEvent(dt);

            if (state.IsViewDirty)
            {
                stageManager.Events?.RaiseTurnExecuted(TurnOutcome.None());
            }
        }

        private bool IsActive()
        {
            if (stageManager == null || stageManager.CurrentState == null) return false;
            if (stageManager.CurrentState.IsGameOver || stageManager.CurrentState.IsStageClear) return false;
            if (stageManager.CurrentState.IsUndoProcessing) return false;
            if (gameManager != null && gameManager.CurrentState != GameFlowState.Playing) return false;
            return true;
        }
    }
}