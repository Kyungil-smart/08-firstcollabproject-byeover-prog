namespace MyGame2.Stage
{
    //  플레이어 전환 (Tab 키).
    public sealed class TagSystem
    {
        public void Initialize(StageState state)
        {
            int p1 = state.GetPlayerIdBySlot(1);
            if (p1 != StageState.InvalidEntityId)
                state.SetActivePlayer(p1);
        }

        public bool Switch(StageState state)
        {
            int p1 = state.GetPlayerIdBySlot(1);
            int p2 = state.GetPlayerIdBySlot(2);
            if (p1 == StageState.InvalidEntityId || p2 == StageState.InvalidEntityId)
                return false;

            state.SetActivePlayer(state.ActivePlayerId == p1 ? p2 : p1);
            return true;
        }
    }
}