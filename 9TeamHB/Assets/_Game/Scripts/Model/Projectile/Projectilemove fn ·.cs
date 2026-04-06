using MyGame2.Stage;
using UnityEngine;

// 투사체

[CreateAssetMenu(fileName = "ProjectileMove_Fn",
    menuName = "Scriptable Objects/EntityFunction/ProjectileMove_Fn")]
public class ProjectileMove_Fn : EntityFunctionSO
{
    [Header("이동 설정")]
    [Tooltip("기본 이동 간격 (초). 발사기의 ProjectileSpeed가 우선")]
    [SerializeField] private float defaultMoveInterval = 0.1f;

    [Header("참조 주입")]
    [SerializeField] private StageStateReferenceSO stageStateReference;
    [SerializeField] private FloatEventChannelSO onUpdateEvent;

    public float DefaultMoveInterval => defaultMoveInterval;

    public override IComponentData CreateComponent(EntityState entity)
    {
        return new ProjectileMoveComponent(this, stageStateReference, entity, onUpdateEvent);
    }
}