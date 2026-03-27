namespace MyGame2.Stage
{
    // Player 전용 데이터.
    public struct PlayerData : IComponentData
    {
        // 플레이어 슬롯 번호 (1 또는 2)
        public int Slot;

        public PlayerData(int slot) { Slot = slot; }
    }
}