using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleUIManager : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("새 게임 시 이동할 씬 이름")]
    [SerializeField] private string storySceneName = "Story_Scene";
    [Tooltip("스테이지 선택 시 이동할 씬 이름")]
    [SerializeField] private string stageSceneName = "Stage_Scene";

    [Header("Prefabs")]
    [Tooltip("환경설정 버튼을 눌렀을 때 생성되는 캔버스 프리팹")]
    [SerializeField] private GameObject settingPrefab;
    private GameObject activeSetting;

    [Header("Debug")]
    [SerializeField] private bool useDebugLog = false;
    
    // 👇 3번 문제 해결: s를 대문자 S로 바꿔야 유니티가 인식합니다!
    private void Start()
    {
        InGameSoundManager.Instance?.PlayTitleBGM();
    }
    
    // 게임시작 버튼 연결용
    public void OnClickGameStart()
    {
        if (useDebugLog) Debug.Log("새 게임 시작. 로딩 화면을 거쳐 Story_Scene으로 이동.");
        
        // 스토리씬으로 보내기
        LoadingManager.LoadScene(storySceneName);
    }

    // 2. 스테이지 선택 버튼
    public void OnClickStageSelect()
    {
        if (useDebugLog) Debug.Log("스테이지 선택. 로딩 화면을 거쳐 Stage_Scene으로 이동.");
        
        LoadingManager.LoadScene(stageSceneName);
    }

    // 3. 환경설정 버튼
    public void OnClickOption()
    {
        if (useDebugLog) Debug.Log("환경설정 창 열기.");

        if (activeSetting == null)
        {
            activeSetting = Instantiate(settingPrefab);
        }
        else
        {
            activeSetting.SetActive(true);
        }
    }
}