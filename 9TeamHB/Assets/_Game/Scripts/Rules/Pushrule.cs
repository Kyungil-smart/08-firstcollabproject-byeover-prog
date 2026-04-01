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
            if (cell.IsClosedDoor) return false;
            if (cell.IsOccupied) return false;

            return true;
        }

        public void ExecutePush(StageState state, int boxId, Direction direction)
        {
            if (!state.TryGetEntity(boxId, out EntityState box)) return;

            GridPos dest = box.Position.Move(direction);
            state.TryMoveEntity(boxId, dest);

            // 틈새 타일 처리
            if (state.HasCrackNotCovered(dest))
                state.SetCrackMovable(dest, boxId);

            // 파괴 함정: 상자를 파괴하고 함정은 유지
            if (state.HasDestroyTrap(dest))
            {
                state.RemoveEntity(boxId);
                state.SetViewDirty();
                return;
            }

            // 일반 함정: 함정을 비활성화하고 상자는 유지
            if (state.HasTrap(dest))
            {
                state.DisableTrap(dest);
                return;
            }

            // 얼음 상자: 미끄러짐 상태로 전환
            if (box.Has<IceSlideData>())
            {
                IceSlideData ice = box.Get<IceSlideData>();
                ice.IsSliding = true;
                ice.SlideDirection = direction;
                box.Set(ice);
            }
        }

        // 막힌 상태에서 부서져야 하는지 판정
        public bool ShouldBreak(StageState state, int pusherId, int boxId, Direction direction)
        {
            if (!state.TryGetEntity(pusherId, out EntityState pusher)) return false;
            if (!state.TryGetEntity(boxId, out EntityState box)) return false;

            if (!pusher.IsPlayer) return false;
            if (box.Get<BoxData>().Ownership != BoxType.Breakable) return false;

            GridPos dest = box.Position.Move(direction);
            bool isBlocked = !state.IsInside(dest) || state.GetCell(dest).HasWall || state.GetCell(dest).IsOccupied;

            return isBlocked;
        }

        // 부숴지는 상자를 파괴
        public void ExecuteBreak(StageState state, int boxId)
        {
            if (state.TryGetEntity(boxId, out EntityState box))
            {
                box.IsAlive = false;
                Debug.Log($"[PushRule] 부숴지는 상자 파괴: {boxId}");
            }
        }
    }
}