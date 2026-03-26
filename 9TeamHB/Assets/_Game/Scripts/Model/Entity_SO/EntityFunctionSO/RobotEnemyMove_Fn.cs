using MyGame2.Stage;
using UnityEngine;
 
[CreateAssetMenu(fileName = "RobotEnemyMove_Fn", menuName = "Scriptable Objects/EntityFunction/RobotEnemyMove_Fn")]
public class RobotEnemyMove_Fn : EntityFunctionSO
{
    
    // --- 설정 ---
    [Header("이동 설정")]
    [SerializeField] private float moveInterval = 0.2f;
    [SerializeField] private float alertDuration = 0.5f;
    [SerializeField] private float chaseSpeedMultiplier = 2f;
    
    // --- 참조 주입 ---
    [SerializeField] StageStateReferenceSO stageStateReference;
    [SerializeField] private FloatEventChannelSO onUpdateEvent;

    // --- 프로퍼티로 설정값 노출 ---
    public float MoveInterval => moveInterval;
    public float AlertDuration => alertDuration;
    public float ChaseSpeedMultiplier => chaseSpeedMultiplier;

    // --- 팩토리 메서드 ---
    // 자신을 기반으로 런타임 컴포넌트(로직+상태)를 생성
    public override IComponentData CreateComponent(EntityState entity)
    {
        return new RobotEnemyMoveComponent(this, stageStateReference.Instance, entity, onUpdateEvent);
    }
}
