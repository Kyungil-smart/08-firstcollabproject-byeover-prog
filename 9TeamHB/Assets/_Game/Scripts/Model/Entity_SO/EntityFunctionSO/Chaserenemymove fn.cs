using MyGame2.Stage;
using UnityEngine;

[CreateAssetMenu(fileName = "ChaserEnemyMove_Fn", menuName = "Scriptable Objects/EntityFunction/ChaserEnemyMove_Fn")]
public class ChaserEnemyMove_Fn : EntityFunctionSO
{
    [Header("이동 설정")]
    [Tooltip("추격 이동 간격 (초)")]
    [SerializeField] private float moveInterval = 0.1f;

    [Tooltip("부쉬 진입 시 탐색 대기 시간 (초)")]
    [SerializeField] private float lostSearchDuration = 0.5f;

    [Header("참조 주입")]
    [SerializeField] private StageStateReferenceSO stageStateReference;
    [SerializeField] private FloatEventChannelSO onUpdateEvent;

    public float MoveInterval => moveInterval;
    public float LostSearchDuration => lostSearchDuration;

    public override IComponentData CreateComponent(EntityState entity)
    {
        return new ChaserEnemyMoveComponent(this, stageStateReference, entity, onUpdateEvent);
    }
}