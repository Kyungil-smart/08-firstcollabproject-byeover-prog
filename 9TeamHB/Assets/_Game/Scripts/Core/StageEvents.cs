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
        public event Action<int, string, float> EnemyWorldMessageRequested;
        public event Action<int> EnemyDespawnStarted;

        // 히든 함정이 발동되어 드러났을 때 (position)
        public event Action<GridPos> HiddenTrapRevealed;

        // 히든 함정 발동 -> 애니메이션 후 플레이어 Kill 요청 (playerId, trapPosition)
        public event Action<int, GridPos> HiddenTrapPlayerKill;

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
        }
    }
}