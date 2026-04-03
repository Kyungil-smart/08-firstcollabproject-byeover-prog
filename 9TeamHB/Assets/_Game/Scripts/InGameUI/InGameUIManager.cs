using TMPro;
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

    [Header("되돌리기 설정")]
    public int maxUndoCount = 3;        // 최대 되돌리기 횟수
    private int currentUndoCount;       // 현재 남은 되돌리기 횟수
    private HUDUndoUI hudUndoUI;        // HUD 스크립트 접근용
    
    [Header("태그 설정")]  
    public int maxTagCount = 3;
    private int currentTagCount;
    private HUDTagUI hudTagUI;

    [Header("이동 횟수 관련")]
    [HideInInspector] public int maxMoveCount;
    [HideInInspector] public int currentMoveCount;
    
    [Header("그 외 HUD 관련 설정")] 
    public int stageCount;
    public float timeElapsed = 0f;      // 흐른 시간 체크 변수 
    private HUDController hudController;
    public bool isTutorialStage; // 튜토리얼인지 스테이지인지 확인함. 

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
        //테스트 용 moveCount 값 세팅
        currentMoveCount = 0; 
        maxMoveCount = 3;
    }

    private void Start()
    {
        timeElapsed = 0f; 
        Time.timeScale = 1f; //시간 흐르게
        
        
        currentUndoCount = maxUndoCount;
        currentTagCount = maxTagCount;
        SetStageCount(1, true); // Todo: Stage Title Text변환확인용 
        ShowHUD();
    }

    private void OnEnable()
    {
        LocalizationManager.LanguageChangedEvent += OnLanguageChanged;
    }
    
    private void OnDisable()
    {
        LocalizationManager.LanguageChangedEvent -= OnLanguageChanged;
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
        
        if (isPauseOn || isSettingOn || isClearOn || isQuitOn)
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
        // 오브젝트 없으면 생성, 있으면 활성화
        if (activeHUD == null)
        {
            activeHUD = Instantiate(hudPrefab);
            
            hudUndoUI = activeHUD.GetComponentInChildren<HUDUndoUI>(true);
            hudTagUI = activeHUD.GetComponentInChildren<HUDTagUI>(true);
            hudController =  activeHUD.GetComponent<HUDController>();
        }
        else
        {
            activeHUD.SetActive(true);
        }
        
        if (hudUndoUI != null)
            hudUndoUI.UpdateUndoUI(currentUndoCount, maxUndoCount);

        if (hudTagUI != null)
            hudTagUI.UpdateTagUI(currentTagCount, maxTagCount);

        if (hudController != null)
        {
            hudController.UpdateStageText(stageCount, isTutorialStage);
            hudController.UpdateMoveCountText(currentMoveCount, maxMoveCount);
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
    
    // activeGameQuit의 GameClearUIController 스크립트를 가져와서 리트라이 하는 코드 가져오기.
    public void ExecuteGameQuitRetry()
    {
        if (activeGameQuit == null)
        {
            activeGameQuit = Instantiate(gameQuitPrefab);
            activeGameQuit.SetActive(false); 
        }
        
        GameQuitUIController quitUIController = activeGameQuit.GetComponent<GameQuitUIController>();
        if (quitUIController != null)
        {
            quitUIController.OnClickRetryButton();
        }
    }
    // Undo 버튼 눌릴시 실행되는 Undo로직(UI포함)
    public void OnClickUndoButton() 
    {
        if (currentUndoCount > 0)
        {
            var stageManager = FindAnyObjectByType<MyGame2.Stage.StageManager>();
            if (stageManager == null) return;

            
            bool entered = stageManager.TryEnterUndo();
            if (entered)
            {
                stageManager.LeaveUndo();
                
                currentUndoCount--;
                if (hudUndoUI != null)
                {
                    hudUndoUI.UpdateUndoUI(currentUndoCount, maxUndoCount);
                }
            }
        }
        else
        {
            Debug.Log("되돌리기 횟수를 모두 소모했습니다.");
        }
    }

    // 태그 버튼 눌릴시 실행되는 로직
    public void OnClickTagButton() 
    {
        if (currentTagCount > 0)
        {
            var stageManager = FindAnyObjectByType<MyGame2.Stage.StageManager>();
            if (stageManager == null) return;

            
            stageManager.SwitchActivePlayer();

            currentTagCount--;
            if (hudTagUI != null)
            {
                hudTagUI.UpdateTagUI(currentTagCount, maxTagCount);
            }
        }
        else
        {
            Debug.Log("태그 횟수가 다 닳은 상태입니다.");
        }
    }

    // Todo: 스테이지 Or 튜토리얼 진입시 몇 Stage인지 지정. (겜씬 HUD 보이기 전 반영)
    public void SetStageCount(int stgCount, bool isTutorial)
    {
        stageCount = stgCount;
        isTutorialStage = isTutorial; 
        if (hudController != null)
        {
            hudController.UpdateStageText(stageCount, isTutorial);
        }
    }

    // 이 함수를 사용하여 이동횟수 업데이트
    public void SetMoveCount(int crtMoveCount, int mxMoveCount)
    {
        maxMoveCount = mxMoveCount;
        if (hudController != null)
        {
            hudController.UpdateMoveCountText(crtMoveCount, mxMoveCount);
        }
    }
    
    // 언어 바뀔때 새로고침. 
    private void OnLanguageChanged()
    {
        if (hudController != null)
        {
            hudController.UpdateStageText(stageCount, isTutorialStage);
        }
    }
}