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

    // 추가: 일시정지 팝업 활성 여부 (HUDController ESC 토글용)
    public bool IsPausePopupActive => activePausePopup != null && activePausePopup.activeSelf;

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
        Time.timeScale = 1f;
        ShowHUD();
    }

    public void Update()
    {
        timeElapsed += Time.deltaTime; 
    }
    
    // 만약 HUD 외에 다른 UI가 켜진 경우 TimeScale 0로 설정해서 시간 안흐르게함. 
    private void UpdateTimeScale()
    {
        bool isPauseOn = activePausePopup != null && activePausePopup.activeSelf;
        bool isSettingOn = activeSetting != null && activeSetting.activeSelf;
        bool isClearOn = activeGameClear != null && activeGameClear.activeSelf;
        bool isQuitOn = activeGameQuit != null && activeGameQuit.activeSelf;
        
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
    
    // HUD

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

    // Pause

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
    
    // Setting

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

    // Game Clear

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

    // Game Quit

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

    // HUD 버튼 브릿지 (Undo / Tag)

    // 복원: HUDUndoUI.OnClickUndoButton()에서 호출
    // 버튼 클릭으로 되돌리기 1회 실행
    public void ExecuteUndo()
    {
        var stageManager = FindAnyObjectByType<MyGame2.Stage.StageManager>();
        if (stageManager == null) return;

        // 되돌리기 진입 -> 즉시 해제 (버튼 1회 클릭 = 1턴 되돌리기)
        bool entered = stageManager.TryEnterUndo();
        if (entered)
        {
            stageManager.LeaveUndo();
        }
    }

    // 복원: HUDTagUI.OnClickTagButton()에서 호출
    // 버튼 클릭으로 태그(플레이어 전환) 실행
    public void ExecuteTag()
    {
        var stageManager = FindAnyObjectByType<MyGame2.Stage.StageManager>();
        if (stageManager == null) return;

        stageManager.SwitchActivePlayer();
    }
}