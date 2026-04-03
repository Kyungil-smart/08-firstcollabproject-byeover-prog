using MyGame2.Stage;
using UnityEngine;

[CreateAssetMenu(
    fileName = "FireTrap_Fn",
    menuName = "Scriptable Objects/EntityFunction/FireTrap_Fn")]
public class FireTrap_Fn : EntityFunctionSO
{
    [Header("발사 설정")]
    [Tooltip("발사 주기 (초)")]
    [SerializeField] private float fireInterval = 3.0f;

    [Tooltip("불 지속 시간 (초)")]
    [SerializeField] private float fireDuration = 1.0f;

    [Tooltip("범위 — 발사대 본체 포함 셀 수")]
    [SerializeField] private int range = 3;

    public override IComponentData CreateComponent(EntityState owner)
    {
        return new FireTrapData(fireInterval, fireDuration, range);
    }
}