namespace MyGame2.Stage
{
    public sealed class TagSystem
    {
        public void Initialize(StageState state)
        {
            int player1Id = state.GetPlayerIdBySlot(1);
            if (player1Id != StageState.InvalidEntityId)
            {
                state.SetActivePlayer(player1Id);
            }
        }

        public bool Switch(StageState state)
        {
            int player1Id = state.GetPlayerIdBySlot(1);
            int player2Id = state.GetPlayerIdBySlot(2);

            if (player1Id == StageState.InvalidEntityId || player2Id == StageState.InvalidEntityId)
            {
                return false;
            }

            int current = state.ActivePlayerId;
            if (current == player1Id)
            {
                state.SetActivePlayer(player2Id);
            }
            else
            {
                state.SetActivePlayer(player1Id);
            }

            return true;
        }
    }
}