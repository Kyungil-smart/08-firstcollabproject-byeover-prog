using System.Collections.Generic;
using MyGame2.Stage;
using UnityEngine;

[CreateAssetMenu(fileName = "Poo", menuName = "Scriptable Objects/EntityFunction/Poo")]
public class Po: EntityFunctionSO
{
    public override IComponentData CreateComponent(EntityState owner)
    {
        throw new System.NotImplementedException();
    }
}
