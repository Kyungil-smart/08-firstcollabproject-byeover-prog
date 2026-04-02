using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingManager : MonoBehaviour
{
    public static string nextSceneName;
    public float minimumLoadingTime = 5f;

    [Header("UI Settings")]
    public Slider loadingBar; // 로딩 바 연결용

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

            // 실제 씬 로딩 진행도 (0.0 ~ 0.9 범위를 0.0 ~ 1.0으로 변환)
            float loadProgress = op.progress / 0.9f;

            // 우리가 설정한 5초 대기 시간 진행도 (0.0 ~ 1.0)
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