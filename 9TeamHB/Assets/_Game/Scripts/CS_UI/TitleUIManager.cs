using UnityEngine;
using UnityEngine.UI;

public class TitleUIManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject keyboardPanel;

    [SerializeField] private GameObject settingPrefab;
    private GameObject activeSetting;

    [Header("버튼")]
    [SerializeField] private Button replayStoryButton; // 추가: 스토리 다시보기 버튼

    [Header("Scene Settings")]
    [SerializeField] private string startSceneName = "Stage_Scene";

    [Header("Debug")]
    [SerializeField] private bool useDebugLog = false;

    private void Start()
    {
        InGameSoundManager.Instance?.PlayTitleBGM();

        // 첫 실행 전이면 다시보기 버튼 숨김
        if (replayStoryButton != null)
            replayStoryButton.gameObject.SetActive(PlayerPrefs.GetInt("FirstRunDone", 0) == 1);
    }

    public void OnClickGameStart()
    {
        // 첫 실행이면 Option_Scene(언어선택) -> Story -> Stage
        if (PlayerPrefs.GetInt("FirstRunDone", 0) == 0)
        {
            LoadingManager.LoadScene("Option_Scene");
            return;
        }

        // 이미 본 적 있으면 바로 스테이지 선택
        LoadingManager.LoadScene(startSceneName);
    }

    public void OnClickReplayStory()
    {
        LoadingManager.LoadScene("Story_Scene");
    }

    public void OpenKeyboardPanel()
    {
        if (keyboardPanel == null) return;
        keyboardPanel.SetActive(true);
    }

    public void CloseKeyboardPanel()
    {
        if (keyboardPanel == null) return;
        keyboardPanel.SetActive(false);
    }

    public void OnClickQuit()
    {
        Application.Quit();
    }

    public void OnClickOption()
    {
        if (activeSetting == null)
            activeSetting = Instantiate(settingPrefab);
        else
            activeSetting.SetActive(true);
    }
}