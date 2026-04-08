using System.Collections.Generic;

namespace MyGame2.Stage
{
    // 한 턴의 StageState 스냅샷.
    // StageState.Restore(snapshot)에 전달하면 해당 시점으로 복원된다.
 
    public sealed class StageSnapshot
    {
        public CellData[] Cells { get; private set; }
        public Dictionary<int, EntityState> EntityDict { get; private set; }
        public Dictionary<int, int> KeysDict { get; private set; }
        public int ActivePlayerId { get; private set; }
        public int TurnIndex { get; private set; }
        public bool IsGameOver { get; private set; }
        public bool IsStageClear { get; private set; }
        public bool IsViewDirty { get; private set; }

        // 현재 StageState를 복사하여 스냅샷 생성.
        public static StageSnapshot Capture(StageState state)
        {
            if (state == null) return null;

            // 셀 배열 복사 (CellData는 struct이므로 Clone으로 값 복사)
            CellData[] cellsCopy = (CellData[])state.Cells.Clone();

            // 엔티티: EntityState.CopyFrom()으로 독립 복사
            var entityCopy = new Dictionary<int, EntityState>(state.EntityDict.Count);
            var keysCopy = new Dictionary<int, int>(4);

            foreach (var kvp in state.EntityDict)
            {
                entityCopy[kvp.Key] = kvp.Value.CopyFrom();

                // PocketData 키 목록 별도 보관
                if (kvp.Value.Has<PocketData>())
                {
                    PocketData pocket = kvp.Value.Get<PocketData>();
                    keysCopy[kvp.Key] = pocket.Keys.Count;
                }
            }

            return new StageSnapshot
            {
                Cells = cellsCopy,
                EntityDict = entityCopy,
                KeysDict = keysCopy,
                ActivePlayerId = state.ActivePlayerId,
                TurnIndex = state.TurnIndex,
                IsGameOver = state.IsGameOver,
                IsStageClear = state.IsStageClear,
                IsViewDirty = state.IsViewDirty
            };
        }
    }
}