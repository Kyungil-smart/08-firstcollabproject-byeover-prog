using System;

namespace MyGame2.Stage
{
    // 셀의 속성 비트 플래그.
    // 여러 속성을 동시에 가질 수 있다 (예: Goal | Trap은 불가하지만 확장성 확보).
    
    [Flags]
    public enum CellFlags
    {
        None = 0,

        // 벽 — 이동 불가, 카메라 시야 차단
        Wall = 1 << 0,

        // 골 — 두 플레이어 모두 서면 클리어
        Goal = 1 << 1,

        // 함정 — 플레이어가 밟으면 즉사, 상자로 덮으면 비활성화
        Trap = 1 << 2,

        // 부쉬 — 플레이어가 위에 있으면 감시자에게 감지되지 않음
        // 감시자는 이 타일을 지나갈 수 없음
        Bush = 1 << 3
    }
}