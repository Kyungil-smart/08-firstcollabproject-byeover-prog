using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDUndoUI : MonoBehaviour
{
    [Header("되돌리기(Undo) UI")]
    public Button undoButton;
    public Image undoFillImage;
    public Image undoIconImage;
    public TextMeshProUGUI undoCountText;
    public Image prohibitionImage;
    public Image prohibitionBackgroundImage;

    [Header("러프 이동 설정")]
    public float fillSpeed = 8f;
    private float targetFillRatio = 1f;

    private Color fillDefaultColor;

    private void Awake()
    {
        if (undoFillImage != null)
        {
            fillDefaultColor = undoFillImage.color;
            undoFillImage.fillAmount = 1f;
        }
        // Space 키로 UI 버튼이 활성화되는 것 방지
        if (undoButton != null)
        {
            Navigation nav = undoButton.navigation;
            nav.mode = Navigation.Mode.None;
            undoButton.navigation = nav;
        }
    }

    private void Update()
    {
        if (undoFillImage != null)
        {
            undoFillImage.fillAmount = Mathf.Lerp(
                undoFillImage.fillAmount, targetFillRatio,
                Time.unscaledDeltaTime * fillSpeed);
        }
    }

    // 시간제: remainingSeconds / maxSeconds
    public void UpdateUndoUI(float remainingSeconds, float maxSeconds)
    {
        if (maxSeconds <= 0f) return;

        targetFillRatio = Mathf.Clamp01(remainingSeconds / maxSeconds);

        // 시간 표시 (소수점 1자리)
        if (undoCountText != null)
        {
            undoCountText.text = $"{Mathf.CeilToInt(remainingSeconds)}s";
        }

        if (remainingSeconds <= 0f)
        {
            if (undoButton != null) undoButton.interactable = false;
            if (prohibitionImage != null) prohibitionImage.gameObject.SetActive(true);
            if (prohibitionBackgroundImage != null) prohibitionBackgroundImage.gameObject.SetActive(true);
        }
        else
        {
            if (undoButton != null) undoButton.interactable = true;
            if (prohibitionImage != null) prohibitionImage.gameObject.SetActive(false);
            if (prohibitionBackgroundImage != null) prohibitionBackgroundImage.gameObject.SetActive(false);
        }
    }

    public void OnClickUndoButton()
    {
        InGameUIManager.Instance.OnClickUndoButton();
    }
}