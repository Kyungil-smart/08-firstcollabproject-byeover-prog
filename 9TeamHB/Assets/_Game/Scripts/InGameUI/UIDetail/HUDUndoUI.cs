using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDUndoUI : MonoBehaviour
{
    [Header("되돌리기 UI")]
    public Button undoButton; // Undo 최상단 버튼     
    public Image undoFillImage; // Image Type Filled로 한 남은횟수/총횟수 만큼 아래에서 위로 채워지는 이미지
    public Image undoIconImage; //가운데 되돌리기 아이콘.
    public TextMeshProUGUI undoCountText;
    private Color fillDefaultColor;
    private Color iconDefaultColor;
    
    private void Awake()
    {
        fillDefaultColor = undoFillImage.color;
        iconDefaultColor = undoIconImage.color;
       // Debug.Log("색칠 색: " + fillDefaultColor + "아이콘 색: " + iconDefaultColor);
    }
    public void UpdateUndoUI(int currentCount, int maxCount)
    {
        if (maxCount <= 0) return;
        
        float fillRatio = (float)currentCount / maxCount;
        
        // FIllImage 채귀
        if (undoFillImage != null)
        {
            undoFillImage.fillAmount = fillRatio;
        }
        
        // 횟수 0 이면 버튼 비활성화
        if (currentCount <= 0)
        {
            undoButton.interactable = false; 
            undoFillImage.color = new Color(0.1f, 0.5f, 0.1f, 1f);
            undoIconImage.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        }
        else
        {
            undoButton.interactable = true;
            undoFillImage.color = fillDefaultColor;
            undoIconImage.color = iconDefaultColor;
        }
        
        if (undoCountText != null)
        {
            undoCountText.text = $"{currentCount} / {maxCount}";
        }
    }
    
    // UI를 프리펩화 했기 때문에 싱글톤매니저 참조할수 있게 프리펩내부에 코드추가.
    public void OnClickUndoButton()
    {
        InGameUIManager.Instance.ExecuteUndo();
    }
}