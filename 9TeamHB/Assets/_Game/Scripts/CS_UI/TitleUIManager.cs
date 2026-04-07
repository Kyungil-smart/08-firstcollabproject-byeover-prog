using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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
        if (PlayerPrefs.GetInt("FirstRunDone", 0) == 0)
        {
            SceneManager.LoadScene("Option_Scene");
            return;
        }
        SceneManager.LoadScene("Stage_Scene");
    }

    public void OnClickReplayStory()
    {
        SceneManager.LoadScene("Story_Scene");
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