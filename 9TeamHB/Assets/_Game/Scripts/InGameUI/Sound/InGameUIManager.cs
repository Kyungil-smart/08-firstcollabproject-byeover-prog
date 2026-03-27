using UnityEngine;

public class InGameUIManager : MonoBehaviour
{
    // 싱글톤 사용
    public static InGameUIManager Instance;

    [Header("UI 프리팹")]
    public GameObject hudPrefab;
    public GameObject pausePopupPrefab;
    public GameObject settingPrefab;
    public GameObject gameClearPrefab;
    public GameObject gameQuitPrefab;
    
    //프리펩으로 생성된 실시간 활성화된 UI
    private GameObject activeHUD; 
    private GameObject activePausePopup;
    private GameObject activeSetting;
    private GameObject activeGameClear;
    private GameObject activeGameQuit;

    //흐른 시간 체크 변수  
    public float timeElapsed = 0f; 
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        timeElapsed = 0f; 
        Time.timeScale = 1f; //시간 흐르게
        ShowHUD();
    }

    public void Update()
    {
        timeElapsed += Time.deltaTime; 
        
         
    }
    
    // 만약 HUD 외에 다른 UI가 켜진 경우 TimeScale 0로 설정해서 시간 안흐르게함. 
    private void UpdateTimeScale()
    {
        // 팝업들이 instantiate로 생성되어 있고, activeSelf로 켜져 있는지 확인
        bool isPauseOn = activePausePopup != null && activePausePopup.activeSelf;
        bool isSettingOn = activeSetting != null && activeSetting.activeSelf;
        bool isClearOn = activeGameClear != null && activeGameClear.activeSelf;
        bool isQuitOn = activeGameQuit != null && activeGameQuit.activeSelf;
        
        // 나중에 메인 스테이지, 다음 스테이지 이런거 뜰때 초기화(Start에서 해서 안해도됨)
        // 
        
        // HUD외 다른 UI 하나라도 켜져 있다면 시간을 멈추기. 
        if (isPauseOn || isSettingOn)
        {
            Time.timeScale = 0f; 
        }
        else if (isClearOn || isQuitOn)
        {
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = 1f;
        }
    }
    
    // HUD 프리펩에서 오브젝트 생성
    public void ShowHUD()
    {
        if (activeHUD == null)
        {
            
            activeHUD = Instantiate(hudPrefab);
        }
        else
        {
            activeHUD.SetActive(true);
        }
    }

    
    public void ShowPausePopup()
    {
        if (activePausePopup == null)
        {
            activePausePopup = Instantiate(pausePopupPrefab);
        }
        else
        {
            activePausePopup.SetActive(true);
        }

        UpdateTimeScale();
    }

    public void ClosePausePopup()
    {
        if (activePausePopup != null)
        {
            activePausePopup.SetActive(false);
        }
        
        UpdateTimeScale();
    }
    
    public void ShowSettingPopup()
    {
        if (activeSetting == null)
        {
            activeSetting = Instantiate(settingPrefab);
        }
        else
        {
            activeSetting.SetActive(true);
        }
        
        UpdateTimeScale();
    }

    public void CloseSettingPopup()
    {
        if (activeSetting != null)
        {
            activeSetting.SetActive(false);
        }
        
        UpdateTimeScale();
    }

    public void ShowGameClear()
    {
        if (activeGameClear == null)
        {
            activeGameClear = Instantiate(gameClearPrefab);
        }
        else
        {
            activeGameClear.SetActive(true);
        }
        
        UpdateTimeScale();
    }

    public void CloseGameClear()
    {
        if (activeGameClear != null)
        {
            activeGameClear.SetActive(false);
        }
        
        UpdateTimeScale();
    }

    public void ShowGameQuit()
    {
        if (activeGameQuit == null)
        {
            activeGameQuit = Instantiate(gameQuitPrefab);
        }
        else
        {
            activeGameQuit.SetActive(true);
        }
        
        UpdateTimeScale();
    }
    
    public void CloseGameQuit()
    {
        if (activeGameQuit != null)
        {
            activeGameQuit.SetActive(false);
        }
        
        UpdateTimeScale();
    }
}