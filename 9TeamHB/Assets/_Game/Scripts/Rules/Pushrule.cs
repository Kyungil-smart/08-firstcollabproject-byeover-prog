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
       
        // 밀기가 가능한지 판정만 한다. 상태를 변경하지 않는다.
        
        public bool CanPush(StageState state, int pusherId, int boxId, Direction direction)
        {
            if (!state.TryGetEntity(pusherId, out EntityState pusher)) return false;
            if (!state.TryGetEntity(boxId, out EntityState box)) return false;
            if (!box.IsBox || !box.IsAlive) return false;
            if (!box.Box.CanBePushedBy(pusher.Player.Slot)) return false;

            GridPos dest = box.Position.Move(direction);
            if (!state.IsInside(dest)) return false;

            CellData cell = state.GetCell(dest);
            if (cell.HasWall) return false;
            if (cell.IsOccupied) return false;

            return true;
        }
        
        // 밀기를 실행한다. CanPush가 true인 상태에서만 호출해야 한다.
        // 상자를 이동시키고, 함정 위면 함정을 비활성화한다.
        
        public void ExecutePush(StageState state, int boxId, Direction direction)
        {
            if (!state.TryGetEntity(boxId, out EntityState box)) return;

            GridPos dest = box.Position.Move(direction);
            state.TryMoveEntity(boxId, dest);

            if (state.HasTrap(dest))
            {
                state.DisableTrap(dest);
            }
        }
    }
}