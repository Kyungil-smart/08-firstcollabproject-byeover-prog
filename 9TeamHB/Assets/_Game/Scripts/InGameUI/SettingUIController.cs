using UnityEngine;

public class SettingUIController : MonoBehaviour
{
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
