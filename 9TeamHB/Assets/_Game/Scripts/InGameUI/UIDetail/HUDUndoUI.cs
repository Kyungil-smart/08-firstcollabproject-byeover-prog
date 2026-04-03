using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDUndoUI : MonoBehaviour
{
    [Header("되돌리기(Undo) UI")]
    public Button undoButton; // Undo 최상단 버튼     
    public Image undoFillImage; // Image Type Filled로 한 남은횟수/총횟수 만큼 아래에서 위로 채워지는 이미지
    public Image undoIconImage; //가운데 되돌리기 아이콘.
    public TextMeshProUGUI undoCountText;
    public Image prohibitionImage; // 금지 이미지
    public Image prohibitionBackgroundImage;

    [Header("러프 이동 설정")]
    public float fillSpeed = 5f; // 게이지 줄어드는 속도
    private float targetFillRatio = 1f; // 매니저한테 받은 실제 게이지 목표치

    private Color fillDefaultColor;
    private Color iconDefaultColor;
    
    private void Awake()
    {
        if (undoFillImage != null)
        {
            fillDefaultColor = undoFillImage.color;
            undoFillImage.fillAmount = 1f; // 시작할때 게이지 꽉 차있게 초기화
        }
        //if (undoIconImage != null) iconDefaultColor = undoIconImage.color;
    }

    private void Update()
    {
        if (undoFillImage != null)
        {
            // Update에서 매 프레임마다 목표치(targetFillRatio)까지 스무스하게 게이지 변경
            undoFillImage.fillAmount = Mathf.Lerp(undoFillImage.fillAmount, targetFillRatio, Time.deltaTime * fillSpeed);
        }
    }

    public void UpdateUndoUI(int currentCount, int maxCount)
    {
        if (maxCount <= 0) return;
        
        // 게이지 즉시 안바꾸고 목표 비율만 갱신
        targetFillRatio = (float)currentCount / maxCount;

        // 횟수 0 이면 버튼 비활성화
        if (currentCount <= 0)
        {
            if (undoButton != null) undoButton.interactable = false; 
            //if (undoFillImage != null) undoFillImage.color = new Color(0.1f, 0.5f, 0.1f, 1f);
            //if (undoIconImage != null) undoIconImage.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            
            // 금지 이미지 On
            if (prohibitionImage != null) prohibitionImage.gameObject.SetActive(true);
            if (prohibitionBackgroundImage != null) prohibitionBackgroundImage.gameObject.SetActive(true);
        }
        else
        {
            if (undoButton != null) undoButton.interactable = true;
            //if (undoFillImage != null) undoFillImage.color = fillDefaultColor;
            //if (undoIconImage != null) undoIconImage.color = iconDefaultColor;
            
            // 금지 이미지 off
            if (prohibitionImage != null) prohibitionImage.gameObject.SetActive(false);
            if (prohibitionBackgroundImage != null) prohibitionBackgroundImage.gameObject.SetActive(false);
        }
        
        if (undoCountText != null)
        {
            undoCountText.text = $"{currentCount} / {maxCount}";
        }
    }
    
    // UI를 프리펩화 했기 때문에 싱글톤매니저 참조할수 있게 프리펩내부에 코드추가.
    public void OnClickUndoButton()
    {
        InGameUIManager.Instance.OnClickUndoButton();
    }
}