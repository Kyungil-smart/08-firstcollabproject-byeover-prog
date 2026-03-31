using System.Collections.Generic;
using MyGame2.Stage;
using UnityEngine;

[CreateAssetMenu(fileName = "InteractionPlayerCmp", menuName = "Scriptable Objects/EntityFunction/InteractionPlayerCmp")]
public class InteractionPlayerCmp: EntityFunctionSO
{
    //--- 설정 ---
    [Header("상호작용 가능한 플레이어 유형")]
    [SerializeField] private bool CanA;
    [SerializeField] private bool CanB;
    
    public override IComponentData CreateComponent(EntityState owner)
    {
        return new InteractionTag(CanA, CanB);
    }
}
