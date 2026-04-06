using MyGame2.Stage;
using UnityEngine;

[CreateAssetMenu(fileName = "AnimalEnemyMove_Fn", menuName = "Scriptable Objects/EntityFunction/AnimalEnemyMove_Fn")]
public class AnimalEnemyMove_Fn : EntityFunctionSO
{
    [Header("이동 설정")]
    [SerializeField] private float moveInterval = 0.2f;
    [SerializeField] private float alertDuration = 0.5f;
    [SerializeField] private float chaseSpeedMultiplier = 2f;
    [SerializeField] private float lostSearchDuration = 0.5f;

    [Header("참조 주입")]
    [SerializeField] private StageStateReferenceSO stageStateReference;
    [SerializeField] private FloatEventChannelSO onUpdateEvent;

    public float MoveInterval => moveInterval;
    public float AlertDuration => alertDuration;
    public float ChaseSpeedMultiplier => chaseSpeedMultiplier;
    public float LostSearchDuration => lostSearchDuration;

    public override IComponentData CreateComponent(EntityState entity)
    {
        return new AnimalMove(this, stageStateReference, entity, onUpdateEvent);
    }
}