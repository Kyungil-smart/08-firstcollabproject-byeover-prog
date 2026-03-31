using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace MyGame2.Stage
{
    // 그리드 셀 하나의 데이터.
    // Flags로 벽/골/함정/부쉬 속성을 표현하고,
    // OccupantId로 해당 셀에 서 있는 엔티티를 추적한다.
    [Serializable]
    public struct CellData
    {
        public const int EmptyOccupantId = -1;

        public CellFlags Flags;
        public int OccupantId;

        

        // 벽인가? (이동 불가, 시야 차단)
        public bool HasWall
        {
            get { return (Flags & CellFlags.Wall) != 0; }
        }

        // 골 지점인가?
        public bool HasGoal
        {
            get { return (Flags & CellFlags.Goal) != 0; }
        }

        // 활성 함정인가? (상자로 덮이면 이 플래그가 해제됨)
        public bool HasTrap
        {
            get { return (Flags & CellFlags.Trap) != 0; }
        }

        // 부쉬인가? (플레이어 은폐, 감시자 통과 불가)
        public bool HasBush
        {
            get { return (Flags & CellFlags.Bush) != 0; }
        }
        
        // 틈새 타일이 이동 가능한가?
        public bool HasCrack
        {
            get { return (Flags & CellFlags.Crack) != 0; }
        }
        
        // 텔레포트 타일인가?
        public bool HasTeleport
        {
            get { return (Flags & CellFlags.Teleport) != 0; }
        }
        // 문 타일인가?
        public bool HasDoor  // 문인가?
        {
            get { return (Flags & CellFlags.Door) != 0; }
        }
        public bool IsOpenedDoor // 열린 문인가?
        {
            get { return HasDoor && HasActive; }
        }
        public bool IsClosedDoor // 닫힌 문인가?
        {
            get { return HasDoor && !HasActive; }
        }

        // 신호를 보내는 타일인가?
        public bool HasSignalButton
        {
            get { return (Flags & CellFlags.Button) != 0; }
        }

        // 점유가 해제 되어도 신호가 유지되어야 하는가?
        public bool IsSticky
        {
            get { return (Flags & CellFlags.Sticky) != 0; }
        }
        
        // 타일이 이동 불가능하게 막혀 있는가( 점유 상관없이 타일 속성 자체로 막혀있는가)
        // 이동을 막을 수 없는 Flags라면 false 바로 반환, 이후 활성 여부에 따라 판단
        public bool IsBlocked => HasBlockCandidate && (HasWall || !HasActive);

        public bool HasActive => (Flags & CellFlags.Active) != 0;
        public bool IsOpenFixed=> (Flags & CellFlags.OpenFixed) != 0;

        // 이동을 막을 수 있는 Flags 체크
        public bool HasBlockCandidate => HasWall || HasCrack || HasDoor;


        // 엔티티가 점유 중인가?
        public bool IsOccupied
        {
            get { return OccupantId != EmptyOccupantId; }
        }
    }
}