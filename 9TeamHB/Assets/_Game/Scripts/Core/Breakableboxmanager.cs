using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyGame2.Stage
{
    public sealed class BreakableBoxManager : MonoBehaviour
    {
        [Header("씬 참조")]
        [SerializeField] private StageManager stageManager;

        [Header("파괴 애니메이션 스프라이트")]
        [SerializeField] private Sprite[] frames;

        [Header("연출 시간")]
        [SerializeField] private float breakDuration = 0.4f;

        private readonly HashSet<int> _trackedIds = new HashSet<int>();
        private readonly HashSet<int> _breakingIds = new HashSet<int>();

        private void OnEnable()
        {
            if (stageManager == null)
            {
                Debug.LogError("[BBM] stageManager null! 인스펙터 연결 필요");
                return;
            }
            stageManager.Events.TurnExecuted += OnTurnExecuted;
            stageManager.Events.StageLoaded += OnStageLoaded;
            Debug.Log("[BBM] 이벤트 구독 완료");
        }

        private void OnDisable()
        {
            if (stageManager == null) return;
            stageManager.Events.TurnExecuted -= OnTurnExecuted;
            stageManager.Events.StageLoaded -= OnStageLoaded;
        }

        private void Start()
        {
            if (stageManager != null && stageManager.CurrentState != null)
                CollectBreakableBoxes();
        }

        private void OnStageLoaded(int idx)
        {
            _trackedIds.Clear();
            _breakingIds.Clear();
            CollectBreakableBoxes();
        }

        private void CollectBreakableBoxes()
        {
            StageState state = stageManager.CurrentState;
            if (state == null) return;

            foreach (EntityState e in state.Entities)
            {
                if (e.IsBox && e.Has<BreakableData>() && e.IsAlive)
                    _trackedIds.Add(e.Id);
            }
            Debug.Log($"[BBM] 부서지는 상자 {_trackedIds.Count}개 추적");
        }

        private void OnTurnExecuted(TurnOutcome outcome)
        {
            CheckAll();
        }

        private void CheckAll()
        {
            if (stageManager == null) return;
            StageState state = stageManager.CurrentState;
            if (state == null) return;

            List<int> toBreak = null;

            foreach (int id in _trackedIds)
            {
                if (_breakingIds.Contains(id)) continue;
                if (!state.TryGetEntity(id, out EntityState entity)) continue;
                if (!entity.Has<BreakableData>()) continue;

                BreakableData data = entity.Get<BreakableData>();
                if (data.IsBreaking)
                {
                    Debug.Log($"[BBM] ID={id} IsBreaking 감지!");
                    if (toBreak == null) toBreak = new List<int>(2);
                    toBreak.Add(id);
                }
            }

            if (toBreak == null) return;

            for (int i = 0; i < toBreak.Count; i++)
            {
                int id = toBreak[i];
                _breakingIds.Add(id);

                if (state.TryGetEntity(id, out EntityState box))
                {
                    float cs = 1f;
                    Vector3 pos = box.Position.ToWorld(cs);
                    Debug.Log($"[BBM] 애니메이션 시작 ID={id} pos={pos} frames={frames?.Length}");
                    StartCoroutine(AnimateBreak(pos, id));
                }
            }
        }

        private IEnumerator AnimateBreak(Vector3 position, int entityId)
        {
            if (frames == null || frames.Length < 2)
            {
                Debug.LogError("[BBM] frames 비어있음!");
                yield break;
            }

            GameObject temp = new GameObject($"BreakFX_{entityId}");
            temp.transform.position = position;
            SpriteRenderer sr = temp.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 1;
            sr.sprite = frames[0];

            float elapsed = 0f;
            int total = frames.Length;

            while (elapsed < breakDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / breakDuration);
                int idx = Mathf.Clamp(Mathf.FloorToInt(t * (total - 1)), 0, total - 1);
                sr.sprite = frames[idx];
                yield return null;
            }

            yield return new WaitForSeconds(0.1f);
            Destroy(temp);

            StageState state = stageManager.CurrentState;
            if (state != null && state.TryGetEntity(entityId, out _))
            {
                state.RemoveEntity(entityId);
                state.SetViewDirty();
            }

            _trackedIds.Remove(entityId);
            _breakingIds.Remove(entityId);
            Debug.Log($"[BBM] 파괴 완료 ID={entityId}");
        }
    }
}