using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class StageCarousel : MonoBehaviour
{
    [Header("UI 구성요소")]
    public ScrollRect scrollRect;
    public RectTransform content;

    [Header("설정값")]
    public float lerpSpeed = 10f;
    public float centerScale = 1.2f;
    public float normalScale = 0.8f;

    private List<RectTransform> stageCards = new List<RectTransform>(); 
    private RectTransform targetCard; // 클릭해서 이동할 목표 카드
    private bool isSnapping = false;

    void Update()
    {
        // 유저가 화면을 클릭/드래그하면 자동 이동 멈춤
        if (Input.GetMouseButtonDown(0))
        {
            isSnapping = false;
        }

        // 스냅 이동 처리 (월드 좌표 기준)
        if (isSnapping && targetCard != null)
        {
            // 1. 뷰포트(보이는 화면)의 실제 정중앙 X좌표 계산
            Vector3[] viewportCorners = new Vector3[4];
            scrollRect.viewport.GetWorldCorners(viewportCorners);
            float viewportCenterX = (viewportCorners[0].x + viewportCorners[2].x) / 2f;

            // 2. 목표 카드의 실제 정중앙 X좌표 계산
            Vector3[] cardCorners = new Vector3[4];
            targetCard.GetWorldCorners(cardCorners);
            float cardCenterX = (cardCorners[0].x + cardCorners[2].x) / 2f;

            // 3. 뷰포트 중앙과 카드 중앙의 거리 차이 계산
            float difference = viewportCenterX - cardCenterX;

            // 4. 차이만큼 Content 전체를 부드럽게 이동
            float newX = Mathf.Lerp(content.position.x, content.position.x + difference, Time.deltaTime * lerpSpeed);
            content.position = new Vector3(newX, content.position.y, content.position.z);

            // 오차가 아주 작아지면 이동 종료
            if (Mathf.Abs(difference) < 0.05f) 
            {
                isSnapping = false;
            }
        }

        UpdateCardScale();
    }

    public void AddStageCard(RectTransform newCard)
    {
        stageCards.Add(newCard);
    }

    public void OnCardClicked(RectTransform clickedCard)
    {
        scrollRect.velocity = Vector2.zero; // 스크롤 관성 정지
        targetCard = clickedCard;           // 목표 카드를 등록
        isSnapping = true;                  // 이동 시작
    }

    private void UpdateCardScale()
    {
        // 화면 크기와 중앙 위치를 미리 가져옵니다.
        Vector3[] viewportCorners = new Vector3[4];
        scrollRect.viewport.GetWorldCorners(viewportCorners);
        float viewportCenterX = (viewportCorners[0].x + viewportCorners[2].x) / 2f;
        float viewportWidth = viewportCorners[2].x - viewportCorners[0].x;

        foreach (RectTransform card in stageCards)
        {
            if (card == null) continue;

            Vector3[] cardCorners = new Vector3[4];
            card.GetWorldCorners(cardCorners);
            float cardCenterX = (cardCorners[0].x + cardCorners[2].x) / 2f;

            // 실제 화면상 중앙에서 얼마나 떨어져 있는지 확인
            float distance = Mathf.Abs(viewportCenterX - cardCenterX);
            
            // 화면 너비에 비례하여 크기가 줄어들도록 계산
            float scale = Mathf.Lerp(centerScale, normalScale, distance / (viewportWidth * 0.3f));
            scale = Mathf.Max(scale, normalScale); // 너무 작아지지 않게 방어
            
            card.localScale = new Vector3(scale, scale, 1f);
        }
    }
    
    public void StartAtCard(int index)
    {
        StartCoroutine(SetInitialPosition(index));
    }
    
    // 1프레임 대기 후 위치를 잡아주는 코루틴
    private System.Collections.IEnumerator SetInitialPosition(int index)
    {
        // UI LayoutGroup이 카드를 정렬할 시간을 주기 위해 1프레임 대기 (매우 중요!)
        yield return null; 

        // 만약 카드가 생성되지 않았다면 실행하지 않음
        if (stageCards.Count <= index) yield break;

        targetCard = stageCards[index];

        // 뷰포트와 카드의 중앙 좌표 계산
        Vector3[] viewportCorners = new Vector3[4];
        scrollRect.viewport.GetWorldCorners(viewportCorners);
        float viewportCenterX = (viewportCorners[0].x + viewportCorners[2].x) / 2f;

        Vector3[] cardCorners = new Vector3[4];
        targetCard.GetWorldCorners(cardCorners);
        float cardCenterX = (cardCorners[0].x + cardCorners[2].x) / 2f;

        float difference = viewportCenterX - cardCenterX;

        // Lerp(애니메이션) 없이 즉시 중앙으로 이동!
        content.position = new Vector3(content.position.x + difference, content.position.y, content.position.z);

        // 크기도 즉시 갱신
        UpdateCardScale();
    }
}