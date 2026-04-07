using UnityEngine;

namespace MyGame2.Stage
{
    public class BreakableBoxVisual : MonoBehaviour
    {
        [Header("씬 참조")]
        [SerializeField] private StageManager stageManager;

        [Header("스프라이트 프레임")]
        [Tooltip("[0]=온전, [1]=금간")]
        [SerializeField] private Sprite[] frames;

        [Header("대상 SpriteRenderer (비우면 자식에서 자동 탐색)")]
        [SerializeField] private SpriteRenderer targetSpriteRenderer;

        private SpriteRenderer _spriteRenderer;
        private int _entityId;
        private bool _initialized;

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
            if (stageManager == null) return;
            stageManager.Events.TurnExecuted -= OnTurnExecuted;
            stageManager.Events.UndoExecuted -= OnUndoExecuted;
        }

        public void Initialize(int entityId)
        {
            _entityId = entityId;
            _initialized = true;

            if (stageManager == null)
                stageManager = FindFirstObjectByType<StageManager>();

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
            if (!_initialized || stageManager == null) return;
            if (frames == null || frames.Length < 2 || _spriteRenderer == null) return;

            StageState state = stageManager.CurrentState;
            if (state == null) return;

            // Undo 중에는 스프라이트만 원래대로 복원
            if (state.IsUndoProcessing)
            {
                if (state.TryGetEntity(_entityId, out EntityState undoEntity) && undoEntity.Has<BreakableData>())
                {
                    BreakableData ud = undoEntity.Get<BreakableData>();
                    _spriteRenderer.sprite = ud.IsBlocked ? frames[1] : frames[0];
                }
                return;
            }

            if (!state.TryGetEntity(_entityId, out EntityState entity)) return;
            if (!entity.Has<BreakableData>()) return;

            BreakableData data = entity.Get<BreakableData>();

            // 파괴 애니메이션은 BreakableBoxManager가 담당 -> 여기선 안 함
            if (data.IsBreaking) return;

            // 금간 / 온전 스프라이트만 표시
            _spriteRenderer.sprite = data.IsBlocked ? frames[1] : frames[0];
        }
    }
}