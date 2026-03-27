using System.Collections.Generic;
using MyGame2.Stage;
using UnityEngine;

[CreateAssetMenu(fileName = "PushableCmp", menuName = "Scriptable Objects/EntityFunction/PushableCmp")]
public class PushableCmp : EntityFunctionSO
{
    // --- 설정 ---
    [Tooltip("플레이어가 밀 수 있는 오브젝트인지 여부")][SerializeField] private bool _canBePushed; 
    public override IComponentData CreateComponent(EntityState owner)
    { 
        return new Pushable(_canBePushed);
    } 
}
