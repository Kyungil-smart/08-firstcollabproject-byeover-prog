using System.Collections.Generic;
using MyGame2.Stage;
using UnityEngine;

[CreateAssetMenu(fileName = "TelePortableCmp", menuName = "Scriptable Objects/EntityFunction/TelePortableCmp")]
public class TelePortableCmp: EntityFunctionSO
{
    public override IComponentData CreateComponent(EntityState owner)
    {
        return new Teleportable(owner);
    }
}
