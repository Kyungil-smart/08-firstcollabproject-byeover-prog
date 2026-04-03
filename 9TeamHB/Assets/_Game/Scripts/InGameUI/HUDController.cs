using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class HUDController : MonoBehaviour
{
    [Header("경과 시간 텍스트")]
    public TextMeshProUGUI playTimeText;
    
    public void Start()
    {
        // 인게임 시작할 때 브금 재생 시작. 
        // if (InGameSoundManager.Instance.mainBGM != null)
        // {
        //     InGameSoundManager.Instance.PlayBGM(InGameSoundManager.Instance.mainBGM);
        // }
    }

    public void Update()
    {
        GetTimeElapsed();
        
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            HandleEscapeKey();
        }

        // Home키: 클리어 진행 초기화 (디버그/테스트용)
        if (Keyboard.current != null && Keyboard.current.homeKey.wasPressedThisFrame)
        {
            StageProgressManager.ResetAll(15); // 튜토리얼 7 + 메인 8 = 15
            Debug.Log("[HUD] Home키 → 전체 클리어 기록 초기화 완료");
        }
    }

    // ESC 토글: 일시정지 열기/닫기
    private void HandleEscapeKey()
    {
        if (InGameUIManager.Instance == null) return;

        // 이미 일시정지 팝업이 열려있으면 -> 닫기
        if (InGameUIManager.Instance.IsPausePopupActive)
        {
            InGameUIManager.Instance.ClosePausePopup();
            return;
        }

        // StageClear/GameOver 상태에서는 ESC 차단
        var gameManager = FindAnyObjectByType<MyGame2.Stage.GameManager>();
        if (gameManager != null)
        {
            if (gameManager.CurrentState != MyGame2.Stage.GameFlowState.Playing)
            {
                return;
            }
        }

        // Playing 상태 -> 일시정지 열기
        OnClickPauseButton();
    }

    public void OnClickPauseButton()
    {
        InGameUIManager.Instance.ShowPausePopup();   
    }

    public void OnClickSettingButton()
    {
        InGameUIManager.Instance.ShowSettingPopup();
    }

    public void GetTimeElapsed()
    {
        if (InGameUIManager.Instance != null)
        {
            float time = InGameUIManager.Instance.timeElapsed;

            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);

            playTimeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        } 
    }
}