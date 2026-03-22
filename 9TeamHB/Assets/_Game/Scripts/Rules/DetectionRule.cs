using System.Collections.Generic;

namespace MyGame2.Stage
{
    /// <summary>
    /// 카메라 감지 판정 규칙.
    /// 외부 버퍼를 받아 GC 할당을 제거한다.
    /// </summary>
    public sealed class DetectionRule
    {
        private readonly CameraEnemy _cameraEnemy;

        /// <summary>중복 방지용 내부 HashSet (재사용)</summary>
        private readonly HashSet<int> _uniqueFilter = new HashSet<int>();

        public DetectionRule(CameraEnemy cameraEnemy)
        {
            _cameraEnemy = cameraEnemy;
        }

        /// <summary>
        /// 모든 카메라를 순회하며 감지된 플레이어 ID를 outBuffer에 채운다.
        /// outBuffer는 호출 전에 Clear 되어 있어야 한다.
        /// </summary>
        public void DetectPlayers(StageState state, List<int> outBuffer)
        {
            _uniqueFilter.Clear();

            for (int i = 0; i < state.CameraIds.Count; i++)
            {
                int cameraId = state.CameraIds[i];

                if (_cameraEnemy.TryDetect(state, cameraId, out int detectedPlayerId))
                {
                    if (_uniqueFilter.Add(detectedPlayerId))
                    {
                        outBuffer.Add(detectedPlayerId);
                    }
                }
            }
        }
    }
}