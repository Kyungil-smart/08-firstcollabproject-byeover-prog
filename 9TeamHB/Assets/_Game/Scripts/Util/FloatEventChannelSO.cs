using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "FloatEventChannelSO", menuName = "Scriptable Objects/EventChannel/FloatEventChannelSO")]
public class FloatEventChannelSO : ScriptableObject
{
    /// 이벤트 구독시 결합을 약하게 하기위한 이벤트 중계 목적의 SO입니다
    /// 이벤트를 호출, 구독할 클래스에서 모두 참조로 공통된 SO를 가지고 있어야합니다
    public UnityAction<float> OnEventRaised;
    public UnityAction<int> OnAlertAndChaseRaised;
    
    // 이벤트 발행
    public void RaiseEvent(float value)
    {
        OnEventRaised?.Invoke(value);
    }
    public void RaiseAlertAndChase(int playerId)
    {
        OnAlertAndChaseRaised?.Invoke(playerId);
    }
    
    // 이벤트 구독 일괄 해제
    public void Clear()
    {
        OnEventRaised = null;
        OnAlertAndChaseRaised = null;
    }
}
