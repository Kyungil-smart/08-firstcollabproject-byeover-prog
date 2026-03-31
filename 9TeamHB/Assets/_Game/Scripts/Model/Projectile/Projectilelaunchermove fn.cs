using MyGame2.Stage;
using UnityEngine;

[CreateAssetMenu(fileName = "ProjectileLauncherMove_Fn",
    menuName = "Scriptable Objects/EntityFunction/ProjectileLauncherMove_Fn")]
public class ProjectileLauncherMove_Fn : EntityFunctionSO
{
    [Header("발사 설정")]
    [Tooltip("투사체 발사 주기 (초)")]
    [SerializeField] private float fireInterval = 2.0f;

    [Tooltip("소환할 투사체 EntitySO")]
    [SerializeField] private EntitySO projectileDefinition;

    [Tooltip("투사체 이동 간격 (초). 작을수록 빠름")]
    [SerializeField] private float projectileSpeed = 0.1f;

    [Header("참조 주입")]
    [SerializeField] private StageStateReferenceSO stageStateReference;
    [SerializeField] private FloatEventChannelSO onUpdateEvent;

    public float FireInterval => fireInterval;
    public EntitySO ProjectileDefinition => projectileDefinition;
    public float ProjectileSpeed => projectileSpeed;

    public override IComponentData CreateComponent(EntityState entity)
    {
        return new ProjectileLauncherMoveComponent(this, stageStateReference, entity, onUpdateEvent);
    }
}