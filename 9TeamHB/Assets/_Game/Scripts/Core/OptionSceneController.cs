using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class OptionSceneController : MonoBehaviour
{
    [Header("UI 연결")]
    public Button englishButton;
    public Button koreanButton;

    private void Start()
    {
        // 이미 첫 실행 완료 -> 타이틀로 바로 이동
        if (PlayerPrefs.GetInt("FirstRunDone", 0) == 1)
        {
            SceneManager.LoadScene("Title_Scene");
            return;
        }

        englishButton.onClick.AddListener(() => SelectLanguage(0));
        koreanButton.onClick.AddListener(() => SelectLanguage(1));
    }

    private void SelectLanguage(int index)
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.ChangeLanguage(index);

        SceneManager.LoadScene("Story_Scene");
    }
}