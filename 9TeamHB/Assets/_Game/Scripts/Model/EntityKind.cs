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
        ProjectileLauncher = 9,
        Projectile = 10,
        Gap = 11,
        Bush = 12,
        ButtonEntity = 13,
        LeverEntity = 14,
        DoorEntity = 15,
        SawTrapEnemy = 16,
        FireTrap = 17,
    }

    public enum BoxType
    {
        Shared = 0,
        Player1Only = 1,
        Player2Only = 2,
        Iron = 3,
        Ice = 4,
        Breakable = 5,
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