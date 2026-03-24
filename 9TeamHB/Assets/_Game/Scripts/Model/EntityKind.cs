namespace MyGame2.Stage
{
    public enum EntityKind
    {
        None = 0,
        Player = 1,
        Box = 2,
        CameraEnemy = 3,
        RobotEnemy = 4,
        AnimalEnemy = 5
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
        Iron = 3
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
}