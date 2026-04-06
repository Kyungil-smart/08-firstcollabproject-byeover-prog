using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; 

// 마우스,키보드 인식을 위해 IPointerDownHandler(누를 때), IPointerUpHandler(뗄 때) 인터페이스 상속
public class HUDRestartUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("UI 연결")]
    public Image fillImage;           
    public RectTransform visualTarget; // 흔들림 대상
    
    [Header("흔들림 설정")]
    public float requiredHoldTime = 2f; // 홀딩 시간 
    public float pressedScaleFactor = 1.1f; // 순간적으로 확대될 크기 
    public float pulseMagnitude = 0.03f; // 계속 누를때 맥박 하는 진폭 
    public float pulseSpeed = 5f; // 맥박 속도 

    private float currentHoldTime = 0f; // 현재 누르고 있는 시간
    private bool isHolding = false;     // 누르는 상태인지 체크
    
    private Vector3 originalScale;      // 복구용 원래 크기
    private float pulseTime = 0f;       // 사인파 계산용 타이머

    private void Awake()
    {
        // 빈칸이면 자기 자신의 RectTransform을 넣음
        if (visualTarget == null) visualTarget = GetComponent<RectTransform>();
        
        // 원래 크기 저장
        originalScale = visualTarget.localScale;
        
        // 시작할 때 게이지 0으로 초기화
        if (fillImage != null) fillImage.fillAmount = 0f;
    }

    private void Update()
    {
        if (isHolding)
        {
            currentHoldTime += Time.deltaTime;
            // 맥박 연출용 타이머
            pulseTime += Time.deltaTime * pulseSpeed; 

            // 원형 게이지 채우기
            if (fillImage != null)
            {
                fillImage.fillAmount = currentHoldTime / requiredHoldTime;
            }

            // 맥박 연출
            if (visualTarget != null)
            {
                
                float baseScaleValue = pressedScaleFactor; //기본 크기
                
                float oscillationValue = Mathf.Sin(pulseTime) * pulseMagnitude; // 사인값에 진폭 곱한 값
                
                // 최종 크기 적용
                float finalScaleValue = baseScaleValue + oscillationValue; //기본크기 + 사인값으로 크기 변화
                visualTarget.localScale = new Vector3(finalScaleValue, finalScaleValue, 1f);
            }

            // 3초 도달 시 재시작
            if (currentHoldTime >= requiredHoldTime)
            {
                ExecuteRestart();
            }
        }
        else
        {
            if (currentHoldTime > 0f)
            {
                currentHoldTime -= Time.deltaTime * 4f; //되돌아가는 속도

                if (fillImage != null)
                {
                    fillImage.fillAmount = currentHoldTime / requiredHoldTime;
                }
                
                visualTarget.localScale = Vector3.Lerp(visualTarget.localScale, originalScale, Time.deltaTime * 10f); // 크기 Lerp로 부드럽게 복구 
            }
        }
    }

    // 이 UI를 마우스로 누를때
    public void OnPointerDown(PointerEventData eventData)
    {
        isHolding = true;
        
        pulseTime = 0f; 
    }

    // 이 UI를 마우스로 땔때
    public void OnPointerUp(PointerEventData eventData)
    {
        isHolding = false;
    }
    
    // 키로 누르는 경우?
    public void SetHoldingByKey(bool holding)
    {
        if (holding && !isHolding) 
        {
            pulseTime = 0f;
        }
        
        isHolding = holding;
    }
    
    // 재시작이 발동되었을 때만 호출되는 강제 리셋 함수
    private void ResetHold()
    {
        currentHoldTime = 0f;
        pulseTime = 0f;
        if (fillImage != null) fillImage.fillAmount = 0f;
        // 크기 복구
        visualTarget.localScale = originalScale; 
    }

    private void ExecuteRestart()
    {
        // 중복 실행 방지를 위해 홀딩 종료
        isHolding = false; 
        ResetHold(); 
        // 재시작 함수 호출
        if (InGameUIManager.Instance != null)
        {
            InGameUIManager.Instance.ExecuteGameQuitRetry();
        }
    }
}