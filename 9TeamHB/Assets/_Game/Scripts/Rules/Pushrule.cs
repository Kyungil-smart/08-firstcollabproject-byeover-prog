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
            if(state.HasCrackNotCovered(dest))
                state.SetCrackMovable(dest, boxId);

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
        // 부숴지는 상자를 위해 추가한 내용

        // 막힌 상태에서 부서져야 하는지 판정하는 함수
        public bool ShouldBreak(StageState state, int pusherId, int boxId, Direction direction)
        {
            if (!state.TryGetEntity(pusherId, out EntityState pusher)) return false;
            if (!state.TryGetEntity(boxId, out EntityState box)) return false;

            // 1. 오직 플레이어가 밀 때만 파괴
            if (!pusher.IsPlayer) return false;

            // 2. 부서지는 상자 타입인지 확인 
            //if (box.Get<BoxData>().Ownership != BoxType.Breakable) return false;


            // [임시 테스트용 코드] 녹색 공용 상자(Shared)를 부서지는 상자로 취급해라!
            if (box.Get<BoxData>().Ownership != BoxType.Shared) return false;


            // 3. 진행 방향이 막혀있는지 확인
            GridPos dest = box.Position.Move(direction);
            bool isBlocked = !state.IsInside(dest) || state.GetCell(dest).HasWall || state.GetCell(dest).IsOccupied;

            return isBlocked;
        }

        // 부숴지는 상자를 파괴하고 맵에서 지우는 함수
        public void ExecuteBreak(StageState state, int boxId)
        {
            if (state.TryGetEntity(boxId, out EntityState box))
            {
                box.IsAlive = false; // 생명줄을 끊으면 GridEntityView가 화면에서 지워줍니다.
                Debug.Log($"[PushRule] 부숴지는 상자 파괴");
            }
        }
    }
    
}