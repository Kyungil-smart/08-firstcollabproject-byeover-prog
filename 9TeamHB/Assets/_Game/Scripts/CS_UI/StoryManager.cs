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

    private int currentPage = 0;

    private void OnEnable()
    {
        // 입력 액션 활성화 및 이벤트 구독
        if (nextAction != null)
        {
            nextAction.action.Enable();
            nextAction.action.performed += OnNextPageInput;
        }
    }

    private void OnDisable()
    {
        // 입력 액션 해제
        if (nextAction != null)
        {
            nextAction.action.performed -= OnNextPageInput;
            nextAction.action.Disable();
        }
    }

    private void Start()
    {
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

        // 남은 페이지가 없을 경우 -> 끝
        EndStory();
    }

    private void UpdateUI()
    {
        if (currentStoryData == null) return;

        StoryPage page = currentStoryData.pages[currentPage];
        
        if (cutsceneImage != null) cutsceneImage.sprite = page.cutsceneImage;
        if (storyText != null) storyText.text = page.storyText;

        if (useDebugLog) Debug.Log($"현재 스토리: {currentPage + 1} / {currentStoryData.pages.Length}");
    }

    private void EndStory()
    {
        if (useDebugLog) Debug.Log("스토리 종료. 스테이지로 이동.");
        
        // 다시 보지 않도록 저장
        PlayerPrefs.SetInt("HasSeenStory", 1);
        PlayerPrefs.Save();
        
        // 로딩 매니저를 통해 이동
        LoadingManager.LoadScene("Stage_Scene");
    }
}
