using System;

namespace MyGame2.Stage
{
    [Flags]
    public enum CellFlags
    {
        None = 0,
        Wall = 1 << 0,
        Goal = 1 << 1,
        Trap = 1 << 2,
        Bush = 1 << 3,
        Crack = 1 << 4,
        Teleport = 1 << 5,
        Active = 1 << 6,
        Door = 1 << 7,
        Button  = 1 << 8,
        Sticky = 1 << 9,
        OpenFixed = 1 << 10,
        HiddenTrap = 1 << 11,
        // 파괴 함정 — 상자를 부수는 함정
        DestroyTrap = 1 << 12,

        Switch = Button | Sticky
    }
}