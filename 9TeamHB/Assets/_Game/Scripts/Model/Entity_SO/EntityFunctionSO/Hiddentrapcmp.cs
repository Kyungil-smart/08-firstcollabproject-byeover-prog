using MyGame2.Stage;
using UnityEngine;

// 히든 함정 마커 엔티티의 기능 컴포넌트 SO.
// EntitySO의 Functions 리스트에 추가하면
// 엔티티 생성 시 HiddenTrapData가 자동 부착된다.

[CreateAssetMenu(
    fileName = "HiddenTrapCmp",
    menuName = "Scriptable Objects/EntityFunction/HiddenTrapCmp")]
public class HiddenTrapCmp : EntityFunctionSO
{
    public override IComponentData CreateComponent(EntityState owner)
    {
        return new HiddenTrapData(true);
    }
}