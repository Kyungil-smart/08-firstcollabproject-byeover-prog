namespace MyGame2.Stage
{
    // 클리어 판정. 모든 플레이어가 살아있고 Goal 위에 서면 클리어.
    public sealed class ClearRule
    {
        public bool Evaluate(StageState state)
        {
            if (state.IsGameOver) return false;
            for (int i = 0; i < state.PlayerIds.Count; i++)
            {
                if (!state.TryGetEntity(state.PlayerIds[i], out EntityState player)) return false;
                if (!player.IsAlive) return false;
                if (!state.HasGoal(player.Position)) return false;
            }
            state.MarkStageClear();
            return true;
        }
    }
}