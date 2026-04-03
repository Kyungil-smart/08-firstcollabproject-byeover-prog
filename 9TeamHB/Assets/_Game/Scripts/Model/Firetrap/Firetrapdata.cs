using MyGame2.Stage;

public class FireTrapData : IComponentData
{
    public float FireInterval;    // 발사 주기 (초)
    public float FireDuration;    // 불 지속 시간 (초)
    public int Range;             // 범위 (발사대 포함 셀 수)

    public float Timer;           // 현재 타이머
    public bool IsActive;         // 현재 불 활성 상태

    public FireTrapData(float fireInterval, float fireDuration, int range)
    {
        FireInterval = fireInterval;
        FireDuration = fireDuration;
        Range = range;
        Timer = 0f;
        IsActive = false;
    }
}