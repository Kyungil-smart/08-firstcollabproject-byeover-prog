using System;
using System.Collections.Generic;

namespace MyGame2.Stage
{
    // 스테이지에서 발생하는 모든 이벤트를 중앙 관리하는 허브.
    // StageState 변이 메서드가 이벤트를 발행하고,
    // View / GameManager / Undo 시스템이 구독한다.
    
    public sealed class StageEvents
    {
        
        // 엔티티 이벤트(나중에 호출 하실려고 할때 쓰시라고 주석 처리 완료)
        // 엔티티가 셀을 이동했을 때. (entityId, from, to)
        public event Action<int, GridPos, GridPos> EntityMoved;

        // 엔티티의 바라보는 방향이 바뀌었을 때. (entityId, newFacing)
        public event Action<int, Direction> FacingChanged;

        // 엔티티가 사망 처리되었을 때. (entityId)
        public event Action<int> EntityKilled;

        // 활성 플레이어가 전환되었을 때. (newActivePlayerId)
        public event Action<int> ActivePlayerChanged;
        
        // 게임 오버 상태가 되었을 때.
        public event Action GameOverTriggered;

        // 스테이지 클리어 상태가 되었을 때.
        public event Action StageClearTriggered;

        // 인덱스가 증가했을 때. (newTurnIndex)
        public event Action<int> TurnAdvanced;
        
        // 플레이어 턴이 실행 완료된 후 발행.
        // View 동기화의 주 트리거.
        public event Action<TurnOutcome> TurnExecuted;

        // 새 스테이지가 로드/리빌드된 직후. (stageIndex)
        public event Action<int> StageLoaded;

        // 워프 연출 완료 후 다음 스테이지 로드 트리거.
        public event Action WarpComplete;
        
        // 적이 월드 메시지를 표시할 때 (entityId, message, duration)
        public event Action<int, string, float> EnemyWorldMessageRequested;
        // 적이 디스폰 시작될 때 (entityId)
        public event Action<int> EnemyDespawnStarted;
        // 히든 함정이 발동되어 드러났을 때 (position)
        public event Action<GridPos> HiddenTrapRevealed;
        // 히든 함정 발동 -> 애니메이션 후 플레이어 Kill 요청 (playerId, trapPosition)
        public event Action<int, GridPos> HiddenTrapPlayerKill;
        // 얼음 상자가 톱날에 의해 파괴됨 (entityId, position, sawFacing)
        public event Action<int, GridPos, Direction> IceBoxSawDestroyed;
        // 페어 활성화 시
        public event Action<GridPos> PairActivated;
        // Undo 실행 완료 시 (다른 팀원 구현용 스텁)
        public event Action UndoExecuted;

        // ID로 view 콜백을 위한 이벤트 딕셔너리
        private Dictionary<int, Action<ViewRequest>> _viewRequests = new();

        public void ViewRequestSubscribe(int id, Action<ViewRequest> request)
        {
            _viewRequests[id] = request;
        }
        

        
        // 발행 메서드 (StageState, TurnSystem 등이 호출)
        
        public void RaiseEntityMoved(int entityId, GridPos from, GridPos to)
        {
            EntityMoved?.Invoke(entityId, from, to);
        }

        public void RaiseFacingChanged(int entityId, Direction newFacing)
        {
            FacingChanged?.Invoke(entityId, newFacing);
        }

        public void RaiseEntityKilled(int entityId)
        {
            EntityKilled?.Invoke(entityId);
        }

        public void RaiseActivePlayerChanged(int newActivePlayerId)
        {
            ActivePlayerChanged?.Invoke(newActivePlayerId);
        }

        public void RaiseGameOver()
        {
            GameOverTriggered?.Invoke();
        }

        public void RaiseStageClear()
        {
            StageClearTriggered?.Invoke();
        }

        public void RaiseTurnAdvanced(int newTurnIndex)
        {
            TurnAdvanced?.Invoke(newTurnIndex);
        }

        public void RaiseTurnExecuted(TurnOutcome outcome)
        {
            TurnExecuted?.Invoke(outcome);
        }

        public void RaiseStageLoaded(int stageIndex)
        {
            StageLoaded?.Invoke(stageIndex);
        }

        public void RaiseWarpComplete()
        {
            WarpComplete?.Invoke();
        }

        // Raise 메서드
        public void RaiseEnemyWorldMessage(int entityId, string message, float duration)
        {
            EnemyWorldMessageRequested?.Invoke(entityId, message, duration);
        }

        public void RaiseEnemyDespawnStarted(int entityId)
        {
            EnemyDespawnStarted?.Invoke(entityId);
        }

        public void RaiseHiddenTrapRevealed(GridPos position)
        {
            HiddenTrapRevealed?.Invoke(position);
        }

        public void RaiseHiddenTrapPlayerKill(int playerId, GridPos trapPosition)
        {
            HiddenTrapPlayerKill?.Invoke(playerId, trapPosition);
        }

        public void RaiseIceBoxSawDestroyed(int entityId, GridPos position, Direction sawFacing)
        {
            IceBoxSawDestroyed?.Invoke(entityId, position, sawFacing);
        }

        public void RaiseUndoExecuted()
        {
            UndoExecuted?.Invoke();
        }

        public void RaisePairActivated(GridPos pairPosition)
        {
            PairActivated?.Invoke(pairPosition);
        }

        public void RaiseViewRequest(ViewRequest request)
        { 
            if(_viewRequests.TryGetValue(request.Id, out var callback))
            {
                callback?.Invoke(request);
            }
        }

       
        // 모든 구독자를 해제한다.
        // 스테이지 전환 시 이전 구독을 깨끗하게 정리할 때 사용.
        public void ClearAll()
        {
            EntityMoved = null;
            FacingChanged = null;
            EntityKilled = null;
            ActivePlayerChanged = null;
            GameOverTriggered = null;
            StageClearTriggered = null;
            TurnAdvanced = null;
            TurnExecuted = null;
            StageLoaded = null;
            WarpComplete = null;
            EnemyWorldMessageRequested = null;
            EnemyDespawnStarted = null;
            HiddenTrapRevealed = null;
            HiddenTrapPlayerKill = null;
            IceBoxSawDestroyed = null;
            UndoExecuted = null;
            _viewRequests.Clear();
        }
    }

    public struct ViewRequest
    {
        public int Id;
        public Action<GridEntityView> Callback;
    }
}