namespace MyGame2.Stage
{
    // TurnSystem을 스테이지별 규칙 조합으로 생성하는 빌더.
    
    public sealed class TurnSystemBuilder
    {
        private PushRule _pushRule;
        private CameraEnemy _cameraEnemy;
        private DetectionRule _detectionRule;
        private DeathRule _deathRule;
        private ClearRule _clearRule;

        private TurnSystemBuilder() { }

        // 기본 규칙이 전부 포함된 빌더를 반환한다.
        public static TurnSystemBuilder Default()
        {
            CameraEnemy cam = new CameraEnemy();
            return new TurnSystemBuilder
            {
                _pushRule = new PushRule(),
                _cameraEnemy = cam,
                _detectionRule = new DetectionRule(cam),
                _deathRule = new DeathRule(),
                _clearRule = new ClearRule()
            };
        }

        public TurnSystemBuilder WithPushRule(PushRule rule)
        {
            _pushRule = rule;
            return this;
        }

        public TurnSystemBuilder WithCameraEnemy(CameraEnemy cam)
        {
            _cameraEnemy = cam;
            if (cam != null)
                _detectionRule = new DetectionRule(cam);
            return this;
        }

        public TurnSystemBuilder WithDeathRule(DeathRule rule)
        {
            _deathRule = rule;
            return this;
        }

        public TurnSystemBuilder WithClearRule(ClearRule rule)
        {
            _clearRule = rule;
            return this;
        }

        // 설정된 규칙으로 TurnSystem을 생성한다.
        public TurnSystem Build()
        {
            // null인 규칙은 빈 구현으로 대체
            PushRule push = _pushRule ?? new PushRule();
            CameraEnemy cam = _cameraEnemy ?? new CameraEnemy();
            DetectionRule det = _detectionRule ?? new DetectionRule(cam);
            DeathRule death = _deathRule ?? new DeathRule();
            ClearRule clear = _clearRule ?? new ClearRule();
            MovementRule move = new MovementRule(push);

            return new TurnSystem(move, push, cam, det, death, clear);
        }
    }
}
