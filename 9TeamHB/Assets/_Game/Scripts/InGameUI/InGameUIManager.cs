using UnityEngine;
using MyGame2.Stage;

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

    [Header("씬 참조")]
    [Tooltip("StageManager를 드래그해서 넣어줘")]
    [SerializeField] private StageManager stageManager;

    [Header("되돌리기 설정 (시간제)")]
    [Tooltip("한 스테이지 최대 되돌리기 시간 (초)")]
    public float maxUndoSeconds = 20f;
    private float remainingUndoSeconds;
    private bool isUndoActive;

    [Header("스테이지 정보 관련")]
    public int stageCount; // 몇스테이지인지.
    public bool isTutorialStage; //  현재 스테이지가 튜토리얼인지 일반 스테이지인지 여부
    public string stageTitleText; // 스테이지 타이틀.
    private HUDController hudController; // HUD 스크립트 접근용
    
    [Header("태그 설정")]
    public int maxTagCount = 3;
    private int currentTagCount;

    // 프리펩으로 생성된 실시간 활성화된 UI
    private GameObject activeHUD;
    private GameObject activePausePopup;
    private GameObject activeSetting;
    private GameObject activeGameClear;
    private GameObject activeGameQuit;

    // HUD 내부 sub-UI
    private HUDUndoUI hudUndoUI;
    private HUDTagUI  hudTagUI;

    // 흐른 시간 체크 변수
    public float timeElapsed = 0f;

    // 스테이지 통계 (클리어 UI 표시용)
    public int MoveCount { get; private set; }
    public int TagCount  { get; private set; }

    // 클리어 순간 스냅샷 (워프 연출 중에도 값이 보존됨)
    private int   _savedMoveCount;
    private int   _savedTagCount;
    private float _savedClearTime;

    // 동일 프레임 중복 호출 방지
    private int _lastTagFrame  = -1;
    private int _lastUndoFrame = -1;
    
    // 생명주기

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        if (stageManager != null)
        {
            stageManager.Events.StageLoaded        += OnStageLoaded;
            stageManager.Events.TurnExecuted        += OnTurnExecuted;
            stageManager.Events.StageClearTriggered += OnStageClear;
            stageManager.Events.WarpComplete        += OnWarpComplete;
        }
        LocalizationManager.LanguageChangedEvent += OnLanguageChanged;
    }

    private void OnDisable()
    {
        if (stageManager != null)
        {
            stageManager.Events.StageLoaded        -= OnStageLoaded;
            stageManager.Events.TurnExecuted        -= OnTurnExecuted;
            stageManager.Events.StageClearTriggered -= OnStageClear;
            stageManager.Events.WarpComplete        -= OnWarpComplete;
        }
        LocalizationManager.LanguageChangedEvent -= OnLanguageChanged;
    }

    private void Start()
    {
        ResetAll();
        Time.timeScale = 1f;
        ShowHUD();
    }

    public void Update()
    {
        timeElapsed += Time.deltaTime;

        // Undo 시간 차감 (누르고 있는 동안)
        if (isUndoActive)
        {
            remainingUndoSeconds -= Time.unscaledDeltaTime;
            if (remainingUndoSeconds <= 0f)
            {
                remainingUndoSeconds = 0f;
                OnReleaseUndoButton(); // 시간 다 쓰면 자동 종료
            }
        }

        // HUD Undo 게이지 실시간 갱신
        if (hudUndoUI != null)
            hudUndoUI.UpdateUndoUI(remainingUndoSeconds, maxUndoSeconds);
    }
    
    // 스테이지 이벤트 핸들러

    // 스테이지 로드 시 모든 것 초기화
    private void OnStageLoaded(int stageIndex)
    {
        ResetAll();
        CloseGameClear();

        SetStageCount(stageIndex, isTutorialStage);
        
        // 새 스테이지 초기 스냅샷 기록
        StageState state = stageManager.CurrentState;
        if (state != null)
            _undoStack.Push(StageSnapshot.Capture(state));
    }

    // 이동 성공 턴만 카운트
    private void OnTurnExecuted(TurnOutcome outcome)
    {
        if (outcome.Executed && outcome.PlayerMove.CanMove)
            MoveCount++;
    }

    // 클리어 판정 -> 스냅샷만 찍고 팝업은 워프 연출 대기
    private void OnStageClear()
    {
        _savedMoveCount = MoveCount;
        _savedTagCount  = TagCount;
        _savedClearTime = timeElapsed;
    }

    // 워프 연출 완료 -> 클리어 팝업 표시
    private void OnWarpComplete()
    {
        ShowGameClear();
    }

    // 타이머·예산·통계 전부 초기값으로
    private void ResetAll()
    {
        // 이전 스테이지에서 Undo 활성 상태였으면 정리
        if (isUndoActive && stageManager != null && stageManager.CurrentState != null)
            stageManager.CurrentState.UndoLeave();

        timeElapsed          = 0f;
        MoveCount            = 0;
        TagCount             = 0;
        currentTagCount      = maxTagCount;
        remainingUndoSeconds = maxUndoSeconds;
        isUndoActive         = false;

        RefreshTagUI();
        RefreshUndoUI();
    }
    
    // 태그 (Tab)

    public bool TryTag()
    {
        if (Time.frameCount == _lastTagFrame) return false;
        _lastTagFrame = Time.frameCount;

        if (stageManager == null || stageManager.CurrentState == null) return false;
        if (currentTagCount <= 0) return false;

        bool switched = stageManager.SwitchActivePlayer();
        if (switched)
        {
            currentTagCount--;
            TagCount++;
            RefreshTagUI();
        }
        return switched;
    }

    public void OnClickTagButton()
    {
        TryTag();
    }

    private void RefreshTagUI()
    {
        if (hudTagUI != null)
            hudTagUI.UpdateTagUI(currentTagCount, maxTagCount);
    }
    
    // Undo (Space) — 시간 예산 + 플래그만 관리
    // 실제 스냅샷 녹화/복원은 기존 Undorecorder가 담당

    public void OnClickUndoButton()
    {
        if (Time.frameCount == _lastUndoFrame) return;
        _lastUndoFrame = Time.frameCount;

        if (stageManager == null || stageManager.CurrentState == null) return;
        if (remainingUndoSeconds <= 0f) return;

        isUndoActive = true;
        stageManager.CurrentState.UndoEnter();
    }

    public void OnReleaseUndoButton()
    {
        if (!isUndoActive) return;

        isUndoActive = false;

        if (stageManager != null && stageManager.CurrentState != null)
            stageManager.CurrentState.UndoLeave();
    }

    private void RefreshUndoUI()
    {
        if (hudUndoUI != null)
            hudUndoUI.UpdateUndoUI(remainingUndoSeconds, maxUndoSeconds);
    }
    
    // 재시작

    public void ExecuteGameQuitRetry()
    {
        CloseGameQuit();
        if (stageManager != null)
            stageManager.RestartCurrentStage();
    }
    
    // 일시정지 프로퍼티

    public bool IsPausePopupActive
    {
        get { return activePausePopup != null && activePausePopup.activeSelf; }
    }
    
    // TimeScale 관리

    private void UpdateTimeScale()
    {
        bool isPauseOn   = activePausePopup != null && activePausePopup.activeSelf;
        bool isSettingOn = activeSetting    != null && activeSetting.activeSelf;
        bool isClearOn   = activeGameClear  != null && activeGameClear.activeSelf;
        bool isQuitOn    = activeGameQuit   != null && activeGameQuit.activeSelf;

        if (isPauseOn || isSettingOn || isClearOn || isQuitOn)
            Time.timeScale = 0f;
        else
            Time.timeScale = 1f;
    }
    
    // UI 표시 / 닫기

    public void ShowHUD()
    {
        if (activeHUD == null)
            activeHUD = Instantiate(hudPrefab);
        else
            activeHUD.SetActive(true);

        if (hudTagUI == null)
            hudTagUI = activeHUD.GetComponentInChildren<HUDTagUI>();
        if (hudUndoUI == null)
            hudUndoUI = activeHUD.GetComponentInChildren<HUDUndoUI>();
        
        if (hudController == null)
            hudController = activeHUD.GetComponent<HUDController>();
        
        RefreshTagUI();
        RefreshUndoUI();
        
        if (hudController != null)
        {
            stageTitleText = hudController.UpdateStageText(stageCount, isTutorialStage);
        }
    }

    public void SetStageCount(int stgCount, bool isTutorial = false)
    {
        stageCount = stgCount;
        isTutorialStage = isTutorial; 
        
        if (hudController != null)
        {
            stageTitleText = hudController.UpdateStageText(stageCount, isTutorial);
        }
    }
    
    private void OnLanguageChanged()
    {
        if (hudController != null)
        {
            stageTitleText = hudController.UpdateStageText(stageCount, isTutorialStage);
        }
    }
    
    public void ShowPausePopup()
    {
        if (activePausePopup == null)
            activePausePopup = Instantiate(pausePopupPrefab);
        else
            activePausePopup.SetActive(true);
        UpdateTimeScale();
    }

    public void ClosePausePopup()
    {
        if (activePausePopup != null)
            activePausePopup.SetActive(false);
        UpdateTimeScale();
    }

    public void ShowSettingPopup()
    {
        if (activeSetting == null)
            activeSetting = Instantiate(settingPrefab);
        else
            activeSetting.SetActive(true);
        UpdateTimeScale();
    }

    public void CloseSettingPopup()
    {
        if (activeSetting != null)
            activeSetting.SetActive(false);
        UpdateTimeScale();
    }

    public void ShowGameClear()
    {
        if (activeGameClear == null)
            activeGameClear = Instantiate(gameClearPrefab);
        else
            activeGameClear.SetActive(true);

        var ctrl = activeGameClear.GetComponent<GameClearUIController>();
        if (ctrl == null)
            ctrl = activeGameClear.GetComponentInChildren<GameClearUIController>();
        if (ctrl != null)
            ctrl.SetClearStats(_savedMoveCount, _savedTagCount, _savedClearTime);

        UpdateTimeScale();
    }

    public void CloseGameClear()
    {
        if (activeGameClear != null)
            activeGameClear.SetActive(false);
        UpdateTimeScale();
    }

    public void ShowGameQuit()
    {
        if (activeGameQuit == null)
            activeGameQuit = Instantiate(gameQuitPrefab);
        else
            activeGameQuit.SetActive(true);
        UpdateTimeScale();
    }

    public void CloseGameQuit()
    {
        if (activeGameQuit != null)
            activeGameQuit.SetActive(false);
        UpdateTimeScale();
    }
}