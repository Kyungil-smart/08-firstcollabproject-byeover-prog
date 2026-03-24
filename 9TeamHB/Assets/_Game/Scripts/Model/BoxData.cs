namespace MyGame2.Stage
{
    // Box 전용 데이터. Kind == Box일 때만 유효.
    public struct BoxData
    {
        // 이 상자를 밀 수 있는 플레이어 제한
        public BoxType Ownership;

        public BoxData(BoxType ownership) { Ownership = ownership; }

        // 해당 플레이어 슬롯이 이 상자를 밀 수 있는가?
        public bool CanBePushedBy(int playerSlot)
        {
            switch (Ownership)
            {
                case BoxType.Shared:      return true;
                case BoxType.Player1Only: return playerSlot == 1;
                case BoxType.Player2Only: return playerSlot == 2;
                case BoxType.Iron:        return false;
                default:                  return false;
            }
        }
    }
}