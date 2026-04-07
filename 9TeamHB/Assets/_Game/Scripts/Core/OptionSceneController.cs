using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OptionSceneController : MonoBehaviour
{
    [Header("UI 연결")]
    public Button englishButton;
    public Button koreanButton;

    private void Start()
    {
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