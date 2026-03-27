namespace MyGame2.Stage
{
    // CameraEnemy 전용 데이터.
    public struct CameraData : IComponentData
    {
        // 감지 패턴 (직선3/직선5/피라미드3줄/피라미드5줄/고정3x3)
        public CameraType Pattern;

        // true면 반시계방향 회전
        public bool ReverseRotation;

        public CameraData(CameraType pattern, bool reverseRotation = false)
        {
            Pattern = pattern;
            ReverseRotation = reverseRotation;
        }
    }
}