using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleUIManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject keyboardPanel;
    // optionPanel을 추후에 추가할 곳

    
    // 환경설정 버튼 눌렀을때 생성되는 세팅 캔버스 프리펩
    [SerializeField] private GameObject settingPrefab; 
    private GameObject activeSetting; // 프리펩으로 생성된 세팅 캔버스 오브젝트를 저장.
    
    [Header("Scene Settings")]
    [SerializeField] private string startSceneName = "Stage_Scene";

    [Header("Debug")]
    [SerializeField] private bool useDebugLog = false;
    

    private void Start()
    {
        InGameSoundManager.Instance?.PlayTitleBGM();
    }
    
    // 게임시작 버튼 연결용
    public void OnClickGameStart()
    {
        if (useDebugLog) Debug.Log("게임 시작. Story_Scene으로 이동합니다.");
        
        // 스토리씬으로 보내기
        LoadingManager.LoadScene(startSceneName);
    }

    // 조작키 버튼 연결용 (패널 열기)
    public void OpenKeyboardPanel()
    {
        if (keyboardPanel == null) return;
        
        keyboardPanel.SetActive(true);
        if (useDebugLog) Debug.Log("조작키 안내.");
    }

    // 조작키 보고 나가기용 버튼 연결용 (패널 닫기)
    public void CloseKeyboardPanel()
    {
        if (keyboardPanel == null) return;
        
        keyboardPanel.SetActive(false);
    }

    // 게임종료 버튼 연결용
    public void OnClickQuit()
    {
        if (useDebugLog) Debug.Log("게임을 종료.");
        
        Application.Quit();
    }
    
    public void OnClickOption()
    {
        // 한 번도 연 적이 없으면 프리팹을 복제해서 화면에 띄움
        if (activeSetting == null)
        {
            activeSetting = Instantiate(settingPrefab);
        }
        else // 이미 만들어둔 게 있다면 활성화만 .
        {
            activeSetting.SetActive(true);
        }
    }
}