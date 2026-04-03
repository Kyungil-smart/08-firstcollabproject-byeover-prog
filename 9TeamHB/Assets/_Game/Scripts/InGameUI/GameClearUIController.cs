using UnityEngine;

public class GameClearUIController : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string titleSceneName = "Title_Scene";

    public void OnClickMainButton()
    {
        Time.timeScale = 1f;
        LoadingManager.LoadScene(titleSceneName);
    }
    
    // 다음 스테이지: StageManager.LoadNextStage()로 같은 씬 내에서 로드
    public void OnClickNextStageButton()
    {
        Time.timeScale = 1f;

        // 클리어 UI 닫기
        if (InGameUIManager.Instance != null)
        {
            InGameUIManager.Instance.CloseGameClear();
        }

        // StageManager를 찾아서 다음 스테이지 로드
        var stageManager = FindAnyObjectByType<MyGame2.Stage.StageManager>();
        if (stageManager != null)
        {
            bool loaded = stageManager.LoadNextStage();
            if (!loaded)
            {
                // 마지막 스테이지였으면 타이틀로 복귀
                Debug.Log("[GameClearUI] 마지막 스테이지 — 타이틀로 이동합니다.");
                LoadingManager.LoadScene(titleSceneName);
            }
        }
        else
        {
            Debug.LogWarning("[GameClearUI] StageManager를 찾지 못했습니다.");
            LoadingManager.LoadScene(titleSceneName);
        }
    }
}