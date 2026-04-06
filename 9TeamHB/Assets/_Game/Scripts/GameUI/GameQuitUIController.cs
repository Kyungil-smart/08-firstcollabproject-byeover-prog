using UnityEngine;
using UnityEngine.SceneManagement;

public class GameQuitUIController : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string titleSceneName = "Title_Scene";

    public void OnClickMainButton()
    {
        Time.timeScale = 1f;
        LoadingManager.LoadScene(titleSceneName);
    }
    
    // 리트라이: StageManager.RestartCurrentStage()로 현재 스테이지 재시작
    public void OnClickRetryButton()
    {
        Time.timeScale = 1f;

        // GameQuit UI 닫기
        if (InGameUIManager.Instance != null)
        {
            InGameUIManager.Instance.CloseGameQuit();
        }

        // StageManager를 찾아서 현재 스테이지 재시작
        var stageManager = FindAnyObjectByType<MyGame2.Stage.StageManager>();
        if (stageManager != null)
        {
            stageManager.RestartCurrentStage();
        }
        else
        {
            // StageManager를 못 찾으면 현재 씬 리로드 (폴백)
            Debug.LogWarning("[GameQuitUI] StageManager를 찾지 못해 씬을 리로드합니다.");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}