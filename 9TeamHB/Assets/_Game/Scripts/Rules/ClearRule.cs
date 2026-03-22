namespace MyGame2.Stage
{
    public sealed class ClearRule
    {
        public bool Evaluate(StageState state)
        {
            if (state.IsGameOver)
            {
                return false;
            }

            for (int i = 0; i < state.PlayerIds.Count; i++)
            {
                if (!state.TryGetEntity(state.PlayerIds[i], out EntityState player))
                {
                    return false;
                }

                if (!player.IsAlive)
                {
                    return false;
                }

                if (!state.HasGoal(player.Position))
                {
                    return false;
                }
            }

            state.MarkStageClear();
            return true;
        }
    }
}