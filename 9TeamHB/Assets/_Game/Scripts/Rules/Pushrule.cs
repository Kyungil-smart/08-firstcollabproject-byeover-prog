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

            // 톱날 범위로는 일반 상자를 밀 수 없음
            // 부서지는 상자와 얼음 상자는 ShouldBreakBySaw에서 별도 처리
            if (state.IsInSawTrapRange(dest))
            {
                return false;
            }

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

        // 막힌 상태에서 부서져야 하는지 판정 (Breakable 상자)
        public bool ShouldBreak(StageState state, int pusherId, int boxId, Direction direction)
        {
            if (!state.TryGetEntity(pusherId, out EntityState pusher)) return false;
            if (!state.TryGetEntity(boxId, out EntityState box)) return false;

            if (!pusher.IsPlayer) return false;
            if (!box.Has<BreakableData>()) return false;

            GridPos dest = box.Position.Move(direction);
            bool isBlocked = !state.IsInside(dest)
                || state.GetCell(dest).HasWall
                || state.GetCell(dest).IsClosedDoor
                || state.GetCell(dest).IsOccupied;

            Debug.Log($"[PushRule] ShouldBreak: boxId={boxId}, dest={dest}, isBlocked={isBlocked}");
            return isBlocked;
        }

        // 톱날 범위에 밀 때 파괴되는 상자인지 판정 (부서지는 상자 + 얼음 상자)
        public bool ShouldBreakBySaw(StageState state, int pusherId, int boxId, Direction direction)
        {
            if (!state.TryGetEntity(pusherId, out EntityState pusher)) return false;
            if (!state.TryGetEntity(boxId, out EntityState box)) return false;

            if (!pusher.IsPlayer) return false;

            GridPos dest = box.Position.Move(direction);
            if (!state.IsInside(dest)) return false;

            // 톱날 범위가 아니면 해당 없음
            if (!state.IsInSawTrapRange(dest)) return false;

            // 목적지에 벽/문/점유가 있으면 안 됨
            CellData cell = state.GetCell(dest);
            if (cell.HasWall || cell.IsClosedDoor || cell.IsOccupied) return false;

            // 부서지는 상자 또는 얼음 상자만 파괴 가능
            if (box.Has<BreakableData>()) return true;
            BoxData boxData = box.Get<BoxData>();
            if (boxData.Ownership == BoxType.Ice) return true;

            return false;
        }

        // 톱날에 의한 상자 파괴 실행
        public void ExecuteSawBreak(StageState state, int boxId, Direction direction)
        {
            if (!state.TryGetEntity(boxId, out EntityState box)) return;

            GridPos dest = box.Position.Move(direction);
            state.TryMoveEntity(boxId, dest);

            // 얼음 상자면 쪼개짐 이벤트 발행
            if (box.Has<IceSlideData>())
            {
                Direction sawFacing = state.GetSawTrapFacingAt(dest);
                state.Events?.RaiseIceBoxSawDestroyed(boxId, dest, sawFacing);
                state.RemoveEntity(boxId);
                state.SetViewDirty();
                return;
            }

            // 부서지는 상자면 IsBreaking 플래그 -> BreakableBoxManager가 애니메이션 처리
            if (box.Has<BreakableData>())
            {
                BreakableData bd = box.Get<BreakableData>();
                bd.IsBreaking = true;
                box.Set(bd);
                box.IsAlive = false;
                box.IsBlocking = false;
                state.SetViewDirty();
                return;
            }

            // 그 외 -> 즉시 제거
            state.RemoveEntity(boxId);
            state.SetViewDirty();
        }

        // 부숴지는 상자를 파괴
        public void ExecuteBreak(StageState state, int boxId)
        {
            if (!state.TryGetEntity(boxId, out EntityState box)) return;

            // BreakableBoxManager가 감지할 수 있도록 IsBreaking 플래그 설정
            if (box.Has<BreakableData>())
            {
                BreakableData bd = box.Get<BreakableData>();
                bd.IsBreaking = true;
                box.Set(bd);
            }

            box.IsAlive = false;
            box.IsBlocking = false;
            state.SetViewDirty();
            Debug.Log($"[PushRule] ExecuteBreak 완료: boxId={boxId}, IsBreaking=true");
        }
    }
}