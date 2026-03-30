using System.Collections.Generic;

namespace MyGame2.Stage
{
    // 이동형 감시자 공용 3×3 감시범위 판정.

    public sealed class DetectionArea3x3
    {
        public bool TryDetect(StageState state, GridPos center, Direction facing,
            bool ignoreObjects, out int detectedPlayerId)
        {
            detectedPlayerId = StageState.InvalidEntityId;

            // 자기 위치 중심 3×3 (총 9칸)
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    GridPos cell = new GridPos(center.X + dx, center.Y + dy);

                    if (!state.IsInside(cell)) continue;

                    CellData cellData = state.GetCell(cell);
                    if (cellData.HasWall) continue;
                    if (cellData.HasBush) continue;

                    if (cellData.IsOccupied &&
                        state.TryGetEntity(cellData.OccupantId, out EntityState occupant) &&
                        occupant.IsPlayer && occupant.IsAlive)
                    {
                        detectedPlayerId = occupant.Id;
                        return true;
                    }
                }
            }

            return false;
        }

        public List<GridPos> CollectDetectionCells(StageState state, GridPos center,
            Direction facing, bool ignoreObjects)
        {
            var result = new List<GridPos>(9);

            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    GridPos cell = new GridPos(center.X + dx, center.Y + dy);

                    if (!state.IsInside(cell)) continue;
                    if (state.GetCell(cell).HasWall) continue;

                    result.Add(cell);
                }
            }

            return result;
        }
    }
}