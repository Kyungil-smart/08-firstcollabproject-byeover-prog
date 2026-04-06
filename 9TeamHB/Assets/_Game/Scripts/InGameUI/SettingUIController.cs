using UnityEngine;
using UnityEngine.UI;

public class SettingUIController : MonoBehaviour
{
    [Header("UI 연결")]
    public Toggle fullScreenToggle;
    
    private void OnEnable()
    {
        // 현재 화면 상태로 Toggle의 체크 여부를 변경.
        if (fullScreenToggle != null)
        {
            fullScreenToggle.isOn = Screen.fullScreen;
        }
    }
    
    // 토글이의 OnValueChanged 연결 함수.
    public void SetFullScreen(bool isFullScreen)
    {
        Screen.fullScreen = isFullScreen;
        Debug.Log("전체화면 모드 변경: " + isFullScreen);
    }
    
    public void CloseSetting()
    {
        // 인게임인 경우 (InGameUiManager 존재) 
        if (InGameUIManager.Instance != null)
        {
            InGameUIManager.Instance.CloseSettingPopup();
        }
        // 2. 타이틀 씬인 경우 (InGameUIManager가 없음)
        else
        {
            gameObject.SetActive(false); 
        }
    }
}
