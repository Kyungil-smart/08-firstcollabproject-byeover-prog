using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video; // 비디오 제어용으로 추가함

public class LoadingManager : MonoBehaviour
{
    public static string nextSceneName;
    public float minimumLoadingTime = 5f;

    [Header("UI Settings")]
    public Slider loadingBar; // 로딩 바 연결용

    [Header("Video Settings")]
    public VideoPlayer videoPlayer; // 비디오 재생기
    public VideoClip koreanVideo;   // 한글 로딩 영상
    public VideoClip englishVideo;  // 영문 로딩 영상

    // 씬이 켜질 때 언어에 맞는 영상으로 넣기
    private void Awake()
    {
        // 비디오 플레이어와 번역 매니저가 존재하는지 먼저 확인
        if (videoPlayer != null && LocalizationManager.Instance != null)
        {
            // 변수 확인 후 영상 교체용
            if (LocalizationManager.Instance.currentLanguage == Language.English)
            {
                videoPlayer.clip = englishVideo; // 영문 테이프로 교체
            }
            else
            {
                videoPlayer.clip = koreanVideo;  // 한글 테이프로 교체
            }
        }
    }

    private void Start()
    {
        // 로딩 바 초기화
        if (loadingBar != null)
        {
            loadingBar.value = 0f;
        }

        StartCoroutine(LoadSceneProcess());
    }

    public static void LoadScene(string sceneName)
    {
        nextSceneName = sceneName;
        SceneManager.LoadScene("Loading_Scene");
    }

    private IEnumerator LoadSceneProcess()
    {
        float timer = 0f;
        AsyncOperation op = SceneManager.LoadSceneAsync(nextSceneName);
        op.allowSceneActivation = false;

        while (!op.isDone)
        {
            timer += Time.deltaTime;

            // 실제 씬 로딩 진행도
            float loadProgress = op.progress / 0.9f;

            // 우리가 설정한 5초 대기 시간 진행도
            float timeProgress = timer / minimumLoadingTime;

            // 로딩이 순식간에 되어도, 일단 5초 정도 걸려서 이뤄지게 제작함
            if (loadingBar != null)
            {
                loadingBar.value = Mathf.Min(loadProgress, timeProgress);
            }

            // 실제 로딩 완료+5초 지남 -> 씬 이동 허용
            if (op.progress >= 0.9f && timer >= minimumLoadingTime)
            {
                // 로딩바 다 채우기
                if (loadingBar != null) loadingBar.value = 1f;
                op.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}