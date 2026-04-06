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
    
    [Header("별(Star) 표시")]
    public GameObject[] starImages;
    
    public void SetupCard(int stageIndex, int bestRecord, bool isTutorial, int starCount, StageCarousel carousel, RectTransform myRect, bool isUnlocked)
    {
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
        
        if (isUnlocked)
        {
            lockImage.SetActive(false); // 자물쇠 숨기기
            startButton.gameObject.SetActive(true); // 시작 버튼 켜기
        }
        else
        {
            lockImage.SetActive(true); // 자물쇠 보여주기
            startButton.gameObject.SetActive(false); // 시작 버튼 숨기기 
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
}