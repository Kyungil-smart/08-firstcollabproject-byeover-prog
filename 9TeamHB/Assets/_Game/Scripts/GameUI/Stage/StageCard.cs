using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro를 쓴다면

public class StageCard : MonoBehaviour
{
    [Header("UI 연결")]
    public TextMeshProUGUI stageStringText;
    public TextMeshProUGUI stageNumText;
    public TextMeshProUGUI stageTitleText;
    public TextMeshProUGUI recordText; // 최고 기록 글자
    public Button cardButton;          // 클릭 버튼
    public bool isTutorial;
    
    public GameObject lockImage; // 자물쇠 이미지 오브젝트
    public Button startButton; // 스타트 버튼
    
    [Header("별 표시")]
    public GameObject[] starImages;
    
    private int _stageFilesIndex; // StageManager.stageFiles 배열 인덱스

    public void SetupCard(int stageIndex, int bestRecord, bool isTutorial, int starCount, StageCarousel carousel, RectTransform myRect, bool isUnlocked, int stageFilesIndex = -1)
    {
        _stageFilesIndex = stageFilesIndex >= 0 ? stageFilesIndex : stageIndex;

        if (isTutorial == false) stageStringText.text = LocalizationManager.Instance.GetText("Stage_Text");
        else stageStringText.text = LocalizationManager.Instance.GetText("Tutorial_Text");

        if (isTutorial == false)
            stageTitleText.text = LocalizationManager.Instance.GetText("Stage" + stageIndex.ToString() + "_Title_Text");
        else stageTitleText.text = "";
        
        stageNumText.text = stageIndex.ToString();
        recordText.text = bestRecord.ToString();
        
        cardButton.onClick.RemoveAllListeners();
        cardButton.onClick.AddListener(() => OnClickStageCard(stageIndex));
        cardButton.onClick.AddListener(() => carousel.OnCardClicked(myRect));
        
        // 스타트 버튼 — 씬 로드 연결
        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(OnClickStart);
        }

        if (isUnlocked)
        {
            lockImage.SetActive(false);
            startButton.gameObject.SetActive(true);
        }
        else
        {
            lockImage.SetActive(true);
            startButton.gameObject.SetActive(false);
        }
        
        UpdateStars(starCount);
    }

    private void UpdateStars(int count)
    {
        int maxStars = starImages.Length;
        int activeStars = Mathf.Clamp(count, 0, maxStars);
        
        for (int i = 0; i < maxStars; i++)
        {
            if (i < activeStars)
            {
                starImages[i].SetActive(true);  // starCount 범위 안이면 켜기
            }
            else
            {
                starImages[i].SetActive(false); // 범위를 벗어나면 끄기 
            }
        }
    }
    
    // 스테이지 클릭.
    private void OnClickStageCard(int stageIndex)
    {
        Debug.Log(stageIndex + "번 스테이지 클릭됨");
    }

    // 플레이 버튼 클릭 — 게임 씬 로드
    private void OnClickStart()
    {
        PlayerPrefs.SetInt("SelectedStage", _stageFilesIndex);
        PlayerPrefs.Save();
        LoadingManager.LoadScene("Game_Scenes");
    }
}