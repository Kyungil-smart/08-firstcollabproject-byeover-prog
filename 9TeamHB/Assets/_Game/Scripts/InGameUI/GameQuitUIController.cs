using UnityEngine;
using UnityEngine.SceneManagement;

public class GameQuitUIController : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string titleSceneName = "Title_Scene"; // 타이틀 씬 이름

    public void OnClickMainButton()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(titleSceneName);
    }
    
    // 추후 리트라이 기능 추가 .
    public void OnClickRetryButton()
    {
        Time.timeScale = 1f;
    }
}