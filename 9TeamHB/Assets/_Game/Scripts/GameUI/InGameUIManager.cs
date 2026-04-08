using MyGame2.Stage;
using System.Collections;
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

    [Header("튜토리얼 단축키 팝업")]
    [Tooltip("튜토리얼 1에서 표시할 단축키 안내 팝업 프리팹")]
    public GameObject tutorialShortcutPrefab;
    private GameObject activeTutorialPopup;
    private bool _tutorialShown;

    [Header("씬 참조")]
    [Tooltip("StageManager")]
    [SerializeField] private StageManager stageManager;

    [Header("되돌리기 설정 (시간제)")]
    [Tooltip("한 스테이지 최대 되돌리기 시간 (초)")]
    public float maxUndoSeconds = 20f;
    private float remainingUndoSeconds;
    private bool isUndoActive;

    [Header("스테이지 정보 관련")]
    public int stageCount;
    public bool isTutorialStage;
    public string stageTitleText;
    private HUDController hudController;

    [Header("태그 설정")]
    public int maxTagCount = 3;
    private int currentTagCount;

    private GameObject activeHUD;
    private GameObject activePausePopup;
    private GameObject activeSetting;
    private GameObject activeGameClear;
    private GameObject activeGameQuit;

    private HUDUndoUI hudUndoUI;
    private HUDTagUI hudTagUI;

    public float timeElapsed = 0f;

    public int MoveCount { get; private set; }
    public int TagCount { get; private set; }

    private int _savedMoveCount;
    private int _savedTagCount;
    private float _savedClearTime;

    private int _lastTagFrame = -1;
    private int _lastUndoFrame = -1;

    private Coroutine _autoNextCoroutine;
    private Coroutine _gameOverCoroutine;

    private UndoRecorder _undoRecorder;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        _undoRecorder = FindAnyObjectByType<UndoRecorder>();
    }

    private void OnEnable()
    {
        if (stageManager != null)
        {
            stageManager.Events.StageLoaded += OnStageLoaded;
            stageManager.Events.TurnExecuted += OnTurnExecuted;
            stageManager.Events.StageClearTriggered += OnStageClear;
            stageManager.Events.WarpComplete += OnWarpComplete;
            stageManager.Events.GameOverTriggered += OnGameOver;
        }
        LocalizationManager.LanguageChangedEvent += OnLanguageChanged;
    }

    private void OnDisable()
    {
        if (stageManager != null)
        {
            stageManager.Events.StageLoaded -= OnStageLoaded;
            stageManager.Events.TurnExecuted -= OnTurnExecuted;
            stageManager.Events.StageClearTriggered -= OnStageClear;
            stageManager.Events.WarpComplete -= OnWarpComplete;
            stageManager.Events.GameOverTriggered -= OnGameOver;
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

        if (isUndoActive)
        {
            if (!_undoRecorder.IsRewindable())
            {
                OnReleaseUndoButton();
            }

            // 추적자가 소환되면 Undo 즉시 해제
            if (stageManager != null && stageManager.CurrentState != null
                && stageManager.CurrentState.ChaserIds.Count > 0)
            {
                OnReleaseUndoButton();
            }

            remainingUndoSeconds -= Time.unscaledDeltaTime;
            if (remainingUndoSeconds <= 0f)
            {
                remainingUndoSeconds = 0f;
                OnReleaseUndoButton();
            }
        }

        if (hudUndoUI != null)
            hudUndoUI.UpdateUndoUI(remainingUndoSeconds, maxUndoSeconds);
    }

    // 스테이지 이벤트 핸들러

    private void OnStageLoaded(int stageIndex)
    {
        if (_autoNextCoroutine != null)
        {
            StopCoroutine(_autoNextCoroutine);
            _autoNextCoroutine = null;
        }

        if (_gameOverCoroutine != null)
        {
            StopCoroutine(_gameOverCoroutine);
            _gameOverCoroutine = null;
        }

        ResetAll();
        CloseGameClear();
        CloseGameQuit();
        CloseTutorialPopup();

        SetStageCount(stageIndex, isTutorialStage);
        SetTagCount(stageIndex);
        RefreshTagUI();
        RefreshUndoUI();

        // 튜토리얼 1 (stageIndex 0)에서 단축키 팝업 1회 표시
        if (stageIndex == 0 && !_tutorialShown)
        {
            _tutorialShown = true;
            ShowTutorialPopup();
        }
    }

    private void OnTurnExecuted(TurnOutcome outcome)
    {
        if (outcome.Executed && outcome.PlayerMove.CanMove)
            MoveCount++;
    }

    private void OnStageClear()
    {
        _savedMoveCount = MoveCount;
        _savedTagCount = TagCount;
        _savedClearTime = timeElapsed;

        string key = "BestTagRecord_" + stageCount;
        int prev = PlayerPrefs.GetInt(key, -1);
        if (prev < 0 || _savedTagCount < prev)
        {
            PlayerPrefs.SetInt(key, _savedTagCount);
            PlayerPrefs.Save();
        }
    }

    private void OnWarpComplete()
    {
        ShowGameClear();
        _autoNextCoroutine = StartCoroutine(AutoNextStageCoroutine());
    }

    private IEnumerator AutoNextStageCoroutine()
    {
        yield return new WaitForSecondsRealtime(2f);

        _autoNextCoroutine = null;
        CloseGameClear();

        if (stageManager != null)
            stageManager.LoadNextStage();
    }

    private void OnGameOver()
    {
        _gameOverCoroutine = StartCoroutine(GameOverSequence());
    }

    private IEnumerator GameOverSequence()
    {
        ShowGameQuit();

        yield return new WaitForSecondsRealtime(2f);

        _gameOverCoroutine = null;
        CloseGameQuit();
        ExecuteGameQuitRetry();
    }

    private void ResetAll()
    {
        if (isUndoActive && stageManager != null && stageManager.CurrentState != null)
            stageManager.CurrentState.UndoLeave();

        if (InGameSoundManager.Instance != null)
            InGameSoundManager.Instance.SuppressSFX = false;

        timeElapsed = 0f;
        MoveCount = 0;
        TagCount = 0;
        currentTagCount = maxTagCount;
        remainingUndoSeconds = maxUndoSeconds;
        isUndoActive = false;

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

            if (isUndoActive)
                OnReleaseUndoButton();

            if (_undoRecorder != null)
                _undoRecorder.ClearHistory();
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

    private void SetTagCount(int index)
    {
        maxTagCount = 3;
        switch (index)
        {
            case 0: maxTagCount = 4; break;
            case 1: maxTagCount = 5; break;
            case 2: maxTagCount = 4; break;
            case 3: maxTagCount = 7; break;
            case 4: maxTagCount = 4; break;
            case 5: maxTagCount = 5; break;
            case 6: maxTagCount = 9; break;
            case 7: maxTagCount = 5; break;
            case 8: maxTagCount = 6; break;
            case 9: maxTagCount = 5; break;
            case 10: maxTagCount = 6; break;
            case 11: maxTagCount = 6; break;
            case 12: maxTagCount = 6; break;
            case 13: maxTagCount = 5; break;
            case 14: maxTagCount = 8; break;
        }
        currentTagCount = maxTagCount;
    }

    // Canvas 카메라 연결 (Screen Space - Camera용)
    private void SetCanvasCamera(GameObject canvasObj)
    {
        if (canvasObj == null) return;
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            canvas.worldCamera = Camera.main;
        }
    }

    // Undo

    public void OnClickUndoButton()
    {
        if (Time.frameCount == _lastUndoFrame) return;
        _lastUndoFrame = Time.frameCount;

        if (stageManager == null || stageManager.CurrentState == null) return;
        if (remainingUndoSeconds <= 0f) return;

        // 추적형 감시자가 활성 중이면 Undo 불가
        if (stageManager.CurrentState.ChaserIds.Count > 0) return;

        if (_undoRecorder != null && _undoRecorder.IsRewindable())
        {
            isUndoActive = true;
            stageManager.CurrentState.UndoEnter();

            if (InGameSoundManager.Instance != null)
                InGameSoundManager.Instance.SuppressSFX = true;
        }
    }

    public void OnReleaseUndoButton()
    {
        if (!isUndoActive) return;

        isUndoActive = false;

        if (InGameSoundManager.Instance != null)
            InGameSoundManager.Instance.SuppressSFX = false;

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
        bool isPauseOn = activePausePopup != null && activePausePopup.activeSelf;
        bool isSettingOn = activeSetting != null && activeSetting.activeSelf;
        bool isClearOn = activeGameClear != null && activeGameClear.activeSelf;
        bool isQuitOn = activeGameQuit != null && activeGameQuit.activeSelf;

        if (isPauseOn || isSettingOn || isClearOn || isQuitOn)
            Time.timeScale = 0f;
        else
            Time.timeScale = 1f;
    }

    // UI 표시 / 닫기

    public void ShowHUD()
    {
        if (activeHUD == null)
        {
            activeHUD = Instantiate(hudPrefab);
            SetCanvasCamera(activeHUD);
        }
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

        if (hudController != null && LocalizationManager.Instance != null)
        {
            stageTitleText = hudController.UpdateStageText(stageCount, isTutorialStage);
        }
    }

    public void SetStageCount(int stgCount, bool isTutorial = false)
    {
        stageCount = stgCount;
        isTutorialStage = isTutorial;

        if (hudController != null && LocalizationManager.Instance != null)
        {
            stageTitleText = hudController.UpdateStageText(stageCount, isTutorial);
        }
    }

    private void OnLanguageChanged()
    {
        if (hudController != null && LocalizationManager.Instance != null)
        {
            stageTitleText = hudController.UpdateStageText(stageCount, isTutorialStage);
        }
    }

    public void ShowPausePopup()
    {
        if (activePausePopup == null)
        {
            activePausePopup = Instantiate(pausePopupPrefab);
            SetCanvasCamera(activePausePopup);
        }
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
        {
            activeSetting = Instantiate(settingPrefab);
            SetCanvasCamera(activeSetting);
        }
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
        InGameSoundManager.Instance?.PlayGameClearPopup();

        if (activeGameClear == null)
        {
            activeGameClear = Instantiate(gameClearPrefab);
            SetCanvasCamera(activeGameClear);
        }
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
        InGameSoundManager.Instance?.PlayGameOverPopup();

        if (activeGameQuit == null)
        {
            activeGameQuit = Instantiate(gameQuitPrefab);
            SetCanvasCamera(activeGameQuit);
        }
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

    // 튜토리얼 단축키 팝업

    public void ShowTutorialPopup()
    {
        if (tutorialShortcutPrefab == null) return;

        if (activeTutorialPopup == null)
        {
            activeTutorialPopup = Instantiate(tutorialShortcutPrefab);
            SetCanvasCamera(activeTutorialPopup);
        }
        else
            activeTutorialPopup.SetActive(true);

        Time.timeScale = 0f;
    }

    public void CloseTutorialPopup()
    {
        if (activeTutorialPopup != null)
            activeTutorialPopup.SetActive(false);
        UpdateTimeScale();
    }
}