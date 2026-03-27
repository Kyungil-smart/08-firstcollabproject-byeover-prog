using System.Collections.Generic;
using MyGame2.Stage;
using UnityEngine;

[CreateAssetMenu(fileName = "PatrolCmp", menuName = "Scriptable Objects/EntityFunction/PatrolCmp")]
public class PatrolCmp: EntityFunctionSO
{
    public override IComponentData CreateComponent(EntityState owner)
    {
        return new PlayerData();
    }
}
