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
    public int maxUndoCount = 3;        // 최대 되돌리기 횟수 (나중에 스테이지 불러올때 InGameUIManager생성 후 InGameUIManager.maxUndoCount에 전달.)
    private int currentUndoCount;       // 현재 남은 되돌리기 횟수
    private HUDUndoUI hudUndoUI; // HUD 스크립트 접근용
    
    [Header("태그 설정")]  
    public int maxTagCount = 3;
    private int currentTagCount;
    private HUDTagUI hudTagUI;

    [Header("이동 횟수 관련")]
    public int maxMoveCount;
    public int currentMoveCount;
    
    [Header("그 외 HUD 관련 설정")] 
    public int stageCount;
    public float timeElapsed = 0f; //흐른 시간 체크 변수 
    private HUDController hudController;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject); //싱글톤인데 씬 바뀔시 데이터초기화, 인게임 시작될때 초기화되는 
        }
        currentMoveCount = 0;
        maxMoveCount = 3;
    }

    private void Start()
    {
        timeElapsed = 0f; 
        Time.timeScale = 1f; //시간 흐르게
        currentUndoCount = maxUndoCount;
        currentTagCount = maxTagCount;
        
        // SetStageCount(1); // 스테이지 텍스트 변화 테스트용 디버깅
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
        {
            hudUndoUI.UpdateUndoUI(currentUndoCount, maxUndoCount);
        }

        if (hudTagUI != null)
        {
            hudTagUI.UpdateTagUI(currentTagCount, maxTagCount);
        }

        if (hudController != null)
        {
            hudController.UpdateStageText(stageCount);
            hudController.UpdateMoveCountText(currentMoveCount, maxMoveCount);
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
    
    // Undo 버튼 눌릴시 실행되는 Undo로직(UI포함)
    public void OnClickUndoButton()
    {
        if (currentUndoCount > 0)
        {
            currentUndoCount--;

            // 팀원분이 만든 되돌리기 로직 실행
            

            // 
            if (hudUndoUI != null)
            {
                hudUndoUI.UpdateUndoUI(currentUndoCount, maxUndoCount);
            }
        }
        else
        {
            Debug.Log("되돌리기 횟수가 다닳은 상태입니다 ");
        }
    }

    
    /// <summary>
    /// 
    /// </summary>
    public void OnClickTagButton()
    {
        if (currentTagCount > 0)
        {
            currentTagCount--;
            
            // 팀원분이 만든 태그 로직 실행.

            if (hudTagUI != null)
            {
                hudTagUI.UpdateTagUI(currentTagCount, maxTagCount);
            }
        }
        else
        {
            Debug.Log("태그 횟수가 다 닳은 상태입니다. ");
        }
    }

    // 스테이지 진입시 몇 Stage인지 지정. (HUD 보일때 반영) InGameUiManager.SetStageCount(int stgCount); 
    public void SetStageCount(int stgCount)
    {
        stageCount = stgCount;
        
        if (hudController != null)
        {
            hudController.UpdateStageText(stageCount);
        }
    }

    // 이 함수를 사용하여 이동횟수 없데이트
    public void SetMoveCount(int crtMoveCount, int mxMoveCount)
    {
        maxMoveCount = mxMoveCount;
        if (hudController != null)
        {
            hudController.UpdateMoveCountText(crtMoveCount, mxMoveCount);
        }
    }
}