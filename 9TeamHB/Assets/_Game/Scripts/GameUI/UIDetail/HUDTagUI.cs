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
    public Image prohibitionImage;
    public Image prohibitionBackgroundImage;

    public Color disabledColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    private Color defaultTagColor;
    private RectTransform buttonRectTransform;
    private int previousCount = -1;

    [Header("떨림 관련 설정")]
    public float duration = 0.2f;
    public float magnitude = 8f;

    private void Awake()
    {
        if (tagImage != null) defaultTagColor = tagImage.color;
        if (tagButton != null)
        {
            buttonRectTransform = tagButton.GetComponent<RectTransform>();

            // Tab 키로 UI 버튼이 활성화되는 것 방지 (InputAction과 중복 호출 차단)
            Navigation nav = tagButton.navigation;
            nav.mode = Navigation.Mode.None;
            tagButton.navigation = nav;
        }
    }

    public void UpdateTagUI(int currentCount, int maxCount)
    {
        if (countText != null) countText.text = $"{currentCount} / {maxCount}";

        if (currentCount <= 0)
        {
            if (tagButton != null) tagButton.interactable = false;
            if (tagImage != null) tagImage.color = disabledColor;
            if (prohibitionImage != null) prohibitionImage.gameObject.SetActive(true);
            if (prohibitionBackgroundImage != null) prohibitionBackgroundImage.gameObject.SetActive(true);

            if (previousCount == 1)
                StartCoroutine(ShakeRoutine());
        }
        else
        {
            if (tagButton != null) tagButton.interactable = true;
            if (tagImage != null) tagImage.color = defaultTagColor;
            if (countText != null) countText.gameObject.SetActive(true);
            if (prohibitionImage != null) prohibitionImage.gameObject.SetActive(false);
            if (prohibitionBackgroundImage != null) prohibitionBackgroundImage.gameObject.SetActive(false);
        }

        previousCount = currentCount;
    }

    private IEnumerator ShakeRoutine()
    {
        Vector3 originalPos = buttonRectTransform.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = originalPos.x + Random.Range(-1f, 1f) * magnitude;
            float y = originalPos.y + Random.Range(-1f, 1f) * magnitude;
            buttonRectTransform.anchoredPosition = new Vector3(x, y, originalPos.z);
            elapsed += Time.deltaTime;
            yield return null;
        }

        buttonRectTransform.anchoredPosition = originalPos;
    }

    public void OnClickTagButton()
    {
        InGameUIManager.Instance.OnClickTagButton();
    }
}