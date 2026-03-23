namespace MyGame2.Stage
{
    // CameraEnemy 전용 데이터.
    public struct CameraData
    {
        // 감지 패턴 (직선3/직선5/피라미드3줄/피라미드5줄)
        public CameraType Pattern;

        // true면 반시계방향 회전, false면 시계방향 회전 (기본)
        public bool ReverseRotation;

        public CameraData(CameraType pattern, bool reverseRotation = false)
        {
            Pattern = pattern;
            ReverseRotation = reverseRotation;
        }
    }
}