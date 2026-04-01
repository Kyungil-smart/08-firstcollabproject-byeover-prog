using MyGame2.Stage;

// 부서지는 상자 컴포넌트 데이터.
// 막다른 방향에서 한 번 더 밀면 파괴된다.

public struct BreakableData : IComponentData
{
    // 활성화된 캐릭터가 직접 밀 때만 파괴 가능
    public bool OnlyByActivePlayer;

    // 현재 막힌 상태인지 (다음 Push 시 파괴 판정용)
    public bool IsBlocked;

    public BreakableData(bool onlyByActivePlayer)
    {
        OnlyByActivePlayer = onlyByActivePlayer;
        IsBlocked = false;
    }
}