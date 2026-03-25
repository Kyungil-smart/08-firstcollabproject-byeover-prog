using UnityEngine;

public class PausePopupController : MonoBehaviour
{
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
        
    }
}
