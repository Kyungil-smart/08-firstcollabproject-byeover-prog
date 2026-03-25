using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleUIManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject keyboardPanel;
    // optionPanel을 추후에 추가할 곳

    [Header("Scene Settings")]
    [SerializeField] private string startSceneName = "UI_Scene";

    [Header("Debug")]
    [SerializeField] private bool useDebugLog = false;

    // 게임시작 버튼 연결용
    public void OnClickGameStart()
    {
        if (useDebugLog) Debug.Log($"{startSceneName}으로 이동.");
        
        // 스테이지 선택 화면(UI_Scene)으로 전환
        SceneManager.LoadScene(startSceneName);
    }

    // 조작키 버튼 연결용 (패널 열기)
    public void OpenKeyboardPanel()
    {
        if (keyboardPanel == null) return;
        
        keyboardPanel.SetActive(true);
        if (useDebugLog) Debug.Log("조작키 안내.");
    }

    // 조작키 보고 나가기용 버튼 연결용 (패널 닫기)
    public void CloseKeyboardPanel()
    {
        if (keyboardPanel == null) return;
        
        keyboardPanel.SetActive(false);
    }

    // 게임종료 버튼 연결용
    public void OnClickQuit()
    {
        if (useDebugLog) Debug.Log("게임을 종료.");
        
        Application.Quit();
    }
}
