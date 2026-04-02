using System.Collections;
using UnityEngine;

namespace MyGame2.Stage
{
    // 부서지는 상자 프리팹에 부착.
    // InteractableTileVisual과 동일한 이벤트 구독 패턴 사용.

    public class BreakableBoxVisual : MonoBehaviour
    {
        [Header("씬 참조")]
        [SerializeField] private StageManager stageManager;

        [Header("스프라이트 프레임")]
        [Tooltip("[0]=온전, [1]=금간, [2~]=부서지는 프레임")]
        [SerializeField] private Sprite[] frames;

        [Header("연출 시간")]
        [SerializeField] private float breakDuration = 0.4f;

        [Header("대상 SpriteRenderer (비우면 자식에서 자동 탐색)")]
        [SerializeField] private SpriteRenderer targetSpriteRenderer;

        private SpriteRenderer _spriteRenderer;
        private int _entityId;
        private bool _initialized;
        private bool _breaking;

        private void Awake()
        {
            _spriteRenderer = targetSpriteRenderer != null
                ? targetSpriteRenderer
                : GetComponentInChildren<SpriteRenderer>();
        }

        private void OnEnable()
        {
            if (stageManager == null) return;
            stageManager.Events.TurnExecuted += OnTurnExecuted;
            stageManager.Events.UndoExecuted += OnUndoExecuted;
        }

        private void OnDisable()
        {
            // 파괴 애니메이션 중이면 구독 해제하지 않음
            if (_breaking) return;
            if (stageManager == null) return;
            stageManager.Events.TurnExecuted -= OnTurnExecuted;
            stageManager.Events.UndoExecuted -= OnUndoExecuted;
        }

        public void Initialize(int entityId)
        {
            _entityId = entityId;
            _initialized = true;
            _breaking = false;

            if (stageManager == null)
                stageManager = FindFirstObjectByType<StageManager>();

            // 구독 보장 (OnEnable보다 늦게 호출될 수 있으므로)
            if (stageManager != null)
            {
                stageManager.Events.TurnExecuted -= OnTurnExecuted;
                stageManager.Events.UndoExecuted -= OnUndoExecuted;
                stageManager.Events.TurnExecuted += OnTurnExecuted;
                stageManager.Events.UndoExecuted += OnUndoExecuted;
            }

            if (_spriteRenderer != null && frames != null && frames.Length > 0)
                _spriteRenderer.sprite = frames[0];
        }

        private void OnTurnExecuted(TurnOutcome outcome) { UpdateVisual(); }
        private void OnUndoExecuted() { UpdateVisual(); }

        private void UpdateVisual()
        {
            if (!_initialized || stageManager == null || _breaking) return;
            if (frames == null || frames.Length < 2 || _spriteRenderer == null) return;

            StageState state = stageManager.CurrentState;
            if (state == null) return;

            if (!state.TryGetEntity(_entityId, out EntityState entity)) return;
            if (!entity.Has<BreakableData>()) return;

            BreakableData data = entity.Get<BreakableData>();

            // IsBreaking → 파괴 애니메이션
            if (data.IsBreaking)
            {
                _breaking = true;

                stageManager.Events.TurnExecuted -= OnTurnExecuted;
                stageManager.Events.UndoExecuted -= OnUndoExecuted;

                Vector3 pos = transform.position;
                int order = _spriteRenderer != null ? _spriteRenderer.sortingOrder : 0;

                // StageManager 코루틴으로 임시 스프라이트 애니메이션
                stageManager.StartCoroutine(
                    AnimateBreakAndRemove(pos, order, _entityId));
                return;
            }

            // IsBlocked → 금 간 스프라이트
            if (data.IsBlocked)
                _spriteRenderer.sprite = frames[1];
            else
                _spriteRenderer.sprite = frames[0];
        }

        private IEnumerator AnimateBreakAndRemove(Vector3 position, int sortOrder, int entityId)
        {
            GameObject temp = new GameObject($"BreakEffect_{entityId}");
            temp.transform.position = position;
            SpriteRenderer sr = temp.AddComponent<SpriteRenderer>();
            sr.sortingOrder = sortOrder + 1;
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

            sr.sprite = frames[total - 1];
            yield return new WaitForSeconds(0.1f);
            Object.Destroy(temp);

            StageState state = stageManager != null ? stageManager.CurrentState : null;
            if (state != null && state.TryGetEntity(entityId, out _))
            {
                state.RemoveEntity(entityId);
                state.SetViewDirty();
            }
        }
    }
}