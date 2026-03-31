using UnityEngine;

namespace MyGame2.Stage
{
    public sealed class MovementRule
    {
        private readonly PushRule _pushRule;

        public MovementRule(PushRule pushRule)
        {
            _pushRule = pushRule;
        }

        public MoveResult TryMove(StageState state, int moverId, Direction direction)
        {
            if (direction == Direction.None)
                return MoveResult.Blocked(moverId, new GridPos(0, 0), new GridPos(0, 0),
                    MoveBlockReason.InvalidDirection);

            if (!state.TryGetEntity(moverId, out EntityState mover))
                return MoveResult.Blocked(moverId, new GridPos(0, 0), new GridPos(0, 0),
                    MoveBlockReason.DeadEntity);

            GridPos from = mover.Position;
            if (!mover.IsAlive)
                return MoveResult.Blocked(moverId, from, from, MoveBlockReason.DeadEntity);

            GridPos target = from.Move(direction);
            if (!state.IsInside(target))
                return MoveResult.Blocked(moverId, from, target, MoveBlockReason.OutOfBounds);

            CellData cell = state.GetCell(target);
            if (cell.IsBlocked)
            {
                if (cell.IsClosedDoor && mover.IsPlayer &&
                    mover.Has<PocketData>() && (mover.Get<PocketData>().HasKey))
                {
                    return MoveResult.OpenDoor(moverId, from, target);
                }
                return MoveResult.Blocked(moverId, from, target, MoveBlockReason.BlockedByWall);
            }

            // 부쉬: 감시자/적은 진입 불가, 플레이어만 가능
            if (cell.HasBush && !mover.IsPlayer)
                return MoveResult.Blocked(moverId, from, target, MoveBlockReason.BlockedByWall);
            

            if (cell.IsOccupied)
            { 
                if(state.TryGetEntity(cell.OccupantId, out EntityState occupant) && occupant.IsAlive)
                {
                     // 적이 플레이어로 이동할 때
                     if (mover.IsLethalMover && occupant.IsPlayer)
                         return MoveResult.ContactKill(moverId, occupant.Id, from, target);
 
                     // 밀 수 있는 상자로 이동할 때
                     if (mover.IsPlayer && occupant.IsPushable)
                     {
                         if (_pushRule.CanPush(state, moverId, occupant.Id, direction))
                             return MoveResult.PushAndMove(moverId, occupant.Id, from, target);
                     }
                     
                     // 획득 가능한 엔티티로 이동할 때
                     if (mover.IsPlayer && occupant.Has<Pickable>())
                     {
                         if(!mover.Has<PocketData>()) // 포켓 없으면 블럭처럼 막힘
                             return MoveResult.Blocked(moverId, from, target, MoveBlockReason.BlockedByEntity);
                         mover.Get<PocketData>().PickUp(state, occupant);
                         return MoveResult.Success(moverId, from, target);
                     }
                }
                Debug.Log("Blocked by entity");
                return MoveResult.Blocked(moverId, from, target, MoveBlockReason.BlockedByEntity);
            }
            
            // 텔레포트 스팟 이동
            if (cell.HasTeleport && mover.CanTeleport)
            {
                if (state.TryGetCellPair(target, out GridPos pair) && 
                    !state.GetCell(pair).IsOccupied) // 텔레포트 가능
                {
                    mover.Get<Teleportable>().IsTeleporting = true;
                    return MoveResult.Success(moverId, from, pair);
                }
                //텔레포트 불가능 시 일반 이동 v
            }

            return MoveResult.Success(moverId, from, target);
        }
    }
}