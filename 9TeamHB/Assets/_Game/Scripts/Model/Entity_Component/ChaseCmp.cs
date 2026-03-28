using MyGame2.Stage;
using UnityEngine;

// 추격 기능 SO.
// 추격 감시자용. 소환 후 캐릭터를 최단 경로로 추격.

[CreateAssetMenu(fileName = "Chase_Cmp", menuName = "Scriptable Objects/EntityCmp/ChaseCmp")]
public class ChaseCmp : EntityFunctionSO
{
    [Tooltip("추격 속도 (초/1unit)")]
    [SerializeField] private float _chaseSpeed = 0.1f;

    [Tooltip("캐릭터 부쉬 진입 시 추격 중지 → 탐색 모드")]
    [SerializeField] private bool _stopOnBushEntry = true;

    [Tooltip("탐색 모드 지속 시간 (초)")]
    [SerializeField] private float _searchDuration = 0.5f;

    [Tooltip("경로가 오브젝트로 완전히 막히면 소멸")]
    [SerializeField] private bool _destroyOnPathBlocked = true;

    [Tooltip("함정 타일 밟으면 소멸")]
    [SerializeField] private bool _destroyOnTrap = true;

    [Tooltip("추격 중 Undo 시스템 비활성화")]
    [SerializeField] private bool _disableUndoDuringChase = true;

    public override IComponentData CreateComponent(EntityState owner)
    {
        return new ChaseData(_chaseSpeed, _stopOnBushEntry, _searchDuration,
            _destroyOnPathBlocked, _destroyOnTrap, _disableUndoDuringChase);
    }
}