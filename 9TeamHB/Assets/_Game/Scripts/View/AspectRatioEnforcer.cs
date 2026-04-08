using UnityEngine;

public class AspectRatioEnforcer : MonoBehaviour
{
    [SerializeField] private float targetAspect = 16f / 9f;

    private void Start()
    {
        float currentAspect = (float)Screen.width / Screen.height;
        if (Mathf.Approximately(currentAspect, targetAspect)) return;

        if (currentAspect > targetAspect)
        {
            // 가로가 넓음 (32:9 등) → 좌우 검은 띠
            float scale = targetAspect / currentAspect;
            Rect rect = new Rect((1f - scale) / 2f, 0f, scale, 1f);
            GetComponent<Camera>().rect = rect;
        }
        else
        {
            // 세로가 넓음 → 상하 검은 띠
            float scale = currentAspect / targetAspect;
            Rect rect = new Rect(0f, (1f - scale) / 2f, 1f, scale);
            GetComponent<Camera>().rect = rect;
        }
    }
}
