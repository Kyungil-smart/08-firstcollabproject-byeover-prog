using UnityEngine;

public class SettingUIController : MonoBehaviour
{
    public void OnClickReturnButton()
    {
        InGameUIManager.Instance.CloseSettingPopup();
    }
}
