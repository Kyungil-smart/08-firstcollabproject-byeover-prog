using System.Collections.Generic;
using MyGame2.Stage;
using UnityEngine;

[CreateAssetMenu(fileName = "PocketCmp", menuName = "Scriptable Objects/EntityFunction/PocketCmp")]
public class PocketCmp: EntityFunctionSO
{
    [Header("열쇠 추종자 정보")] 
    [SerializeField] private KeyFollower _prefab;
    
    public override IComponentData CreateComponent(EntityState owner)
    {
        return new PocketData(owner, _prefab);
    }
}
