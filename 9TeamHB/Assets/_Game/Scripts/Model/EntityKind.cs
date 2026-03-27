namespace MyGame2.Stage
{
    public enum EntityKind
    {
        None = 0,
        Player = 1,
        Box = 2,
        CameraEnemy = 3,
        RobotEnemy = 4,
        AnimalEnemy = 5,
        PatrolCameraEnemy = 6,   // 새 감시자A (이동형 CCTV, 적발 시 폭발)
        SummonerEnemy = 7,       // 새 감시자B (소환형, 적발 시 추격자 소환)
        ChaserEnemy = 8,         // 추격 감시자 (소환되어 캐릭터 추격)

        // --- 타일 오브젝트 ---
        Gap = 9,                 // 틈새
        Bush = 10                // 부쉬 (숨는 곳)
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
        Ice = 4
    }

    public enum CameraType
    {
        LineShort = 0,
        LineLong = 1,
        PyramidSmall = 2,
        PyramidLarge = 3,
        // 본인 위치 포함 3×3 고정형 (비회전)
        Fixed3x3 = 4
    }

    public enum PlayerType
    {
        Robot = 0,
        Animal = 1
    }
}