using System.Collections.Generic;
using MyGame2.Stage;
using UnityEngine;

[CreateAssetMenu(fileName = "SummonerMoveCmp", menuName = "Scriptable Objects/EntityFunction/SummonerMoveCmp")]
public class SummonerMoveCmp: EntityFunctionSO
{
    [Header("참조 주입")]
    [SerializeField] StageStateReferenceSO _stageStateReference;
    [SerializeField] FloatEventChannelSO _moveEventChannel;
    
    [Header("이동 설정")]
    [Tooltip("일반 순찰 이동 간격 (초)")]
    [SerializeField] private float _moveInterval;
    [Tooltip("감지 후 정지 시간 (초)")]
    [SerializeField] private float _alertDuration;
    [Header("추격자")]
    [SerializeField] EntitySO _chaserDefinition;
    
    public override IComponentData CreateComponent(EntityState owner)
    {
        return new SummonerMove(_stageStateReference, owner, _moveEventChannel, _moveInterval, _alertDuration,
            _chaserDefinition);
    }
}
