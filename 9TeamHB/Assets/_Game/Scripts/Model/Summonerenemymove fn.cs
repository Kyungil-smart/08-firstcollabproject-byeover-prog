using MyGame2.Stage;
using UnityEngine;

[CreateAssetMenu(fileName = "SummonerEnemyMove_Fn", menuName = "Scriptable Objects/EntityFunction/SummonerEnemyMove_Fn")]
public class SummonerEnemyMove_Fn : EntityFunctionSO
{
    [Header("이동 설정")]
    [SerializeField] private float moveInterval = 0.2f;
    [SerializeField] private float alertDuration = 0.5f;

    [Header("순찰 설정")]
    [Tooltip("스폰 위치 기준 순찰 반경 (맨해튼 거리). 이 범위를 넘으면 이동하지 않음")]
    [SerializeField] private int patrolRadius = 4;

    [Header("소환 설정")]
    [SerializeField] private EntitySO chaserDefinition;

    [Header("참조 주입")]
    [SerializeField] private StageStateReferenceSO stageStateReference;
    [SerializeField] private FloatEventChannelSO onUpdateEvent;

    public float MoveInterval => moveInterval;
    public float AlertDuration => alertDuration;
    public int PatrolRadius => patrolRadius;
    public EntitySO ChaserDefinition => chaserDefinition;

    public override IComponentData CreateComponent(EntityState entity)
    {
        return new SummonerEnemyMoveComponent(this, stageStateReference, entity, onUpdateEvent);
    }
}