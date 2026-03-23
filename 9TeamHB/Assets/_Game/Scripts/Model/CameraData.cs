namespace MyGame2.Stage
{
    // CameraEnemy 전용 데이터.
    public struct CameraData
    {
        // 감지 패턴 (직선3/직선5/피라미드3줄/피라미드5줄)
        public CameraType Pattern;

        public CameraData(CameraType pattern) { Pattern = pattern; }
    }
}