using MyGame2.Stage;

// 부쉬 컴포넌트 데이터.
// 캐릭터가 위에 위치하면 감시자로부터 은신.
// 감시자는 부쉬를 통과할 수 없다.

public struct BushData : IComponentData
{
    // 현재 캐릭터가 은신 중인지
    public bool IsOccupied;

    // 은신 중인 캐릭터 EntityId
    public int OccupantId;

    public BushData(bool isOccupied = false)
    {
        IsOccupied = isOccupied;
        OccupantId = -1;
    }
}