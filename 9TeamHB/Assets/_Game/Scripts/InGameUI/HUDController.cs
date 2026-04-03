using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class HUDController : MonoBehaviour
{
    [Header("경과 시간 텍스트")]
    public TextMeshProUGUI playTimeText;

    [Header("텍스트")] 
    public TextMeshProUGUI stageText;
    [Header("이동 횟수 LocalizedText(텍스트 등록)")]
    public LocalizedText moveCountLocText;
    
    [Header("UI 연결")]
    public HUDRestartUI hudRestartUI;

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
        
        if (Keyboard.current != null)
        {
            // ESC: 일시정지 토글 
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                HandleEscapeKey();
            }

            // Tab: 태그 버튼 
            if (Keyboard.current.tabKey.wasPressedThisFrame)
            {
                InGameUIManager.Instance.OnClickTagButton();
            }
            
            // R: 재시작 꾹 누르기 
            if (hudRestartUI != null)
            {
                if (Keyboard.current.rKey.wasPressedThisFrame)
                {
                    hudRestartUI.SetHoldingByKey(true);
                }
                else if (Keyboard.current.rKey.wasReleasedThisFrame)
                {
                    hudRestartUI.SetHoldingByKey(false);
                }
            }

        
            if (Keyboard.current.homeKey.wasPressedThisFrame)
            {
                StageProgressManager.ResetAll(15); // 튜토리얼 7 + 메인 8 = 15
                Debug.Log("[HUD] Home키 → 전체 클리어 기록 초기화 완료");
            }
        }
    }
    
    
    public void UpdateStageText(int stageNum)
    {
        if (stageText != null)
        {
            stageText.text = $"Stage {stageNum}";
        }
    }

    public void UpdateMoveCountText(int currentCount, int maxCount)
    {
        if (moveCountLocText != null)
        {
            moveCountLocText.SetVariables(currentCount, maxCount);
        }
    }


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