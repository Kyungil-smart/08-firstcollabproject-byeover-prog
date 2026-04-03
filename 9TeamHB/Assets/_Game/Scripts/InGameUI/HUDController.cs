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
        // 브금 재생
        // if (InGameSoundManager.Instance.mainBGM != null)
        // {
        //     InGameSoundManager.Instance.PlayBGM(InGameSoundManager.Instance.mainBGM);
        // }
        // 시작 시 타이머 재생
        
    }

    public void Update()
    {
        GetTimeElapsed();
        
        if(Keyboard.current != null)
        {
            // 키보드 인풋
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                OnClickPauseButton();
            }
            // if (Keyboard.current.spaceKey.wasPressedThisFrame)
            // {
            //     InGameUIManager.Instance.OnClickUndoButton();
            // }

            if (Keyboard.current.tabKey.wasPressedThisFrame)
            {
                InGameUIManager.Instance.OnClickTagButton();
            }
            
            // 재시작 꾹누를때
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
        }
    }
    
    // 텍스트를 업데이트 
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
    
    //프리펩화했을때 InGameUIManger 참조 못하는 문제 해결을 위해 프리펩 안에 스크립트로 참조
    public void OnClickPauseButton()
    {
            // 버튼 누르면 InGameUIManger 싱글톤에 정의된 ShowPausePopup으로 일시정지화면 생성
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
            //Debug.Log(minutes + ":" + seconds);
        } 
    }
}