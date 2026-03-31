using System;
using System.Collections.Generic;

namespace MyGame2.Stage
{
    // 스테이지에서 발생하는 모든 이벤트를 중앙 관리하는 허브.

    public sealed class StageEvents
    {
        public event Action<int, GridPos, GridPos> EntityMoved;
        public event Action<int, Direction> FacingChanged;
        public event Action<int> EntityKilled;
        public event Action<int> ActivePlayerChanged;
        public event Action GameOverTriggered;
        public event Action StageClearTriggered;
        public event Action<int> TurnAdvanced;
        public event Action<TurnOutcome> TurnExecuted;
        public event Action<int> StageLoaded;
        public event Action WarpComplete;

        // ID로 view 콜백을 위한 이벤트 딕셔너리
        private Dictionary<int, Action<ViewRequest>> _viewRequests = new();

        public void ViewRequestSubscribe(int id, Action<ViewRequest> request)
        {
            _viewRequests[id] = request;
        }
        

        
        // 발행 메서드 (StageState, TurnSystem 등이 호출)
        
        public void RaiseEntityMoved(int entityId, GridPos from, GridPos to)
        { EntityMoved?.Invoke(entityId, from, to); }

        public void RaiseFacingChanged(int entityId, Direction newFacing)
        { FacingChanged?.Invoke(entityId, newFacing); }

        public void RaiseEntityKilled(int entityId)
        { EntityKilled?.Invoke(entityId); }

        public void RaiseActivePlayerChanged(int newActivePlayerId)
        { ActivePlayerChanged?.Invoke(newActivePlayerId); }

        public void RaiseGameOver()
        { GameOverTriggered?.Invoke(); }

        public void RaiseStageClear()
        { StageClearTriggered?.Invoke(); }

        public void RaiseTurnAdvanced(int newTurnIndex)
        { TurnAdvanced?.Invoke(newTurnIndex); }

        public void RaiseTurnExecuted(TurnOutcome outcome)
        { TurnExecuted?.Invoke(outcome); }

        public void RaiseStageLoaded(int stageIndex)
        { StageLoaded?.Invoke(stageIndex); }

        public void RaiseWarpComplete()
        { WarpComplete?.Invoke(); }

        public void RaiseEnemyWorldMessage(int entityId, string message, float duration)
        { EnemyWorldMessageRequested?.Invoke(entityId, message, duration); }

        public void RaiseEnemyDespawnStarted(int entityId)
        { EnemyDespawnStarted?.Invoke(entityId); }

        public void RaiseHiddenTrapRevealed(GridPos position)
        { HiddenTrapRevealed?.Invoke(position); }

        public void RaiseHiddenTrapPlayerKill(int playerId, GridPos trapPosition)
        { HiddenTrapPlayerKill?.Invoke(playerId, trapPosition); }

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
            _viewRequests.Clear();
        }
    }

    public struct ViewRequest
    {
        public int Id;
        public Action<GridEntityView> Callback;
    }
}