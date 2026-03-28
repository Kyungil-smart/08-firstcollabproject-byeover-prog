using MyGame2.Stage;

// 소환 컴포넌트 데이터.
// 새 감시자B용. 적발 시 캐릭터 좌표에 추격 감시자를 소환.

public struct SummonData : IComponentData
{
    // 소환 딜레이 (초)
    public float SummonDelay;

    // 소환된 추격자가 활동 중이면 이동/추가소환 정지
    public bool StopWhileSummonActive;

    // 직접 접촉 피해 없음
    public bool NoContactDamage;

    // 현재 소환된 추격자가 활동 중인지
    public bool IsSummonActive;

    // 소환된 추격자의 EntityId
    public int SummonedChaserId;

    public SummonData(float summonDelay, bool stopWhileSummonActive, bool noContactDamage)
    {
        SummonDelay = summonDelay;
        StopWhileSummonActive = stopWhileSummonActive;
        NoContactDamage = noContactDamage;
        IsSummonActive = false;
        SummonedChaserId = -1;
    }
}