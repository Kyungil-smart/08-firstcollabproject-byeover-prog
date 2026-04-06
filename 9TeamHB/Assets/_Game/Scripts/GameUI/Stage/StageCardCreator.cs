using UnityEngine;

public class StageCardCreator : MonoBehaviour
{
    public GameObject stageCardPrefab; // Prefab/UI/StageCard 프리펩
    public Transform contentTransform; // Scroll View 안의 Content 오브젝트
    
    [Header("캐러셀 연결")]
    public StageCarousel stageCarousel;
    private int _globalIndex = 0; // stageFiles 배열 인덱스와 동일
    void Start()
    {
        // 튜토리얼 1~5 (stageFiles 0~4)
        for (int i = 1; i <= 5; i++) CreateCard(i, true, 1);
        
        // 스테이지 1, 2 (stageFiles 5~6)
        for (int i = 1; i <= 2; i++) CreateCard(i, false, 1);
        
        // 튜토리얼 6, 7 (stageFiles 7~8)
        for (int i = 6; i <= 7; i++) CreateCard(i, true, 1);
        
        // 스테이지 3~5 (stageFiles 9~11)
        for (int i = 3; i <= 5; i++) CreateCard(i, false, 2);
        
        // 스테이지 6~8 (stageFiles 12~14)
        for (int i = 6; i <= 8; i++) CreateCard(i, false, 3);
        
        stageCarousel.StartAtCard(0);
    }

    // 중복되던 카드 생성 코드를 하나로 합친 함수
    private void CreateCard(int index, bool isTutorial, int starCount)
    {
        GameObject newCard = Instantiate(stageCardPrefab, contentTransform);
        StageCard stageCard = newCard.GetComponent<StageCard>();
        RectTransform cardRect = newCard.GetComponent<RectTransform>();
        
        stageCarousel.AddStageCard(cardRect);
        int record = 0;
        
        bool isUnlocked = isTutorial || StageProgressManager.IsUnlocked(_globalIndex);
        
        stageCard.SetupCard(index, record, isTutorial, starCount, stageCarousel, cardRect, isUnlocked, _globalIndex);
        _globalIndex++;
    }
}