using UnityEngine;

namespace MyGame2.Stage
{
    // 문/레버/버튼 프리팹에 부착하는 비주얼 컴포넌트.

    public class InteractableTileVisual : MonoBehaviour
    {
        [Header("씬 참조")]
        [SerializeField] private StageManager stageManager;

        [Header("Animator 모드 (문 등 애니메이션이 필요한 경우)")]
        [SerializeField] private Animator targetAnimator;

        [Header("스프라이트 교체 모드 (버튼 등 이미지 2장인 경우)")]
        [Tooltip("비활성 상태 스프라이트")]
        [SerializeField] private Sprite inactiveSprite;
        [Tooltip("활성 상태 스프라이트")]
        [SerializeField] private Sprite activeSprite;
        [Tooltip("스프라이트를 교체할 SpriteRenderer (비우면 자식에서 자동 탐색)")]
        [SerializeField] private SpriteRenderer targetSpriteRenderer;

        private static readonly int AnimIsActive = Animator.StringToHash("IsActive");

        private Animator _animator;
        private SpriteRenderer _spriteRenderer;
        private int _entityId;
        private bool _lastActive;
        private bool _initialized;
        private bool _useAnimator;
        private bool _useSpriteSwap;

        private void Awake()
        {
            _animator = targetAnimator != null
                ? targetAnimator
                : GetComponentInChildren<Animator>();

            _spriteRenderer = targetSpriteRenderer != null
                ? targetSpriteRenderer
                : GetComponentInChildren<SpriteRenderer>();

            // 모드 결정
            _useAnimator = _animator != null && targetAnimator != null;
            _useSpriteSwap = inactiveSprite != null && activeSprite != null;
        }

        private void OnEnable()
        {
            if (stageManager == null) return;
            stageManager.Events.TurnExecuted += OnTurnExecuted;
            stageManager.Events.UndoExecuted += OnUndoExecuted;
            stageManager.Events.StageLoaded += OnStageLoaded;
        }

        private void OnDisable()
        {
            if (stageManager == null) return;
            stageManager.Events.TurnExecuted -= OnTurnExecuted;
            stageManager.Events.UndoExecuted -= OnUndoExecuted;
            stageManager.Events.StageLoaded -= OnStageLoaded;
        }

        // StageManager.SpawnViewForEntity()에서 호출
        public void Initialize(int entityId)
        {
            _entityId = entityId;
            _initialized = true;
            UpdateVisual();
        }

        private void OnStageLoaded(int idx) { UpdateVisual(); }
        private void OnTurnExecuted(TurnOutcome outcome) { UpdateVisual(); }
        private void OnUndoExecuted() { UpdateVisual(); }

        private void UpdateVisual()
        {
            if (!_initialized || stageManager == null) return;
            StageState state = stageManager.CurrentState;
            if (state == null) return;
            if (!state.TryGetEntity(_entityId, out EntityState entity)) return;

            CellData cell = state.GetCell(entity.Position);
            bool isActive = cell.HasActive;

            // Animator 모드
            if (_useAnimator && _animator != null)
            {
                _animator.SetBool(AnimIsActive, isActive);
            }

            // 스프라이트 교체 모드
            if (_useSpriteSwap && _spriteRenderer != null)
            {
                _spriteRenderer.sprite = isActive ? activeSprite : inactiveSprite;
            }

            _lastActive = isActive;
        }
    }
}