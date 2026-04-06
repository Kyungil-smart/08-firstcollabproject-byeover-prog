using UnityEngine;
using UnityEngine.SceneManagement;

public class PausePopupController : MonoBehaviour
{
    [Header("이동할 스테이지 선택 씬 이름")]
    public string stageSelectionSceneName = "Stage_Scene";
    
    public void OnClickContinueButton()
    {
        InGameUIManager.Instance.ClosePausePopup();
    }

    public void OnClickRetryButton()
    {
        InGameUIManager.Instance.ExecuteGameQuitRetry();
        InGameUIManager.Instance.ClosePausePopup();
    }
    
    public void OnClickStageSelectionButton()
    {
        // 시간 원래대로 되돌리기
        Time.timeScale = 1f;
        
        // 타이틀 씬으로 이동.
        SceneManager.LoadScene(stageSelectionSceneName);
    }
    
    public void OnClickSettingButton()
    {
        InGameUIManager.Instance.ShowSettingPopup();
    }
    
    public void OnClickQuitButton()
    {
        // 실제 빌드된 게임 종료
        Application.Quit();

        // 유니티 에디터 상에서 테스트할 때 (에디터 재생 모드 종료)
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif

        Debug.Log("게임이 종료되었습니다.");
    }
    
    
}
