using UnityEngine;
using UnityEngine.SceneManagement;

public class GameClearUIController : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string titleSceneName = "Title_Scene"; // 타이틀 씬 이름
    [SerializeField] private string nextStageSceneName = "Stage_Scene"; // 다음 스테이지 씬 이름(나중에 현재 스테이지 + 1으로 수정)

    public void OnClickMainButton()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(titleSceneName);
    }
    
    public void OnClickNextStageButton()
    {
        Time.timeScale = 1f;
        
        // 나중에 현재 스테이지 정보 갱신 후 씬 이동.
        SceneManager.LoadScene(nextStageSceneName);
    }
}
