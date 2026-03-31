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
        PatrolCameraEnemy = 6,
        SummonerEnemy = 7,
        ChaserEnemy = 8,
        Gap = 9,
        Bush = 10,
        ProjectileLauncher = 11,  // 투사체 발사기 (벽 타일, 4방향)
        Projectile = 12           // 투사체 (실시간 이동, 접촉 즉사/파괴)
    }

    public enum BoxType
    {
        Shared = 0,
        Player1Only = 1,
        Player2Only = 2,
        Iron = 3,
        Ice = 4
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