using System.Collections.Generic;
using MyGame2.Stage;
using UnityEngine;

[CreateAssetMenu(fileName = "Detect_Fn", menuName = "Scriptable Objects/EntityFunction/Detect_Fn")]
public class Detect_Fn: EntityFunctionSO
{
    
    /// 해당 GridPos에 DetectionBlocker 속성이 있는지 확인 후 결과 반환
    private bool CellBlocked(StageState state, GridPos pos)
    {
        int id = state.GetCell(pos).OccupantId;
        if (state.TryGetEntity(id, out EntityState entity))
        {
            if (entity.Entity.GetEntityFunc("DetectionBlocker") != null)
                return true;
        }
        return false;
        // 해당 속성의 엔티티가 없거나 점유가 안되어 있다면 false 반환
    }
}
