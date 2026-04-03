using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public struct SliderStageData
{
    public string stageNumberName;
    public Sprite thumbnail;
    public string difficulty;
    // isLocked 제거 — 런타임에 StageProgressManager로 판정
}

public class StageSelectionSliderUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI gameLogoText;
    [SerializeField] private TextMeshProUGUI stageNumberText;
    [SerializeField] private Image stageThumbnail;
    [SerializeField] private TextMeshProUGUI difficultyText;
    [SerializeField] private TextMeshProUGUI clearStatusText;    // 클리어 여부 표시 (선택)

    [Header("Buttons")]
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button startButton;
    [SerializeField] private Button backButton;

    [Header("Scene Settings")]
    [SerializeField] private string targetSceneName = "Game_Scenes";
    [SerializeField] private string backSceneName = "Title_Scene";

    [Header("Stage Data")]
    [SerializeField] private SliderStageData[] stages;
    [SerializeField] private bool useDebugLog = false;

    private int currentIndex = 0;

    private void Start()
    {
        if (prevButton != null) prevButton.onClick.AddListener(() => ChangeStage(-1));
        if (nextButton != null) nextButton.onClick.AddListener(() => ChangeStage(1));
        if (startButton != null) startButton.onClick.AddListener(OnClickStart);
        if (backButton != null) backButton.onClick.AddListener(OnClickBack);

        UpdateStageUI();
    }

    // 무한 순환 로직
    private void ChangeStage(int direction)
    {
        currentIndex += direction;

        if (currentIndex < 0)
        {
            currentIndex = stages.Length - 1;
        }
        else if (currentIndex >= stages.Length)
        {
            currentIndex = 0;
        }

        UpdateStageUI();
    }

    private void UpdateStageUI()
    {
        if (stages == null || stages.Length == 0) return;

        SliderStageData currentData = stages[currentIndex];

        // 기본 정보 표시
        if (stageNumberText != null) stageNumberText.text = currentData.stageNumberName;
        if (difficultyText != null) difficultyText.text = currentData.difficulty;
        if (stageThumbnail != null && currentData.thumbnail != null)
        {
            stageThumbnail.sprite = currentData.thumbnail;
        }

        // 런타임 잠금/클리어 판정
        bool isUnlocked = StageProgressManager.IsUnlocked(currentIndex);
        bool isCleared = StageProgressManager.IsCleared(currentIndex);

        // 양쪽 화살표는 항상 활성
        if (prevButton != null) prevButton.interactable = true;
        if (nextButton != null) nextButton.interactable = true;

        // 시작 버튼: 해금된 스테이지만 활성
        if (startButton != null)
        {
            startButton.interactable = isUnlocked;
        }

        // 썸네일: 잠긴 스테이지는 회색
        if (stageThumbnail != null)
        {
            stageThumbnail.color = isUnlocked ? Color.white : Color.gray;
        }

        // 클리어 상태 텍스트 (clearStatusText가 없으면 무시)
        if (clearStatusText != null)
        {
            if (!isUnlocked)
            {
                clearStatusText.text = "LOCKED";
                clearStatusText.color = Color.gray;
            }
            else if (isCleared)
            {
                clearStatusText.text = "CLEAR!";
                clearStatusText.color = Color.yellow;
            }
            else
            {
                clearStatusText.text = "";
            }
        }

        if (useDebugLog)
        {
            Debug.Log($"[StageSelect] {currentData.stageNumberName} " +
                      $"| index={currentIndex} | unlocked={isUnlocked} | cleared={isCleared}");
        }
    }

    public void OnClickStart()
    {
        if (!StageProgressManager.IsUnlocked(currentIndex))
        {
            Debug.LogWarning("[StageSelect] 잠긴 스테이지는 시작할 수 없습니다.");
            return;
        }

        if (useDebugLog)
        {
            Debug.Log($"{stages[currentIndex].stageNumberName} 선택. " +
                      $"index={currentIndex} → {targetSceneName}으로 입장.");
        }

        // StageManager가 읽을 인덱스를 PlayerPrefs에 저장
        PlayerPrefs.SetInt("SelectedStage", currentIndex);
        PlayerPrefs.Save();

        LoadingManager.LoadScene(targetSceneName);
    }

    public void OnClickBack()
    {
        if (useDebugLog) Debug.Log($"{backSceneName}으로 복귀.");
        LoadingManager.LoadScene(backSceneName);
    }
}