namespace MyGame2.Stage
{
    public enum EnemyAIState
    {
        Patrol = 0,       // 시계방향 순찰 (공통)
        Alert = 1,        // 0.5초 정지 + 느낌표 팝업 (공통)
        Chase = 2,        // A* 경로로 추격 (동물, 추격자)
        Lost = 3,         // 부쉬 진입으로 대상 놓침, 0.5초 탐색 (동물, 추격자)
        ReturnToZone = 4, // 기존 감시구역으로 복귀 (동물)
        Explode = 5,      // 감시영역 함정화 (CCTV, 새감시자A)
        Summon = 6,       // 추격 감시자 소환 중 (새감시자B)
        Frozen = 7,       // 소환 후 이동 정지, 추격자 소멸 대기 (새감시자B)
        Spawn = 8,        // 소환되어 등장 중 (추격자)
        Despawn = 9       // 소멸 처리 (추격자)
    }

    // 이동형 감시자 공용 상태 데이터.

    public class EnemyAlertData : IComponentData
    {
        public EnemyAIState State;

        // 상태 진입 후 경과 시간
        public float Timer;

        // Chase/Lost 상태에서 플레이어를 마지막으로 본 위치
        public GridPos LastKnownPlayerPos;

        // SummonerEnemy 전용: 소환한 추격자(ChaserEnemy)의 엔티티 ID
        // 추격자가 소멸하면 Summoner는 Frozen → Patrol로 복귀한다.
        public int SpawnedChaserId;

        // ChaserEnemy 전용: 자신을 소환한 SummonerEnemy의 엔티티 ID
        // 소멸 시 Summoner에게 통보하기 위해 필요하다.
        public int OwnerSummonerId;

        public EnemyAlertData()
        {
            State = EnemyAIState.Patrol;
            Timer = 0f;
            SpawnedChaserId = StageState.InvalidEntityId;
            OwnerSummonerId = StageState.InvalidEntityId;
        }

        // 순찰 상태로 초기화 (추격 종료 후 복귀 시 사용)
        public void Reset()
        {
            State = EnemyAIState.Patrol;
            Timer = 0f;
            SpawnedChaserId = StageState.InvalidEntityId;
        }
    }
}