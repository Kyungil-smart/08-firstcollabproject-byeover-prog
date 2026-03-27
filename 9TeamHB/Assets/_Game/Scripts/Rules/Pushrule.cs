using UnityEngine;

namespace MyGame2.Stage
{
    public sealed class PushRule
    {
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

        public void ExecutePush(StageState state, int boxId, Direction direction)
        {
            if (!state.TryGetEntity(boxId, out EntityState box)) return;

            GridPos dest = box.Position.Move(direction);
            state.TryMoveEntity(boxId, dest);

            if (state.HasTrap(dest))
            {
                state.DisableTrap(dest);
                return; // 함정 위에서 정지 — 미끄러짐 시작 안 함
            }

            // 얼음 상자: 미끄러짐 상태로 전환 (IceSlideProcessor가 이후 처리)
            if (box.Has<IceSlideData>())
            {
                IceSlideData ice = box.Get<IceSlideData>();
                ice.IsSliding = true;
                ice.SlideDirection = direction;
                box.Set(ice);
            }
        }
    }
}