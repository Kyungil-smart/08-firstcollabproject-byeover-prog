using System.Collections.Generic;
using MyGame2.Stage;
using UnityEngine;

[CreateAssetMenu(fileName = "PickableCmp", menuName = "Scriptable Objects/EntityFunction/PickableCmp")]
public class PickableCmp : EntityFunctionSO
{
    public override IComponentData CreateComponent(EntityState owner)
    {
        return new Pickable();
    }
}
