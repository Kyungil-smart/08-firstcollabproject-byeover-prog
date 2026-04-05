using System;
using System.Collections.Generic;

namespace MyGame2.Stage
{
    // 한 턴의 스테이지 상태를 통째로 저장하는 스냅샷.
    // StageState.Restore(snapshot)가 요구하는 구조 그대로 맞춤.

    public sealed class StageSnapshot
    {
        public CellData[] Cells;
        public Dictionary<int, EntityState> EntityDict;
        public Dictionary<int, List<KeyFollower>> KeysDict;
        public int ActivePlayerId;
        public int TurnIndex;
        public bool IsGameOver;
        public bool IsStageClear;
        public bool IsViewDirty;

        // 캡처

        public static StageSnapshot Capture(StageState state)
        {
            var snap = new StageSnapshot();

            // 셀 딥카피 (CellData는 struct)
            snap.Cells = (CellData[])state.Cells.Clone();

            // 스칼라 상태
            snap.ActivePlayerId = state.ActivePlayerId;
            snap.TurnIndex      = state.TurnIndex;
            snap.IsGameOver     = state.IsGameOver;
            snap.IsStageClear   = state.IsStageClear;
            snap.IsViewDirty    = state.IsViewDirty;

            // 엔티티 딥카피
            snap.EntityDict = new Dictionary<int, EntityState>(state.EntityDict.Count);
            snap.KeysDict   = new Dictionary<int, List<KeyFollower>>();

            foreach (var kvp in state.EntityDict)
            {
                snap.EntityDict[kvp.Key] = CloneEntity(kvp.Value);

                // PocketData의 Keys는 별도 저장 (Restore에서 특수 처리)
                if (kvp.Value.Has<PocketData>())
                {
                    PocketData pocket = kvp.Value.Get<PocketData>();
                    snap.KeysDict[kvp.Key] = new List<KeyFollower>(pocket.Keys);
                }
            }

            return snap;
        }

        // 엔티티 클론

        private static EntityState CloneEntity(EntityState src)
        {
            // 빈 엔티티 생성 후 필드 덮어쓰기
            var e = EntityState.CreateAnimal(src.Position, src.Facing);
            e.Id                = src.Id;
            e.Kind              = src.Kind;
            e.SpawnPosition     = src.SpawnPosition;
            e.IsAlive           = src.IsAlive;
            e.IsBlocking        = src.IsBlocking;
            e.BlocksCameraSight = src.BlocksCameraSight;
            e.Definition        = src.Definition;

            // 컴포넌트 복사
            // struct 타입: boxing 시 자동 딥카피
            // IDisposable(IUpdate 등): 라이브 참조를 들고 있으므로 스킵
            // ->Restore 시 existing.RestoreFrom()이 기존 라이브 컴포넌트를 유지함
            foreach (var comp in src.Components)
            {
                if (comp is IDisposable) continue;
                e.Set(comp);
            }

            return e;
        }
    }
}