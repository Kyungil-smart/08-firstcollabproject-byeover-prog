using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MyGame2
{
    // 옵션 씬에서 설정 완료 후 스토리 씬으로 전환하는 매니저
    
    public class OptionSceneManager : MonoBehaviour
    {
        [Header("씬 전환")]
        [Tooltip("전환할 씬 이름")]
        [SerializeField] private string nextSceneName = "Story_Scene";

        [Header("UI 참조")]
        [Tooltip("씬 전환 버튼 (Setting_Panel 아래 Button)")]
        [SerializeField] private Button startButton;

        private void Start()
        {
            if (startButton != null)
            {
                startButton.onClick.AddListener(OnStartButtonClicked);
            }
        }
        
        // 버튼 클릭 시 스토리 씬으로 전환
        private void OnStartButtonClicked()
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}