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
    private HUDTagUI hudTagUI;

    // 흐른 시간 체크 변수
    public float timeElapsed = 0f;

    // 스테이지 통계 (클리어 UI 표시용)
    public int MoveCount { get; private set; }
    public int TagCount { get; private set; }

    // 클리어 순간 스냅샷 (워프 연출 중에도 값이 보존됨)
    private int _savedMoveCount;
    private int _savedTagCount;
    private float _savedClearTime;

    // 동일 프레임 중복 호출 방지
    private int _lastTagFrame = -1;
    private int _lastUndoFrame = -1;

    // 자동 다음 스테이지 코루틴 참조 (중복 방지)
    private Coroutine _autoNextCoroutine;

    // 생명주기

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

        // Undo 시간 차감 (누르고 있는 동안)
        if (isUndoActive)
        {
            if (!_undoRecorder.IsRewindable())
            {
                OnReleaseUndoButton();  // 되돌리기 불가능한 상황에서는 자동 종료
            }

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
        // 자동 전환 코루틴이 남아있으면 정리
        if (_autoNextCoroutine != null)
        {
            StopCoroutine(_autoNextCoroutine);
            _autoNextCoroutine = null;
        }

        ResetAll();
        CloseGameClear();

        SetStageCount(stageIndex, isTutorialStage);
        SetTagCount(stageIndex);
        RefreshTagUI();
        RefreshUndoUI();
    }

    // 이동 성공 턴만 카운트
    private void OnTurnExecuted(TurnOutcome outcome)
    {
        if (outcome.Executed && outcome.PlayerMove.CanMove)
            MoveCount++;
    }

    // 수정: 클리어 판정 → 통계만 저장 (UI는 워프 끝난 후 표시)
    private void OnStageClear()
    {
        _savedMoveCount = MoveCount;
        _savedTagCount = TagCount;
        _savedClearTime = timeElapsed;
        // ShowGameClear()는 여기서 호출하지 않음!
        // 워프 이펙트가 끝난 후 OnWarpComplete()에서 호출
    }

    // 수정: 워프 연출 완료 → 클리어 UI 표시 → 2초 뒤 자동 다음 스테이지
    private void OnWarpComplete()
    {
        ShowGameClear();
        _autoNextCoroutine = StartCoroutine(AutoNextStageCoroutine());
    }

    private IEnumerator AutoNextStageCoroutine()
    {
        // 2초 대기 (Time.timeScale 영향 안 받음)
        yield return new WaitForSecondsRealtime(2f);

        _autoNextCoroutine = null;
        CloseGameClear();

        if (stageManager != null)
            stageManager.LoadNextStage();
    }

    // 게임 오버 코루틴 

    private void OnGameOver()
    {
        ShowGameQuit();
        _autoNextCoroutine = StartCoroutine(AutoGameOverCoroutine());
    }

    private IEnumerator AutoGameOverCoroutine()
    {
        // 2초 대기 (Time.timeScale 영향 안 받음)
        yield return new WaitForSecondsRealtime(2f);

        _autoNextCoroutine = null;
        CloseGameQuit();

        ExecuteGameQuitRetry();
    }

    // 타이머·예산·통계 전부 초기값으로
    private void ResetAll()
    {
        // 이전 스테이지에서 Undo 활성 상태였으면 정리
        if (isUndoActive && stageManager != null && stageManager.CurrentState != null)
            stageManager.CurrentState.UndoLeave();

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

            // 태그 후 Undo 이력 차단
            // 진행 중인 Undo가 있으면 먼저 종료
            if (isUndoActive)
                OnReleaseUndoButton();

            // UndoRecorder의 히스토리를 현재 시점으로 리셋
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

    // 스테이지 별 태그 카운터 최대치 조정
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

    // Undo (Space) — 시간 예산 + 플래그만 관리
    // 실제 스냅샷 녹화/복원은 UndoRecorder가 담당
    public void OnClickUndoButton()
    {
        if (Time.frameCount == _lastUndoFrame) return;
        _lastUndoFrame = Time.frameCount;

        if (stageManager == null || stageManager.CurrentState == null) return;
        if (remainingUndoSeconds <= 0f) return;


        if (_undoRecorder != null && _undoRecorder.IsRewindable())
        {
            isUndoActive = true;
            stageManager.CurrentState.UndoEnter();
        }
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
        InGameSoundManager.Instance?.PlayGameClearPopup();

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

        InGameSoundManager.Instance?.PlayGameOverPopup();

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