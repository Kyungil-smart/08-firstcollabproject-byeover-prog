using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections; 

public class HUDTagUI : MonoBehaviour
{
    [Header("태그 UI 연결")]
    public Button tagButton;
    public Image tagImage;
    public TextMeshProUGUI countText;
    public Image prohibitionImage; // 금지 이미지
    
    public Color disabledColor = new Color(0.3f, 0.3f, 0.3f, 1f); // 다 썼을 때 변할 어두운 회색

    private Color defaultTagColor;       // 원래 색상 기억용
    private RectTransform buttonRectTransform; // UI를 흔들기 위해 위치 정보 기억용
    private int previousCount = -1;      // 방금 전 횟수 기억용

    [Header("떨림 관련 설정")]
    public float duration = 0.2f; // 떠는 시간  
    public float magnitude = 8f;  // 떠는 진도
    private void Awake()
    {
        if (tagImage != null) defaultTagColor = tagImage.color;
        
        buttonRectTransform = tagButton.GetComponent<RectTransform>(); //버튼을 흔들기 위해 가져옴
    }

    // 태그 값 변경시 UI 수정
    public void UpdateTagUI(int currentCount, int maxCount)
    {
        if (countText != null) countText.text = currentCount.ToString(); //텍스트 변경.
        
        // 카운트 0될때
        if (currentCount <= 0)
        {
            // 버튼 처리
            if (tagButton != null) tagButton.interactable = false;
            if (tagImage != null) tagImage.color = disabledColor;
            
            // 금지 이미지 On
            if (prohibitionImage != null) prohibitionImage.gameObject.SetActive(true);
            
            // 값이 1 -> 0 일때만 흔들기 발생 (0 -> 0 발생X)
            if (previousCount == 1)
            {
                StartCoroutine(ShakeRoutine());
            }
        }
        else //횟수 남아있는 경우.
        {
            if (tagButton != null) tagButton.interactable = true;
            if (tagImage != null) tagImage.color = defaultTagColor;
            
            if (countText != null) countText.gameObject.SetActive(true);
            if (prohibitionImage != null) prohibitionImage.gameObject.SetActive(false);
        }

        // 1 -> 0 인지 체크를 위한 변수
        previousCount = currentCount;
    }
    
    private IEnumerator ShakeRoutine()
    {
        
        
        Vector3 originalPos = buttonRectTransform.anchoredPosition; // 원래 위치 기억
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // 상하좌우 무작위로 위치를 살짝씩 쉐이크
            float x = originalPos.x + Random.Range(-1f, 1f) * magnitude;
            float y = originalPos.y + Random.Range(-1f, 1f) * magnitude;
            
            buttonRectTransform.anchoredPosition = new Vector3(x, y, originalPos.z);
            
            elapsed += Time.deltaTime;
            yield return null; // 다음 프레임까지 대기
        }

        //원래 위치로 복구
        buttonRectTransform.anchoredPosition = originalPos;
    }

    // 버튼 클릭 시 매니저 호출
    public void OnClickTagButton()
    {
        InGameUIManager.Instance.ExecuteTag(); 
    }
}