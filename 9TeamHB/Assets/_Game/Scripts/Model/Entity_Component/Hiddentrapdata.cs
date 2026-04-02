using MyGame2.Stage;

// 히든 함정 엔티티 전용 데이터.
// 이 컴포넌트가 부착된 엔티티는 페어링된 스위치/레버에 의해
// 비활성화될 수 있는 히든 함정이다.

public class HiddenTrapData : IComponentData
{
    public bool IsActive;

    public HiddenTrapData(bool isActive = true)
    {
        IsActive = isActive;
    }
}