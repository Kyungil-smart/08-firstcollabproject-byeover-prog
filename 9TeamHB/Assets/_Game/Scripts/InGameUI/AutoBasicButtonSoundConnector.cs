using UnityEngine;
using UnityEngine.UI;

public class AutoBasicButtonSoundConnector : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Button[] allButtons = GetComponentsInChildren<Button>(true);
        
        //모든 자식 버튼에 기본 버튼 클릭 사운드 추가.
        foreach (Button btn in allButtons)
        {
            btn.onClick.AddListener(InGameSoundManager.Instance.PlayBasicButtonClickSound); 
        }
    }
}
