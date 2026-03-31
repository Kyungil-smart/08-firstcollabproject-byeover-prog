using System.Collections.Generic;

namespace MyGame2.Stage
{
    // 추격자 타겟 전달용 정적 레지스트리
    // B감시자가 SpawnChaser에서 등록, 추격자가 OnUpdate에서 읽음
    // 컴포넌트 조회/EnemyAlertData 의존 없이 확실하게 전달

    public static class ChaserTargetRegistry
    {
        private static readonly Dictionary<int, int> _targets = new Dictionary<int, int>();

        // B감시자가 호출: 추격자 entityId -> 타겟 playerId
        public static void Register(int chaserEntityId, int targetPlayerId)
        {
            _targets[chaserEntityId] = targetPlayerId;
        }

        // 추격자가 호출: 자기 entityId로 타겟 읽기
        public static int GetTarget(int chaserEntityId)
        {
            if (_targets.TryGetValue(chaserEntityId, out int targetId))
            {
                _targets.Remove(chaserEntityId); // 1회 읽고 제거
                return targetId;
            }
            return StageState.InvalidEntityId;
        }

        // 스테이지 리셋 시 호출
        public static void Clear()
        {
            _targets.Clear();
        }
    }
}