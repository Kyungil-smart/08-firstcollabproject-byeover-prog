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

    [Header("Story Data")]
    [SerializeField] private StoryData currentStoryData; // 스토리 데이터에서 정보 받아옴

    [Header("Input Settings")]
    [SerializeField] private InputActionReference nextAction; // 클릭+스페이스바로 넘어가기
    
    [Header("Typing Effect Settings")]
    [SerializeField] private float typeSpeed = 0.05f; // 한 글자가 나오는 데 걸리는 시간

    [Header("Debug")]
    [SerializeField] private bool useDebugLog = false;

    [Header("Scene Transition")]
    [SerializeField] private string nextSceneName = "Stage_Scene"; // 다음에 이동할 씬 이름

    private int currentPage = 0;
    
    private Coroutine typingCoroutine;
    private bool isTyping = false;

    private void OnEnable()
    {
        // 입력 액션 활성화 및 이벤트 구독
        if (nextAction != null)
        {
            nextAction.action.Enable();
            nextAction.action.performed += OnNextPageInput;
        }
        LocalizationManager.LanguageChangedEvent += OnLanguageChanged; 
    }

    private void OnDisable()
    {
        // 입력 액션 해제
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
        {
            storyText.font = LocalizationManager.Instance.storyFont;
        }
        
        UpdateUI();
    }

    // New Input System 부르기 (마우스 클릭 또는 스페이스바 감지)
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

        // 다음 페이지가 남아있을 경우
        if (currentPage < currentStoryData.pages.Length)
        {
            UpdateUI();
            return; // 업데이트 후엔 종료
        }

        // 남은 페이지가 없을 경우 끝
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
        isTyping = true; // 타이핑 시작!

        storyText.text = fullText;
        storyText.ForceMeshUpdate(); 
        int totalVisibleCharacters = storyText.textInfo.characterCount;
        storyText.maxVisibleCharacters = 0;

        for (int i = 0; i <= totalVisibleCharacters; i++)
        {
            storyText.maxVisibleCharacters = i;
            
            // 필요하다면 타닥타닥 효과음을 넣는 곳 (주석 해제해서 사용 가능)
            // InGameSoundManager.Instance?.PlayBasicButtonClickSound();

            yield return new WaitForSeconds(typeSpeed);
        }

        isTyping = false; // 글자가 다 나오면 타이핑 종료!
    }
    
    private void SkipTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        
        storyText.maxVisibleCharacters = 99999; // 모든 글자를 즉시 화면에 표시
        isTyping = false; // 타이핑 상태 끄기
    }

    private void OnLanguageChanged()
    {
        if (LocalizationManager.Instance != null && LocalizationManager.Instance.storyFont != null)
            storyText.font = LocalizationManager.Instance.storyFont;
            
        UpdateUI(); 
    }
    
    private void EndStory()
    {
        if (useDebugLog) Debug.Log($"스토리 종료. {nextSceneName}으로 이동.");

        // 다시 보지 않도록 저장 (오프닝용)
        PlayerPrefs.SetInt("HasSeenStory", 1);
        PlayerPrefs.Save();

        // 로딩 매니저를 통해 지정한 씬으로 이동
        LoadingManager.LoadScene(nextSceneName);
    }
}