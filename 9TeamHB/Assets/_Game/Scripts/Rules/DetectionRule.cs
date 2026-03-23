using System.Collections.Generic;

namespace MyGame2.Stage
{
    // 모든 카메라를 순회하여 감지된 플레이어 ID를 수집한다.
    public sealed class DetectionRule
    {
        private readonly CameraEnemy _cameraEnemy;
        private readonly HashSet<int> _uniqueFilter = new HashSet<int>();

        public DetectionRule(CameraEnemy cameraEnemy)
        {
            _cameraEnemy = cameraEnemy;
        }

        public void DetectPlayers(StageState state, List<int> outBuffer)
        {
            _uniqueFilter.Clear();
            for (int i = 0; i < state.CameraIds.Count; i++)
            {
                if (_cameraEnemy.TryDetect(state, state.CameraIds[i], out int pid))
                {
                    if (_uniqueFilter.Add(pid))
                        outBuffer.Add(pid);
                }
            }
        }
    }
}