using TMPro;
using UnityEngine;

public class InGameUIManager : MonoBehaviour
{
    public static InGameUIManager Instance;

    [Header("UI 프리팹")]
    public GameObject hudPrefab;
    public GameObject pausePopupPrefab;
    public GameObject settingPrefab;
    public GameObject gameClearPrefab;
    public GameObject gameQuitPrefab;

    private GameObject activeHUD;
    private GameObject activePausePopup;
    private GameObject activeSetting;
    private GameObject activeGameClear;
    private GameObject activeGameQuit;

    [Header("되돌리기 설정 (시간제)")]
    [Tooltip("한 스테이지 최대 되돌리기 시간 (초)")]
    public float maxUndoSeconds = 20f;
    private float remainingUndoSeconds;
    private bool isUndoActive;
    private HUDUndoUI hudUndoUI;

    [Header("태그 설정")]
    public int maxTagCount = 3;
    private int currentTagCount;
    private HUDTagUI hudTagUI;

    [Header("이동 횟수 관련")]
    [HideInInspector] public int maxMoveCount;
    [HideInInspector] public int currentMoveCount;

    [Header("그 외 HUD 관련 설정")]
    public int stageCount;
    public string stageTitleText;       
    public float timeElapsed = 0f;      // 흐른 시간 체크 변수
    private HUDController hudController;
    public bool isTutorialStage;        // 튜토리얼인지 스테이지인지 확인함.

    public bool IsPausePopupActive => activePausePopup != null && activePausePopup.activeSelf;

    // 외부에서 태그 가능 여부 확인용
    public bool CanTag => currentTagCount > 0;

    // 외부에서 Undo 가능 여부 확인용
    public bool CanUndo => remainingUndoSeconds > 0f;

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
        currentMoveCount = 0;
        maxMoveCount = 3;
    }

    private void Start()
    {
        timeElapsed = 0f;
        Time.timeScale = 1f;

        remainingUndoSeconds = maxUndoSeconds;
        currentTagCount = maxTagCount;
        isUndoActive = false;

        ShowHUD();
        SetStageCount(1, true); // Todo: Stage Title Text 변환확인용
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

        // Undo 활성 중 시간 차감
        if (isUndoActive)
        {
            remainingUndoSeconds -= Time.unscaledDeltaTime;

            if (hudUndoUI != null)
                hudUndoUI.UpdateUndoUI(remainingUndoSeconds, maxUndoSeconds);

            // 시간 소진 -> 강제 종료
            if (remainingUndoSeconds <= 0f)
            {
                remainingUndoSeconds = 0f;
                ForceStopUndo();
            }
        }
    }

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

    // HUD

    public void ShowHUD()
    {
        if (activeHUD == null)
        {
            activeHUD = Instantiate(hudPrefab);
            hudUndoUI = activeHUD.GetComponentInChildren<HUDUndoUI>(true);
            hudTagUI = activeHUD.GetComponentInChildren<HUDTagUI>(true);
            hudController = activeHUD.GetComponent<HUDController>();
        }
        else
        {
            activeHUD.SetActive(true);
        }

        if (hudUndoUI != null)
            hudUndoUI.UpdateUndoUI(remainingUndoSeconds, maxUndoSeconds);

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

    // Setting

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

    // Game Clear

    public void ShowGameClear()
    {
        if (activeGameClear == null)
            activeGameClear = Instantiate(gameClearPrefab);
        else
            activeGameClear.SetActive(true);
        UpdateTimeScale();
    }

    public void CloseGameClear()
    {
        if (activeGameClear != null)
            activeGameClear.SetActive(false);
        UpdateTimeScale();
    }

    // Game Quit

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

    private int _lastUndoFrame = -1;

    public void OnClickUndoButton()
    {
        // 같은 프레임 중복 호출 차단
        if (Time.frameCount == _lastUndoFrame) return;
        _lastUndoFrame = Time.frameCount;

        if (remainingUndoSeconds <= 0f)
        {
            Debug.Log("되돌리기 시간을 모두 소모했습니다.");
            return;
        }

        var stageManager = FindAnyObjectByType<MyGame2.Stage.StageManager>();
        if (stageManager == null) return;

        bool entered = stageManager.TryEnterUndo();
        if (entered)
        {
            isUndoActive = true;
        }
    }

    public void OnReleaseUndoButton()
    {
        if (!isUndoActive) return;
        isUndoActive = false;

        var stageManager = FindAnyObjectByType<MyGame2.Stage.StageManager>();
        if (stageManager != null)
            stageManager.LeaveUndo();

        if (hudUndoUI != null)
            hudUndoUI.UpdateUndoUI(remainingUndoSeconds, maxUndoSeconds);
    }

    private void ForceStopUndo()
    {
        isUndoActive = false;

        var stageManager = FindAnyObjectByType<MyGame2.Stage.StageManager>();
        if (stageManager != null)
            stageManager.LeaveUndo();

        if (hudUndoUI != null)
            hudUndoUI.UpdateUndoUI(0f, maxUndoSeconds);
    }

    public bool IsUndoActive => isUndoActive;

    private int _lastTagFrame = -1;

    public bool TryTag()
    {
        // 같은 프레임 중복 호출 차단 (Tab키 + UI버튼 동시 트리거 방지)
        if (Time.frameCount == _lastTagFrame) return false;
        _lastTagFrame = Time.frameCount;

        if (currentTagCount <= 0)
        {
            Debug.Log("태그 횟수가 다 닳은 상태입니다.");
            return false;
        }

        var stageManager = FindAnyObjectByType<MyGame2.Stage.StageManager>();
        if (stageManager == null) return false;

        bool switched = stageManager.SwitchActivePlayer();
        if (!switched) return false;

        currentTagCount--;
        if (hudTagUI != null)
            hudTagUI.UpdateTagUI(currentTagCount, maxTagCount);

        return true;
    }

    public void OnClickTagButton()
    {
        TryTag();
    }

    
    // Todo: 스테이지 Or 튜토리얼 진입시 몇 Stage인지 지정. (겜씬 HUD 보이기 전 반영)
    public void SetStageCount(int stgCount, bool isTutorial)
    {
        stageCount = stgCount;
        isTutorialStage = isTutorial; 
        if (hudController != null)
        {
            // Todo: 게임 클리어 시 기획에 맞게 해당 스테이지 타이틀 텍스트 + "\n" + "Clear!" 텍스트 표시. 
            stageTitleText = hudController.UpdateStageText(stageCount, isTutorial);
            //Debug.Log(stageTitleText);
        }
    }

    // 이 함수를 사용하여 이동횟수 업데이트 (이동횟수 표시 폐기되어서 사용X)
    public void SetMoveCount(int crtMoveCount, int mxMoveCount)
    {
        maxMoveCount = mxMoveCount;
        if (hudController != null)
            hudController.UpdateMoveCountText(crtMoveCount, mxMoveCount);
    }
    
    // 언어 바뀔때 새로고침. 
    private void OnLanguageChanged()
    {
        if (hudController != null)
        {
            stageTitleText = hudController.UpdateStageText(stageCount, isTutorialStage);
            //Debug.Log(stageTitleText);
        }
    }
}