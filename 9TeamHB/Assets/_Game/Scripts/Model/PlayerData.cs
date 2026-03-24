namespace MyGame2.Stage
{
    // Player 전용 데이터. Kind == Player일 때만 유효.
    public struct PlayerData
    {
        // 플레이어 슬롯 번호 (1 또는 2)
        public int Slot;

        public PlayerData(int slot) { Slot = slot; }
    }
}