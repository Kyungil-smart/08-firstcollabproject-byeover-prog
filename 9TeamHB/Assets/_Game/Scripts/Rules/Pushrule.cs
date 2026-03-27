using UnityEngine;

namespace MyGame2.Stage
{
    // 상자 밀기 규칙.
    // CanPush()로 판정만, ExecutePush()로 실행만.
    public sealed class PushRule
    {
        // 밀기 가능 여부 판정. 상태 변경 없음.
        public bool CanPush(StageState state, int pusherId, int boxId, Direction direction)
        {
            if (!state.TryGetEntity(pusherId, out EntityState pusher)) return false;
            if (!state.TryGetEntity(boxId, out EntityState box)) return false;
            if (!box.IsPushable || !box.IsAlive) return false;
            if (!box.CanBePushedBy(pusher.Get<PlayerData>().Slot)) return false;

            GridPos dest = box.Position.Move(direction);
            if (!state.IsInside(dest)) return false;

            CellData cell = state.GetCell(dest);
            if (cell.HasWall) return false;
            if (cell.IsOccupied) return false;

            return true;
        }

        // 밀기 실행. CanPush가 true인 상태에서만 호출.
        public void ExecutePush(StageState state, int boxId, Direction direction)
        {
            if (!state.TryGetEntity(boxId, out EntityState box)) return;

            GridPos dest = box.Position.Move(direction);
            state.TryMoveEntity(boxId, dest);
            if(state.HasCrack(dest))
                state.SetCrackMovable(dest, boxId);

            if (state.HasTrap(dest))
                state.DisableTrap(dest);
        }
    }
}