using System;
using UnityEngine;

namespace MyGame2.Stage
{
    // 게임 전체 흐름 상태를 관리한다.
    // StageEvents를 구독하여 게임 오버/클리어를 자동 감지한다.
    // WarpComplete를 구독하여 클리어 기록 저장 + 클리어 UI 표시.
    public enum GameFlowState
    {
        Boot = 0,
        Playing = 1,
        Paused = 2,
        GameOver = 3,
        StageClear = 4
    }

    public sealed class GameManager : MonoBehaviour
    {
        [Header("씬 참조")]
        [Tooltip("StageManager에서 이벤트를 구독하기 위해 필요")]
        [SerializeField] private StageManager stageManager;

        // 외부에서 게임 흐름 상태 변경을 감지할 수 있는 이벤트.
        public event Action<GameFlowState> StateChanged;

        public GameFlowState CurrentState { get; private set; } = GameFlowState.Boot;
        
        private StageEvents _subscribedEvents;

        private void OnEnable()
        {
            if (stageManager != null)
            {
                SubscribeToStageEvents(stageManager.Events);
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromStageEvents();
        }
        
        public void SubscribeToStageEvents(StageEvents events)
        {
            UnsubscribeFromStageEvents();

            if (events == null)
            {
                return;
            }

            _subscribedEvents = events;
            _subscribedEvents.GameOverTriggered += OnGameOver;
            _subscribedEvents.StageClearTriggered += OnStageClear;
            _subscribedEvents.StageLoaded += OnStageLoaded;
            _subscribedEvents.WarpComplete += OnWarpComplete;
        }

        private void UnsubscribeFromStageEvents()
        {
            if (_subscribedEvents == null)
            {
                return;
            }

            _subscribedEvents.GameOverTriggered -= OnGameOver;
            _subscribedEvents.StageClearTriggered -= OnStageClear;
            _subscribedEvents.StageLoaded -= OnStageLoaded;
            _subscribedEvents.WarpComplete -= OnWarpComplete;
            _subscribedEvents = null;
        }
        
        private void OnGameOver()
        {
            SetState(GameFlowState.GameOver);

            // 게임오버 시 즉시 GameQuit UI 표시
            if (InGameUIManager.Instance != null)
            {
                InGameUIManager.Instance.ShowGameQuit();
            }
        }

        private void OnStageClear()
        {
            // 상태만 전환. UI는 워프 연출 완료 후 OnWarpComplete에서 표시.
            SetState(GameFlowState.StageClear);
        }

        // 워프 연출 완료 → 클리어 기록 저장 + 클리어 UI 표시
<<<<<<< HEAD
        // +분기 처리 추가, 엔딩씬으로 이동하기
        private void OnWarpComplete()
        {
            if (stageManager != null)
            {
                // 1. 임시 처리: 병합 에러 해결 전까지 currentIndex를 0으로 고정
                int currentIndex = 0; 
                // StageProgressManager.MarkCleared(currentIndex); // 에러가 나므로 이 줄은 계속 주석 유지!

                // 2. 만약 방금 깬 스테이지가 마지막 메인 스테이지 클리어라면?
                if (currentIndex == 14) // <--- 이 if문이 살아있어야 아래 else가 에러 나지 않습니다!
                {
                    // 곧바로 엔딩 씬으로 이동!
                    LoadingManager.LoadScene("Ending_Scene");
                }
                else
                {
                    // 마지막 판 x -> 평소처럼 클리어 UI 표시
                    if (InGameUIManager.Instance != null)
                    {
                        InGameUIManager.Instance.ShowGameClear();
                    }
                }
=======
        private void OnWarpComplete()
        {
            // 현재 스테이지 클리어 기록 저장
            if (stageManager != null)
            {
                StageProgressManager.MarkCleared(stageManager.CurrentStageIndex);
            }

            // 클리어 UI 표시
            if (InGameUIManager.Instance != null)
            {
                InGameUIManager.Instance.ShowGameClear();
>>>>>>> origin/Develop
            }
        }

        private void OnStageLoaded(int stageIndex)
        {
            SetState(GameFlowState.Playing);
        }
        
        public void SetState(GameFlowState nextState)
        {
            if (CurrentState == nextState)
            {
                return;
            }

            CurrentState = nextState;
            Time.timeScale = nextState == GameFlowState.Paused ? 0f : 1f;
            StateChanged?.Invoke(CurrentState);
        }
    }
}