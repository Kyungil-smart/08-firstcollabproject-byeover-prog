using MyGame2.Stage;

// 추격 컴포넌트 데이터.
// 추격 감시자용. 소환 후 캐릭터를 최단 경로로 추격.

public struct ChaseData : IComponentData
{
    // 추격 속도 (초/1unit)
    public float ChaseSpeed;

    // 부쉬 진입 시 추격 중지 → 탐색 모드
    public bool StopOnBushEntry;

    // 탐색 모드 지속 시간 (초)
    public float SearchDuration;

    // 경로가 오브젝트로 막히면 소멸
    public bool DestroyOnPathBlocked;

    // 함정 타일 밟으면 소멸
    public bool DestroyOnTrap;

    // 추격 중 Undo 시스템 비활성화
    public bool DisableUndoDuringChase;

    // 추격 대상 캐릭터 EntityId
    public int TargetPlayerId;

    // 탐색 모드 중인지
    public bool IsSearching;

    // 탐색 모드 경과 시간
    public float SearchElapsed;

    public ChaseData(float chaseSpeed, bool stopOnBushEntry, float searchDuration,
        bool destroyOnPathBlocked, bool destroyOnTrap, bool disableUndoDuringChase)
    {
        ChaseSpeed = chaseSpeed;
        StopOnBushEntry = stopOnBushEntry;
        SearchDuration = searchDuration;
        DestroyOnPathBlocked = destroyOnPathBlocked;
        DestroyOnTrap = destroyOnTrap;
        DisableUndoDuringChase = disableUndoDuringChase;
        TargetPlayerId = -1;
        IsSearching = false;
        SearchElapsed = 0f;
    }
}