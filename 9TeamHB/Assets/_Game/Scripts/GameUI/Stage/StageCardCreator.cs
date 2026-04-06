using UnityEngine;

public class StageCardCreator : MonoBehaviour
{
    public GameObject stageCardPrefab; // Prefab/UI/StageCard 프리펩
    public Transform contentTransform; // Scroll View 안의 Content 오브젝트
    
    [Header("캐러셀 연결")]
    public StageCarousel stageCarousel;
    private int maxUnlockedStage = 1;
    private int _globalIndex = 0; // stageFiles 배열 순서와 매핑되는 순차 인덱스
    void Start()
    {
        
        maxUnlockedStage = PlayerPrefs.GetInt("MaxUnlockedStage", 1);
        // 튜토리얼 1, 2
        for (int i = 1; i <= 2; i++) CreateCard(i, true, 1);
        
        // 스테이지 1
        for (int i = 1; i <= 1; i++) CreateCard(i, false, 1);
        
        // 튜토리얼 3, 4, 5
        for (int i = 3; i <= 5; i++) CreateCard(i, true, 1);
        
        // 스테이지 2
        for (int i = 2; i <= 2; i++) CreateCard(i, false, 1);
        
        // 튜토리얼 6, 7
        for (int i = 6; i <= 7; i++) CreateCard(i, true, 1);
        
        // 스테이지 3, 4, 5
        for (int i = 3; i <= 5; i++) CreateCard(i, false, 2);
        
        // 스테이지 6, 7, 8
        for (int i = 6; i <= 8; i++) CreateCard(i, false, 3);
        
        stageCarousel.StartAtCard(0); // TOdo: 나중에 스테이지 시작 부분 바꿀필요있을때 변경.
    }

    // 중복되던 카드 생성 코드를 하나로 합친 함수
    private void CreateCard(int index, bool isTutorial, int starCount)
    {
        // Content의 자식으로 프리팹 생성
        GameObject newCard = Instantiate(stageCardPrefab, contentTransform);
        StageCard stageCard = newCard.GetComponent<StageCard>();
        RectTransform cardRect = newCard.GetComponent<RectTransform>();
        
        stageCarousel.AddStageCard(cardRect);
        int record = 0; // 저장된 최고기록
        
        bool isUnlocked = false;
        if (isTutorial)
        {
            isUnlocked = true; // 튜토리얼은 항상 열려있게 하려면 이렇게 둡니다.
        }
        else
        {
            if (index <= maxUnlockedStage) isUnlocked = true;
            else isUnlocked = false;
        }
       
        
        // 카드에 데이터 넣어주기 (캐러셀 정보와 자신의 RectTransform도 같이 넘겨줍니다)
        stageCard.SetupCard(index, record, isTutorial, starCount, stageCarousel, cardRect, isUnlocked, _globalIndex);
        _globalIndex++;
    }
}