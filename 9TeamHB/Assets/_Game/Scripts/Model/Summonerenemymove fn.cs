using MyGame2.Stage;
using UnityEngine;

[CreateAssetMenu(fileName = "SummonerEnemyMove_Fn", menuName = "Scriptable Objects/EntityFunction/SummonerEnemyMove_Fn")]
public class SummonerEnemyMove_Fn : EntityFunctionSO
{
    [Header("이동 설정")]
    [SerializeField] private float moveInterval = 0.2f;
    [SerializeField] private float alertDuration = 0.5f;

    [Header("소환 설정")]
    [SerializeField] private EntitySO chaserDefinition;

    [Header("참조 주입")]
    [SerializeField] private StageStateReferenceSO stageStateReference;
    [SerializeField] private FloatEventChannelSO onUpdateEvent;

    public float MoveInterval => moveInterval;
    public float AlertDuration => alertDuration;
    public EntitySO ChaserDefinition => chaserDefinition;

    public override IComponentData CreateComponent(EntityState entity)
    {
        return new SummonerEnemyMoveComponent(this, stageStateReference, entity, onUpdateEvent);
    }
}