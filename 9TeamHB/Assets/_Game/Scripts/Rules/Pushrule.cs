namespace MyGame2.Stage
{
    // [밀기 규칙]
    // 플레이어가 상자 방향으로 이동하면 상자가 같은 방향으로 1칸 밀림
    // 상자 뒤에 벽, 다른 상자, 다른 엔티티가 있으면 밀 수 없음 (이동 차단)
    // 공용 상자(B): 양쪽 다 밀기 가능
    // P1 전용 상자(O): Player1만 밀기 가능
    // P2 전용 상자(Y): Player2만 밀기 가능
    // 상자를 함정 위에 밀면 함정 비활성화 (덮음)
    // 상자를 골 위에 밀면 골을 막음
    // 연쇄 밀기 불가 (뒤에 상자 있으면 안 밀림)
   
    public sealed class PushRule
    {
       
        public bool TryPush(StageState state, int pusherId, int boxId, Direction direction)
        {
            if (!state.TryGetEntity(pusherId, out EntityState pusher))
            {
                return false;
            }

            if (!state.TryGetEntity(boxId, out EntityState box))
            {
                return false;
            }

            if (!box.IsBox || !box.IsAlive)
            {
                return false;
            }

            // 소유권 확인: 이 플레이어가 이 상자를 밀 수 있는가?
            if (!box.CanBePushedBy(pusher.PlayerSlot))
            {
                return false;
            }

            // 상자가 밀릴 목적지
            GridPos boxDestination = box.Position.Move(direction);

            // 목적지가 맵 밖이면 불가
            if (!state.IsInside(boxDestination))
            {
                return false;
            }

            // 목적지가 벽이면 불가
            CellData destCell = state.GetCell(boxDestination);
            if (destCell.HasWall)
            {
                return false;
            }

            // 목적지에 다른 엔티티가 있으면 불가 (연쇄 밀기 금지)
            if (destCell.IsOccupied)
            {
                return false;
            }

            // 밀기 실행
            state.MoveEntity(boxId, boxDestination);

            // 함정 위에 밀었으면 함정 비활성화
            if (state.HasTrap(boxDestination))
            {
                state.DisableTrap(boxDestination);
            }

            return true;
        }
    }
}