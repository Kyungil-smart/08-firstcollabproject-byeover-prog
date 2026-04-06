using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyGame2.Stage
{
    /// <summary>
    /// CCTV 감지 시 0.5초 딜레이 후 플레이어를 죽이는 핸들러.
    /// TurnExecuted 이벤트에서 CameraDetectedPlayerIds를 확인하여 동작.
    /// 감지 즉시: 입력 차단(SetGameOverSilent) + 발각 사운드 재생
    /// 0.5초 후: KillEntity + MarkGameOver → 게임오버 팝업
    /// </summary>
    public sealed class CameraDetectionDelayHandler : MonoBehaviour
    {
        [SerializeField] private StageManager stageManager;

        [Tooltip("감지 → 사망까지 딜레이 (초)")]
        [SerializeField] private float delay = 0.5f;

        private void OnEnable()
        {
            if (stageManager != null)
                stageManager.Events.TurnExecuted += OnTurnExecuted;
        }

        private void OnDisable()
        {
            if (stageManager != null)
                stageManager.Events.TurnExecuted -= OnTurnExecuted;
        }

        private void OnTurnExecuted(TurnOutcome outcome)
        {
            if (outcome.CameraDetectedPlayerIds == null || outcome.CameraDetectedPlayerIds.Count == 0)
                return;

            StageState state = stageManager != null ? stageManager.CurrentState : null;
            if (state == null || state.IsGameOver) return;

            // 즉시 입력 차단
            state.SetGameOverSilent();

            StartCoroutine(DelayedKillCoroutine(new List<int>(outcome.CameraDetectedPlayerIds)));
        }

        private IEnumerator DelayedKillCoroutine(List<int> playerIds)
        {
            // 발각 사운드 재생
            if (InGameSoundManager.Instance != null)
                InGameSoundManager.Instance.PlaySFX(InGameSoundManager.Instance.sfxDetect);

            yield return new WaitForSecondsRealtime(delay);

            StageState state = stageManager != null ? stageManager.CurrentState : null;
            if (state == null) yield break;

            for (int i = 0; i < playerIds.Count; i++)
                state.KillEntity(playerIds[i]);

            state.MarkGameOver();
        }
    }
}