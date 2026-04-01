using MyGame2.Stage;
using UnityEngine;

[CreateAssetMenu(fileName = "SummonerEnemyMove_Fn", menuName = "Scriptable Objects/EntityFunction/SummonerEnemyMove_Fn")]
public class SummonerEnemyMove_Fn : EntityFunctionSO
{
    [Header("이동 설정")]
    [Tooltip("순찰 이동 간격 (초)")]
    [SerializeField] private float moveInterval = 0.2f;

    [Tooltip("적발 후 정지 시간 (초)")]
    [SerializeField] private float alertDuration = 0.5f;

    [Header("순찰 설정")]
    [Tooltip("스폰 위치 기준 순찰 사각형 반경 (칸). 4면 9x9 사각형 둘레를 순찰")]
    [SerializeField] private int patrolRadius = 4;

    [Header("소환 설정")]
    [Tooltip("소환할 추격 감시자의 EntitySO")]
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