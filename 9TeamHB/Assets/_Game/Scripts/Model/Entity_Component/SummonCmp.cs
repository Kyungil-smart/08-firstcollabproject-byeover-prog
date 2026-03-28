using MyGame2.Stage;
using UnityEngine;

// 소환 기능 SO.
// 새 감시자B용. 적발 시 캐릭터 좌표에 추격 감시자를 소환.

[CreateAssetMenu(fileName = "Summon_Cmp", menuName = "Scriptable Objects/EntityCmp/SummonCmp")]
public class SummonCmp : EntityFunctionSO
{
    [Tooltip("소환 딜레이 (초)")]
    [SerializeField] private float _summonDelay = 0.5f;

    [Tooltip("추격자 활동 중이면 이동/추가소환 정지")]
    [SerializeField] private bool _stopWhileSummonActive = true;

    [Tooltip("이 감시자 자체는 접촉 피해 없음 (소환만 함)")]
    [SerializeField] private bool _noContactDamage = true;

    public override IComponentData CreateComponent(EntityState owner)
    {
        return new SummonData(_summonDelay, _stopWhileSummonActive, _noContactDamage);
    }
}