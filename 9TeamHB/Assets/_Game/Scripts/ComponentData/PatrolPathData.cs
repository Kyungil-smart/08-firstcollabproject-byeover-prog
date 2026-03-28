using MyGame2.Stage;

// 경로 기반 순찰 컴포넌트 데이터.
// 새 감시자A/B용. 기존 PatrolData(로봇/동물)와 구분.
// 루프 경로면 시계방향, 비루프면 왕복 이동.

public struct PatrolPathData : IComponentData
{
    // 루프 경로 시 시계방향 순찰
    public bool LoopClockwise;

    // 비루프 경로 시 왕복 이동
    public bool CanPingPong;

    // 오브젝트를 무시하고 감시 가능 (벽, 부쉬만 차단)
    public bool IgnoreObjectSight;

    // 함정/투사체에 영향 없음
    public bool IgnoreTrap;

    // 경로 상 현재 인덱스
    public int CurrentPathIndex;

    // 왕복 시 진행 방향 (1: 정방향, -1: 역방향)
    public int PathDirection;

    public PatrolPathData(bool loopClockwise, bool canPingPong,
        bool ignoreObjectSight, bool ignoreTrap)
    {
        LoopClockwise = loopClockwise;
        CanPingPong = canPingPong;
        IgnoreObjectSight = ignoreObjectSight;
        IgnoreTrap = ignoreTrap;
        CurrentPathIndex = 0;
        PathDirection = 1;
    }
}