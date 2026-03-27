using System;

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


        // 엔티티가 점유 중인가?
        public bool IsOccupied
        {
            get { return OccupantId != EmptyOccupantId; }
        }
    }
}