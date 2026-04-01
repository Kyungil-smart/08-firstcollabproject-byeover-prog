namespace MyGame2.Stage
{
    // 바닥형 함정 02 (톱날) 전용 데이터.
    // 앵커 위치에서 Facing 방향으로 Size칸만큼 위험 범위를 가진다.
    
    public struct SawTrapData : IComponentData
    {
        // 톱날이 커버하는 셀 수 (2 또는 5)
        public int Size;

        public SawTrapData(int size)
        {
            Size = size;
        }
    }
}