namespace MyGame2.Stage
{
    // 엔티티 종류.
    public enum EntityKind
    {
        None = 0,
        Player = 1,
        Box = 2,
        CameraEnemy = 3,
        RobotEnemy = 4,
        AnimalEnemy = 5
    }

    // 상자 소유권 타입.
    public enum BoxType
    {
        Shared = 0,
        Player1Only = 1,
        Player2Only = 2
    }

    // 카메라 감지 패턴 타입.
    public enum CameraType
    {
        LineShort = 0,
        LineLong = 1,
        PyramidSmall = 2,
        PyramidLarge = 3
    }
}