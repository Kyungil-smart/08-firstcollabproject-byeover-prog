using System.Collections;
using UnityEngine;

namespace MyGame2.Stage
{
    // 부서지는 상자 프리팹에 부착.
    // 스프라이트 프레임 방식으로 파괴 애니메이션 재생.

    public class BreakableBoxVisual : MonoBehaviour
    {
        [Header("스프라이트 프레임")]
        [Tooltip("[0]=온전, [1]=금간, [2~]=부서지는 프레임")]
        [SerializeField] private Sprite[] frames;

        [Header("연출 시간")]
        [SerializeField] private float breakDuration = 0.4f;

        [Header("대상 SpriteRenderer (비우면 자식에서 자동 탐색)")]
        [SerializeField] private SpriteRenderer targetSpriteRenderer;

        private StageManager _stageManager;
        private SpriteRenderer _spriteRenderer;
        private int _entityId;
        private bool _initialized;
        private bool _wasBlocked;
        private bool _isBreaking; // 파괴 애니메이션 재생 중

        private void Awake()
        {
            _spriteRenderer = targetSpriteRenderer != null
                ? targetSpriteRenderer
                : GetComponentInChildren<SpriteRenderer>();
        }

        public void Initialize(int entityId)
        {
            _entityId = entityId;
            _initialized = true;
            _wasBlocked = false;
            _isBreaking = false;

            _stageManager = FindFirstObjectByType<StageManager>();

            if (_stageManager != null)
            {
                _stageManager.Events.TurnExecuted -= OnTurnExecuted;
                _stageManager.Events.UndoExecuted -= OnUndoExecuted;
                _stageManager.Events.TurnExecuted += OnTurnExecuted;
                _stageManager.Events.UndoExecuted += OnUndoExecuted;
            }

            if (_spriteRenderer != null && frames != null && frames.Length > 0)
                _spriteRenderer.sprite = frames[0];
        }

        private void OnDisable()
        {
            // 파괴 애니메이션 중이면 구독 해제하지 않음
            if (_isBreaking) return;

            if (_stageManager == null) return;
            _stageManager.Events.TurnExecuted -= OnTurnExecuted;
            _stageManager.Events.UndoExecuted -= OnUndoExecuted;
        }

        private void OnTurnExecuted(TurnOutcome outcome) { UpdateVisual(); }
        private void OnUndoExecuted() { UpdateVisual(); }

        private void UpdateVisual()
        {
            if (!_initialized || _stageManager == null || _isBreaking) return;
            if (frames == null || frames.Length < 2 || _spriteRenderer == null) return;

            StageState state = _stageManager.CurrentState;
            if (state == null) return;

            // 엔티티가 없거나 죽었으면 -> 파괴 애니메이션
            if (!state.TryGetEntity(_entityId, out EntityState entity) || !entity.IsAlive)
            {
                _isBreaking = true;
                // Sync가 gameObject를 숨겼을 수 있으니 다시 활성화
                gameObject.SetActive(true);
                StartCoroutine(PlayBreakAnimation());
                return;
            }

            if (!entity.Has<BreakableData>()) return;

            BreakableData data = entity.Get<BreakableData>();

            if (data.IsBlocked && !_wasBlocked)
            {
                // 막힌 상태 -> 금 간 스프라이트
                if (frames.Length >= 2)
                    _spriteRenderer.sprite = frames[1];
            }
            else if (!data.IsBlocked && _wasBlocked)
            {
                // 막힘 해제 -> 온전한 스프라이트
                _spriteRenderer.sprite = frames[0];
            }

            _wasBlocked = data.IsBlocked;
        }

        private IEnumerator PlayBreakAnimation()
        {
            // 이벤트 구독 해제 (애니메이션 중 추가 호출 방지)
            if (_stageManager != null)
            {
                _stageManager.Events.TurnExecuted -= OnTurnExecuted;
                _stageManager.Events.UndoExecuted -= OnUndoExecuted;
            }

            if (frames.Length < 3)
            {
                _spriteRenderer.sprite = frames[frames.Length - 1];
                yield return new WaitForSeconds(breakDuration);
                gameObject.SetActive(false);
                yield break;
            }

            // 금 간 프레임(1)부터 마지막까지 순차 재생
            int startFrame = 1;
            int totalFrames = frames.Length - startFrame;
            float elapsed = 0f;

            while (elapsed < breakDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / breakDuration);
                int frameIndex = startFrame + Mathf.Clamp(
                    Mathf.FloorToInt(t * (totalFrames - 1)), 0, totalFrames - 1);
                _spriteRenderer.sprite = frames[frameIndex];
                yield return null;
            }

            _spriteRenderer.sprite = frames[frames.Length - 1];
            yield return new WaitForSeconds(0.1f);
            gameObject.SetActive(false);
        }
    }
}