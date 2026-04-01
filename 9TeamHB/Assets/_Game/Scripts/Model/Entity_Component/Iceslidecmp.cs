using MyGame2.Stage;
using UnityEngine;

// 얼음 미끄러짐 기능 SO.
// 이 기능이 부착된 상자는 밀었을 때 벽이나 오브젝트에 닿을 때까지 미끄러진다.

[CreateAssetMenu(fileName = "IceSlide_Cmp", menuName = "Scriptable Objects/EntityCmp/IceSlideCmp")]
public class IceSlideCmp : EntityFunctionSO
{
    public override IComponentData CreateComponent(EntityState owner)
    {
        return new IceSlideData(false, Direction.None);
    }
}