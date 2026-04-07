using UnityEngine;

public class EndingSceneController : MonoBehaviour
{
    [Tooltip("엔딩 연출 후 크레딧으로 넘어가기까지 대기 시간 (초)")]
    public float waitTime = 5f;

    private float _timer;

    private void Update()
    {
        _timer += Time.deltaTime;

        // 대기 시간 경과 또는 아무 키 입력 시 크레딧으로
        if (_timer >= waitTime || Input.anyKeyDown)
        {
            LoadingManager.LoadScene("Credit_Scene");
        }
    }
}