using UnityEngine;

public class StageCardCreator : MonoBehaviour
{
    public GameObject stageCardPrefab;
    public Transform contentTransform;
    
    [Header("캐러셀 연결")]
    public StageCarousel stageCarousel;
    private int _globalIndex = 0;

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

    private void Update()
    {
        // Home키: 클리어 기록 초기화 + 카드 잠금 상태 갱신
        if (Input.GetKeyDown(KeyCode.Home))
        {
            StageProgressManager.ResetAll(15);
            RefreshAllCards();
        }
    }

    private void RefreshAllCards()
    {
        _globalIndex = 0;
        foreach (Transform child in contentTransform)
            Destroy(child.gameObject);
        stageCarousel.ClearCards();
        Start();
    }

    private void CreateCard(int index, bool isTutorial, int starCount)
    {
        GameObject newCard = Instantiate(stageCardPrefab, contentTransform);
        StageCard stageCard = newCard.GetComponent<StageCard>();
        RectTransform cardRect = newCard.GetComponent<RectTransform>();
        
        stageCarousel.AddStageCard(cardRect);
        int record = 0;
        
        bool isUnlocked = StageProgressManager.IsUnlocked(_globalIndex);
        
        stageCard.SetupCard(index, record, isTutorial, starCount, stageCarousel, cardRect, isUnlocked, _globalIndex);
        _globalIndex++;
    }
}