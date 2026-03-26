using UnityEngine;
using UnityEngine.SceneManagement;

public class PausePopupController : MonoBehaviour
{
    [Header("이동할 타이틀 씬 이름")]
    public string titleSceneName = "Title_Scene";
    
    public void OnClickContinueButton()
    {
        InGameUIManager.Instance.ClosePausePopup();
    }

    public void OnClickSettingButton()
    {
        InGameUIManager.Instance.ShowSettingPopup();
    }
    
    public void OnClickMainButton()
    {
        // 시간 원래대로 되돌리기
        Time.timeScale = 1f;
        
        // 타이틀 씬으로 이동.
        SceneManager.LoadScene(titleSceneName);
    }
}
