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
        Bush = 10,              // 부쉬 엔티티 오브젝트
        ProjectileLauncher = 11,  // 투사체 발사기 (벽 타일, 4방향)
        Projectile = 12,          // 투사체 (실시간 이동, 접촉 즉사/파괴)
        SawTrapEnemy = 13         // 바닥형 함정 02 (톱날, 1×2 / 1×5)
    }

    public enum BoxType
    {
        Shared = 0,
        Player1Only = 1,
        Player2Only = 2,
        Iron = 3,
        Ice = 4,
        Breakable = 5
    }

    public enum CameraType
    {
        LineShort = 0,
        LineLong = 1,
        PyramidSmall = 2,
        PyramidLarge = 3,
        Fixed3x3 = 4
    }

    public enum PlayerType
    {
        Robot = 0,
        Animal = 1
    }
}