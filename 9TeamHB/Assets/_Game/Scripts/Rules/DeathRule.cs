using System.Collections.Generic;

namespace MyGame2.Stage
{
    // 사망 판정 규칙.
    public sealed class DeathRule
    {
        public bool ApplyContactKill(StageState state, MoveResult result)
        {
            if (!result.IsContactKill) return false;
            bool killed = state.KillEntity(result.TargetEntityId);
            if (state.IsAnyPlayerDead()) state.MarkGameOver();
            return killed;
        }

        public bool ApplyCameraDetections(StageState state, IReadOnlyList<int> detectedPlayerIds)
        {
            bool changed = false;
            for (int i = 0; i < detectedPlayerIds.Count; i++)
                changed |= state.KillEntity(detectedPlayerIds[i]);
            if (state.IsAnyPlayerDead()) state.MarkGameOver();
            return changed;
        }

        // 카메라 감지 — 입력만 차단, KillEntity는 딜레이 후 처리
        public void ApplyCameraDetectionsSilent(StageState state)
        {
            state.SetGameOverSilent();
        }
    }
}