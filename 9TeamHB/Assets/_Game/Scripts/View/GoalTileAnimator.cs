using UnityEngine;
using MyGame2.Stage;

public class GoalTileAnimator : MonoBehaviour
{
    [Header("씬 참조")]
    [SerializeField] private StageManager stageManager;

    [Header("애니메이션 설정")]
    [SerializeField] private Sprite[] frames;       // ClearTile_0 ~ 12 드래그
    [SerializeField] private float frameInterval = 0.08f;
    [SerializeField] private int sortingOrder = -1;  // 타일 위, 엔티티 아래

    private readonly System.Collections.Generic.List<SpriteRenderer> _goalRenderers 
        = new System.Collections.Generic.List<SpriteRenderer>();
    private float _timer;
    private int _frameIndex;

    private void OnEnable()
    {
        if (stageManager != null)
            stageManager.Events.StageLoaded += OnStageLoaded;
    }

    private void OnDisable()
    {
        if (stageManager != null)
            stageManager.Events.StageLoaded -= OnStageLoaded;
    }

    private void Start()
    {
        if (stageManager != null && stageManager.CurrentState != null)
            SpawnGoalVisuals();
    }

    private void OnStageLoaded(int idx)
    {
        ClearAll();
        SpawnGoalVisuals();
    }

    private void SpawnGoalVisuals()
    {
        StageState state = stageManager.CurrentState;
        if (state == null || frames == null || frames.Length == 0) return;

        for (int y = 0; y < state.Height; y++)
        {
            for (int x = 0; x < state.Width; x++)
            {
                GridPos pos = new GridPos(x, y);
                if (!state.HasGoal(pos)) continue;

                GameObject go = new GameObject($"GoalAnim_{x}_{y}");
                go.transform.SetParent(transform);
                go.transform.position = pos.ToWorld(1f);

                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = frames[0];
                sr.sortingOrder = sortingOrder;

                _goalRenderers.Add(sr);
            }
        }
    }

    private void Update()
    {
        if (_goalRenderers.Count == 0 || frames.Length == 0) return;

        _timer += Time.deltaTime;
        if (_timer < frameInterval) return;
        _timer -= frameInterval;

        _frameIndex = (_frameIndex + 1) % frames.Length;

        for (int i = 0; i < _goalRenderers.Count; i++)
        {
            if (_goalRenderers[i] != null)
                _goalRenderers[i].sprite = frames[_frameIndex];
        }
    }

    private void ClearAll()
    {
        for (int i = 0; i < _goalRenderers.Count; i++)
        {
            if (_goalRenderers[i] != null)
                Destroy(_goalRenderers[i].gameObject);
        }
        _goalRenderers.Clear();
        _frameIndex = 0;
        _timer = 0f;
    }
}