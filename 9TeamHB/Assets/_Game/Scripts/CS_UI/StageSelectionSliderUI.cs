using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

[System.Serializable]
public struct SliderStageData
{
    public string stageNumberName;
    public Sprite thumbnail;
    public string difficulty;
    public bool isLocked;
}

public class StageSelectionSliderUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI gameLogoText;
    [SerializeField] private TextMeshProUGUI stageNumberText;
    [SerializeField] private Image stageThumbnail;
    [SerializeField] private TextMeshProUGUI difficultyText;

    [Header("Buttons")]
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button startButton;
    [SerializeField] private Button backButton;

    [Header("Scene Settings")]
    [SerializeField] private string targetSceneName = "Game_Scenes"; //추후에 GamePlay_Scene 등으로 변경할 필요 있음
    [SerializeField] private string backSceneName = "Title_Scene"; //맨 처음 시작화면

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

    // 무한 순환 로직 적용
    private void ChangeStage(int direction)
    {
        currentIndex += direction;

        // 인덱스가 0보다 작아지면(1번에서 왼쪽 클릭) 배열의 맨 끝(10번)으로 보냄
        if (currentIndex < 0)
        {
            currentIndex = stages.Length - 1;
        }
        // 인덱스가 배열 크기를 넘어가면(10번에서 오른쪽 클릭) 배열의 처음(1번)으로 보냄
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

        if (stageNumberText != null) stageNumberText.text = currentData.stageNumberName;
        if (difficultyText != null) difficultyText.text = currentData.difficulty;
        if (stageThumbnail != null && currentData.thumbnail != null)
        {
            stageThumbnail.sprite = currentData.thumbnail;
        }

        // 양쪽 화살표는 항상 켜질 수 있도록 제작함
        if (prevButton != null) prevButton.interactable = true;
        if (nextButton != null) nextButton.interactable = true;

        if (startButton != null)
        {
            startButton.interactable = !currentData.isLocked;
            if (stageThumbnail != null)
            {
                stageThumbnail.color = currentData.isLocked ? Color.gray : Color.white;
            }
        }

        if (useDebugLog) Debug.Log($"현재 화면: {currentData.stageNumberName} ");
    }

    public void OnClickStart()
    {
        if (useDebugLog) Debug.Log($"{stages[currentIndex].stageNumberName} 선택. {targetSceneName}으로 입장.");

        PlayerPrefs.SetInt("SelectedStage", currentIndex);
        PlayerPrefs.Save();
        SceneManager.LoadScene(targetSceneName);
    }

    // back 버튼
    public void OnClickBack()
    {
        if (useDebugLog) Debug.Log($"{backSceneName}으로 복귀.");
        SceneManager.LoadScene(backSceneName);
    }
}
