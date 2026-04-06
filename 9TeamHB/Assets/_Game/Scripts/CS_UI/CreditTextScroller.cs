 using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class CreditTextScroller : MonoBehaviour
{
    [Header("Scroll Settings")]
    [SerializeField] private float baseScrollSpeed = 150f; // 기본 속도
    [SerializeField] private float fastMultiplier = 5f;    // 꾹 누를 때 빨라지는 배수 (5배)
    [SerializeField] private float startY = -1500f;

    [Header("Input Settings")]
    [SerializeField] private InputActionReference speedUpAction; // 스페이스바/마우스 좌클릭 연결

    private RectTransform rectTransform;
    private TextMeshProUGUI tmpText;
    private string lastText = "";

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        tmpText = GetComponent<TextMeshProUGUI>();
    }

    private void Update()
    {
        if (tmpText != null && tmpText.text != lastText)
        {
            lastText = tmpText.text;
            rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, startY);
        }

        float currentSpeed = baseScrollSpeed;
        
        // 버튼을 꾹 누르고 있는 동안 속도를 곱해줍니다.
        if (speedUpAction != null && speedUpAction.action.IsPressed())
        {
            currentSpeed = baseScrollSpeed * fastMultiplier;
        }

        rectTransform.anchoredPosition += Vector2.up * (currentSpeed * Time.deltaTime);
    }
}