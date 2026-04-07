using UnityEngine;
using UnityEngine.UI;

public class OptionSceneController : MonoBehaviour
{
    [Header("UI 연결")]
    public Button englishButton;
    public Button koreanButton;

    private void Start()
    {
        // 이미 한 번 봤으면 바로 스테이지 선택으로
        if (PlayerPrefs.GetInt("FirstRunDone", 0) == 1)
        {
            LoadingManager.LoadScene("Stage_Scene");
            return;
        }

        englishButton.onClick.AddListener(() => SelectLanguage(0));
        koreanButton.onClick.AddListener(() => SelectLanguage(1));
    }

    private void SelectLanguage(int index)
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.ChangeLanguage(index);

        LoadingManager.LoadScene("Story_Scene");
    }
}