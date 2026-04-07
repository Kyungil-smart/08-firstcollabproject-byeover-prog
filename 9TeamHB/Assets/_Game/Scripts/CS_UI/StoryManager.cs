using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class StoryManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image cutsceneImage;
    [SerializeField] private TextMeshProUGUI storyText;
    [SerializeField] private Button skipButton; // 추가: 스킵 버튼

    [Header("Story Data")]
    [SerializeField] private StoryData currentStoryData;

    [Header("Input Settings")]
    [SerializeField] private InputActionReference nextAction;

    [Header("Debug")]
    [SerializeField] private bool useDebugLog = false;

    [Header("Scene Transition")]
    [SerializeField] private string nextSceneName = "Stage_Scene";

    private int currentPage = 0;

    private void OnEnable()
    {
        if (nextAction != null)
        {
            nextAction.action.Enable();
            nextAction.action.performed += OnNextPageInput;
        }
        LocalizationManager.LanguageChangedEvent += OnLanguageChanged;
    }

    private void OnDisable()
    {
        if (nextAction != null)
        {
            nextAction.action.performed -= OnNextPageInput;
            nextAction.action.Disable();
        }
        LocalizationManager.LanguageChangedEvent -= OnLanguageChanged;
    }

    private void Start()
    {
        if (storyText != null && LocalizationManager.Instance != null && LocalizationManager.Instance.storyFont != null)
            storyText.font = LocalizationManager.Instance.storyFont;

        // 스킵 버튼 연결
        if (skipButton != null)
            skipButton.onClick.AddListener(EndStory);

        UpdateUI();
    }

    private void OnNextPageInput(InputAction.CallbackContext context)
    {
        NextPage();
    }

    private void NextPage()
    {
        if (currentStoryData == null || currentStoryData.pages.Length == 0) return;

        currentPage++;

        if (currentPage < currentStoryData.pages.Length)
        {
            UpdateUI();
            return;
        }

        EndStory();
    }

    private void UpdateUI()
    {
        if (currentStoryData == null) return;

        StoryPage page = currentStoryData.pages[currentPage];

        if (cutsceneImage != null) cutsceneImage.sprite = page.cutsceneImage;

        if (storyText != null)
        {
            if (LocalizationManager.Instance != null)
                storyText.text = LocalizationManager.Instance.GetText(page.storyKey);
            else
                storyText.text = page.storyKey;
        }

        if (useDebugLog) Debug.Log($"현재 스토리: {currentPage + 1} / {currentStoryData.pages.Length}");
    }

    private void OnLanguageChanged()
    {
        if (LocalizationManager.Instance != null && LocalizationManager.Instance.storyFont != null)
            storyText.font = LocalizationManager.Instance.storyFont;
        UpdateUI();
    }

    private void EndStory()
    {
        PlayerPrefs.SetInt("HasSeenStory", 1);
        PlayerPrefs.SetInt("FirstRunDone", 1);
        PlayerPrefs.Save();

        // 로딩 씬 없이 바로 타이틀로
        UnityEngine.SceneManagement.SceneManager.LoadScene("Title_Scene");
    }
}