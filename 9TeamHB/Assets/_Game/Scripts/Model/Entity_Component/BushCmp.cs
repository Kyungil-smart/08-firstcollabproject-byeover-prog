using MyGame2.Stage;
using UnityEngine;

// 부쉬 기능 SO.
// 캐릭터 은신 처리 및 감시자 통과 차단.

[CreateAssetMenu(fileName = "Bush_Cmp", menuName = "Scriptable Objects/EntityCmp/BushCmp")]
public class BushCmp : EntityFunctionSO
{
    public override IComponentData CreateComponent(EntityState owner)
    {
        return new BushData(false);
    }
}