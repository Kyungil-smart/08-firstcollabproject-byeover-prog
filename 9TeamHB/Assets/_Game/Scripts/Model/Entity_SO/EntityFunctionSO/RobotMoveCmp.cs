using System.Collections.Generic;
using MyGame2.Stage;
using UnityEngine;

[CreateAssetMenu(fileName = "RobotMoveCmp", menuName = "Scriptable Objects/EntityFunction/RobotMoveCmp")]
public class RobotMoveCmp: EntityFunctionSO
{
    [Header("참조 주입")]
    [SerializeField] StageStateReferenceSO _stageStateReference;
    [SerializeField] FloatEventChannelSO _robotMoveEventChannel;
    
    [Header("이동 설정")]
    [Tooltip("일반 순찰 이동 간격 (초)")]
    [SerializeField] private float _moveInterval;
    [Tooltip("감지 후 정지 시간 (초)")]
    [SerializeField] private float _alertDuration;
    [Tooltip("경계 모드 속도 배율")]
    [SerializeField] private float _chaseSpeedMultiplier;
    
    
    public override IComponentData CreateComponent(EntityState owner)
    {
        return new RobotMove(owner, _stageStateReference, _robotMoveEventChannel, _moveInterval, _alertDuration,
            _chaseSpeedMultiplier);
    }
}
