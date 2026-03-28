using MyGame2.Stage;
using UnityEngine;

// 부서지는 상자 기능 SO.
// 막다른 방향(벽, 오브젝트 등)에서 활성화된 캐릭터가 한 번 더 밀면 파괴.

[CreateAssetMenu(fileName = "Breakable_Cmp", menuName = "Scriptable Objects/EntityCmp/BreakableCmp")]
public class BreakableCmp : EntityFunctionSO
{
    [Tooltip("활성화된 캐릭터가 직접 밀 때만 파괴 가능 (일반상자로 밀어서는 파괴 불가)")]
    [SerializeField] private bool _onlyByActivePlayer = true;

    public override IComponentData CreateComponent(EntityState owner)
    {
        return new BreakableData(_onlyByActivePlayer);
    }
}