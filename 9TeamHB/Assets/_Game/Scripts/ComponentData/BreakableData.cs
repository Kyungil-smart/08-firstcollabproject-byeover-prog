using MyGame2.Stage;

// 부서지는 상자 컴포넌트 데이터.
// 막다른 방향에서 한 번 더 밀면 파괴된다.
// 틈새 위의 부서지는 상자는 플레이어가 밟고 → 벗어날 때 파괴된다.

public struct BreakableData : IComponentData
{
    public bool OnlyByActivePlayer;
    public bool IsBlocked;
    public bool IsBreaking;   // 파괴 애니메이션 트리거 (TurnSystem이 설정)
    public bool IsStepped;    // 플레이어가 현재 위에 올라서 있는 상태

    public BreakableData(bool onlyByActivePlayer)
    {
        OnlyByActivePlayer = onlyByActivePlayer;
        IsBlocked = false;
        IsBreaking = false;
        IsStepped = false;
    }
}