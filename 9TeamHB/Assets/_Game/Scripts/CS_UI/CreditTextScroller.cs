using UnityEngine;
using TMPro;
using UnityEngine.UI; // 이미지 제어 기능 추가
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class CreditTextScroller : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private Image backgroundImage;      // 씬의 배경이 될 이미지 
    [SerializeField] private TextMeshProUGUI tmpText;    // 크레딧 텍스트

    [Header("데이터 설정")]
    [SerializeField] private Sprite firstImage;          // 첫 번째 배경 그림
    [SerializeField] private Sprite secondImage;         // 두 번째 배경 그림
    [SerializeField] private string firstTextKey = "";   // 첫 번째 텍스트 키 
    [SerializeField] private string secondTextKey = "";  // 두 번째 텍스트 키 
    
    [Header("스크롤 설정")]
    [SerializeField] private float baseScrollSpeed = 150f;
    [SerializeField] private float fastMultiplier = 5f;
    [SerializeField] private float startY = -1500f;
    [SerializeField] private float changeImagePosY = 0f; 

    [SerializeField] private float endPosY = 3000f;              // 텍스트가 이 높이를 넘어가면 끝나게 함.
    [SerializeField] private string titleSceneName = "Title_Scene";
    
    [Header("Input Settings")]
    [SerializeField] private InputActionReference speedUpAction; 

    private RectTransform rectTransform;
    private bool isImageChanged = false;
    private bool isEnded = false; //
    
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (tmpText == null) tmpText = GetComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        if (LocalizationManager.Instance != null)
        {
            // 번역 매니저에서 텍스트를 불러오기
            string text1 = LocalizationManager.Instance.GetText(firstTextKey);
            string text2 = LocalizationManager.Instance.GetText(secondTextKey);
            
            // 두 텍스트 사이에 줄바꿈 넣기
            tmpText.text = text1 + "\n\n\n\n\n\n\n\n" + text2; 
        }

        // 초기 위치 및 첫 번째 이미지 세팅
        rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, startY);
        
        if (backgroundImage != null && firstImage != null)
        {
            backgroundImage.sprite = firstImage;
        }
            
        isImageChanged = false;
    }

    private void Update()
    {
        if (isEnded) return;
        
        float currentSpeed = baseScrollSpeed;
        
        // 버튼을 꾹 누르면 배속
        if (speedUpAction != null && speedUpAction.action.IsPressed())
        {
            currentSpeed = baseScrollSpeed * fastMultiplier;
        }

        // 텍스트를 위로 이동
        rectTransform.anchoredPosition += Vector2.up * (currentSpeed * Time.deltaTime);

        // 텍스트가 일정 높이(changeImagePosY) 이상 올라가면 두 번째 이미지로 교체
        if (!isImageChanged && rectTransform.anchoredPosition.y > changeImagePosY)
        {
            if (backgroundImage != null && secondImage != null)
            {
                backgroundImage.sprite = secondImage;
                isImageChanged = true; // 한 번만 바뀌도록 잠금
            }
        }
        
        if (rectTransform.anchoredPosition.y > endPosY)
        {
            isEnded = true; 
            SceneManager.LoadScene(titleSceneName);
        }
    }
}