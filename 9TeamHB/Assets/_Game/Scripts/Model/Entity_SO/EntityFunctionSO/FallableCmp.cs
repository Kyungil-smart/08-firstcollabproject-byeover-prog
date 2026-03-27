using System.Collections.Generic;
using MyGame2.Stage;
using UnityEngine;

[CreateAssetMenu(fileName = "FallableCmp", menuName = "Scriptable Objects/EntityFunction/FallableCmp")]
public class FallableCmp: EntityFunctionSO
{
    //--- 설정---
    [SerializeField] private float _fallDuration;
    [SerializeField] private float _fallDistance;
    
    public override IComponentData CreateComponent(EntityState owner)
    {
        return new Fallable(_fallDuration, _fallDistance);
    }
}
