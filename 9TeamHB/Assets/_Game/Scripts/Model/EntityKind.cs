namespace MyGame2.Stage
{
    public enum EntityKind
    {
        None = 0,
        Player = 1,
        Box = 2,
        CameraEnemy = 3,       // 고정형 CCTV 감시자
        RobotEnemy = 4,         // 로봇 감시자 (웨이포인트 순찰, 2배속 가속)
        AnimalEnemy = 5,        // 동물 감시자 (순찰 + A* 추격)
        PatrolCameraEnemy = 6,  // 새 감시자A (이동형 CCTV)
        SummonerEnemy = 7,      // 새 감시자B (적발 시 추격자 소환, 본체 피격 판정 없음)
        ChaserEnemy = 8,        // 추격 감시자 (SummonerEnemy가 소환, A* 추격 후 소멸)
        Gap = 9,                // 틈새 타일 오브젝트
        Bush = 10               // 부쉬 엔티티 오브젝트
    }

    public enum BoxType
    {
        // 녹색 — 양쪽 다 밀기 가능
        Shared = 0,
        // 노란색 — Player1만 밀기 가능
        Player1Only = 1,
        // 주황색 — Player2만 밀기 가능
        Player2Only = 2,
        // 빨간색 — 아무도 못 밂 (철 상자)
        Iron = 3,
        // 파란색 — 밀면 벽이나 오브젝트에 닿을 때까지 미끄러짐
        Ice = 4,
        // 갈색(나무) - 양쪽 다 밀기 가능, 막힌 상태에서 밀면 파괴됨
        Breakable = 5   // 부숴지는 상자 추가부분
    }

    public enum CameraType
    {
        LineShort = 0,
        LineLong = 1,
        PyramidSmall = 2,
        PyramidLarge = 3,
        // 본인 위치 포함 3×3 고정형
        Fixed3x3 = 4
    }

    public enum PlayerType
    {
        Robot = 0,
        Animal = 1
    }
}