using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections; 

public class StoryManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image cutsceneImage;
    [SerializeField] private TextMeshProUGUI storyText;
    [SerializeField] private Button skipButton; 

    [Header("Story Data")]
    [SerializeField] private StoryData currentStoryData;

    [Header("Input Settings")]
    [SerializeField] private InputActionReference nextAction; 
    
    [Header("Typing Effect Settings")]
    [SerializeField] private float typeSpeed = 0.05f; 

    [Header("Debug")]
    [SerializeField] private bool useDebugLog = false;

    [Header("Scene Transition")]
    [SerializeField] private string nextSceneName = "Stage_Scene";

    private int currentPage = 0;
    
    private Coroutine typingCoroutine;
    private bool isTyping = false;

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

        
        if (skipButton != null)
            skipButton.onClick.AddListener(EndStory);

        UpdateUI();
    }

    
    private void OnNextPageInput(InputAction.CallbackContext context)
    {
        if (isTyping)
        {
            // 타이핑 중이라면 전체 글자를 한 번에 팍 띄움 (스킵)
            SkipTyping();
        }
        else
        {
            // 타이핑이 다 끝난 상태라면 다음 페이지로 넘어감
            NextPage();
        }
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
            string localizedText = "";
            if (LocalizationManager.Instance != null)
                localizedText = LocalizationManager.Instance.GetText(page.storyKey);
            else
                localizedText = page.storyKey;

            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypeTextRoutine(localizedText));
        }

        if (useDebugLog) Debug.Log($"현재 스토리: {currentPage + 1} / {currentStoryData.pages.Length}");
    }

    private IEnumerator TypeTextRoutine(string fullText)
    {
        isTyping = true; 

        storyText.text = fullText;
        storyText.ForceMeshUpdate(); 
        int totalVisibleCharacters = storyText.textInfo.characterCount;
        storyText.maxVisibleCharacters = 0;

        for (int i = 0; i <= totalVisibleCharacters; i++)
        {
            storyText.maxVisibleCharacters = i;
            
            
            // InGameSoundManager.Instance?.PlayBasicButtonClickSound();

            yield return new WaitForSeconds(typeSpeed);
        }

        isTyping = false; 
    }
    
    private void SkipTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        
        storyText.maxVisibleCharacters = 99999; 
        isTyping = false; 
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