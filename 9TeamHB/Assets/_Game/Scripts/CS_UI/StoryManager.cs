using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class StoryManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image cutsceneImage;
    [SerializeField] private TextMeshProUGUI storyText;

    [Header("Story Data")]
    [SerializeField] private StoryData currentStoryData; // 스토리 데이터에서 정보 받아옴

    [Header("Input Settings")]
    [SerializeField] private InputActionReference nextAction; // 클릭+스페이스바로 넘어가기

    [Header("Debug")]
    [SerializeField] private bool useDebugLog = false;

    [Header("Scene Transition")]
    [SerializeField] private string nextSceneName = "Stage_Scene"; // 다음에 이동할 씬 이름

    private int currentPage = 0;

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
        if (storyText != null && LocalizationManager.Instance != null && LocalizationManager.Instance.mainFont != null)
        {
            storyText.font = LocalizationManager.Instance.mainFont;
        }
        
        UpdateUI();
    }

    // New Input System 부르기
    private void OnNextPageInput(InputAction.CallbackContext context)
    {
        NextPage();
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
            storyText.text = LocalizationManager.Instance.GetText(page.storyKey);
        }


        if (storyText != null) 
        {
            // 번역 검사
            if (LocalizationManager.Instance != null)
            {
                // 잘되면 정상적으로 번역본을 가져오기
                // TODO: 병합 에러로 임시 주석 처리, storyText.text = LocalizationManager.Instance.GetText(page.storyKey);
            }
            else
            {
                // 바로 씬을 키면 Key값만 띄움
                // TODO: 병합 에러로 임시 주석 처리, storyText.text = page.storyKey; 
                Debug.LogWarning("타이틀 씬부터 실행하는걸 추천함.");
            }
        }

        if (useDebugLog) Debug.Log($"현재 스토리: {currentPage + 1} / {currentStoryData.pages.Length}");
    }

    private void OnLanguageChanged()
    {
        storyText.font = LocalizationManager.Instance.mainFont;
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
