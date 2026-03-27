namespace MyGame2.Stage
{
    // 얼음 상자 전용 데이터.
    // 이 컴포넌트가 부착된 상자는 밀었을 때
    // 벽이나 다른 오브젝트에 닿을 때까지 미끄러진다.
    //
    // [사용 방법]
    // EntityConfigSO에서 useIceSlide 체크 → EntityState 생성 시 자동 부착
    //
    // [미끄러짐 규칙] (기획서 참고)
    // - 밀면 해당 방향으로 계속 이동
    // - 벽에 닿으면 정지
    // - 다른 상자/감시자/캐릭터에 닿으면 정지
    // - 함정 위를 지나가면 함정 비활성화 + 상자도 정지

    public struct IceSlideData : IComponentData
    {
        // 현재 미끄러지고 있는 중인가?
        public bool IsSliding;

        // 미끄러지는 방향 (밀린 방향)
        public Direction SlideDirection;

        public IceSlideData(bool isSliding, Direction slideDirection)
        {
            IsSliding = isSliding;
            SlideDirection = slideDirection;
        }
    }
}