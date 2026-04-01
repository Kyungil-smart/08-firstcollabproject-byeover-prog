using MyGame2.Stage;
using UnityEngine;

[CreateAssetMenu(
    fileName = "SawTrap_Cmp",
    menuName = "Scriptable Objects/EntityFunction/SawTrapCmp")]
public sealed class SawTrapCmp : EntityFunctionSO
{
    [Tooltip("톱날이 커버하는 셀 수 (2 또는 5)")]
    [SerializeField] private int size = 2;

    public override IComponentData CreateComponent(EntityState entity)
    {
        return new SawTrapData(size);
    }
}