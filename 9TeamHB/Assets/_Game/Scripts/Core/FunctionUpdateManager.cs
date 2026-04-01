using System.Collections.Generic;
using UnityEngine;

namespace MyGame2.Stage
{
    // IUpdate를 가진 모든 EntityFunctionSO를 업데이트하는 중앙 관리자
    public sealed class FunctionUpdateManager : MonoBehaviour
    {
        [SerializeField] private StageManager stageManager;
        [SerializeField] private GameManager gameManager;
        // todo 스테이지 생성시 이전 구독을 안전하게 해제하고 엔티티들이 새로 구독하는 구조 필요
        //  - StageEvent와 같은 시기에 하면 아마도 될거 같다. 혹은 엔티티 생성 전에 초기화
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
            // ---

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
            if (stageManager.CurrentState.IsUpdatable()) return false;
            if (gameManager != null && gameManager.CurrentState != GameFlowState.Playing) return false;
            return true;
        }
    }
}