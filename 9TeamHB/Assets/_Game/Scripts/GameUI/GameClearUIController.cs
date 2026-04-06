using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameClearUIController : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string titleSceneName = "Title_Scene"; // 타이틀 씬 이름

    [Header("통계 텍스트 (프리팹 내 기존 TMP에 연결)")]
    [Tooltip("이동 횟수 표시용 TMP (프리팹의 MoveCount 텍스트 드래그)")]
    [SerializeField] private TextMeshProUGUI moveCountText;
    [Tooltip("태그 횟수 표시용 TMP (프리팹에 새로 추가하거나 기존 텍스트 드래그)")]
    [SerializeField] private TextMeshProUGUI tagCountText;
    [Tooltip("클리어 타임 표시용 TMP (프리팹의 PlayTime 텍스트 드래그)")]
    [SerializeField] private TextMeshProUGUI clearTimeText;

    public TextMeshProUGUI stageTitleText;

    private void OnEnable()
    {
        stageTitleText.text = InGameUIManager.Instance.stageTitleText;
    }
    
    // InGameUIManager.ShowGameClear()에서 호출
    public void SetClearStats(int moves, int tags, float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);

        if (moveCountText != null)
            moveCountText.text = moves.ToString();

        if (tagCountText != null)
            tagCountText.text = tags.ToString();

        if (clearTimeText != null)
            clearTimeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public void OnClickMainButton()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(titleSceneName);
    }

    public void OnClickNextStageButton()
    {
        Time.timeScale = 1f;

        // 같은 씬 내에서 다음 스테이지 로드 (씬 리로드 X)
        var stageMgr = FindAnyObjectByType<MyGame2.Stage.StageManager>();
        if (stageMgr != null)
        {
            InGameUIManager.Instance?.CloseGameClear();
            stageMgr.LoadNextStage();
        }
    }
}