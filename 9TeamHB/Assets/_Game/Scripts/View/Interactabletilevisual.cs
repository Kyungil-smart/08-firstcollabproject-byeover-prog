using System.Collections;
using UnityEngine;

namespace MyGame2.Stage
{
    // 문/레버/버튼 프리팹에 부착하는 비주얼 컴포넌트.
    // HiddenTrapVisualManager와 같은 스프라이트 프레임 방식.

    public class InteractableTileVisual : MonoBehaviour
    {
        [Header("씬 참조")]
        [SerializeField] private StageManager stageManager;

        [Header("스프라이트 프레임")]
        [Tooltip("[0]=비활성, [마지막]=활성. 3장 이상이면 전환 시 애니메이션")]
        [SerializeField] private Sprite[] frames;

        [Header("연출 시간 (프레임 3장 이상일 때)")]
        [SerializeField] private float transitionDuration = 0.3f;

        [Header("렌더링")]
        [SerializeField] private int sortingOrder = 1;

        [Header("대상 SpriteRenderer (비우면 자식에서 자동 탐색)")]
        [SerializeField] private SpriteRenderer targetSpriteRenderer;

        private SpriteRenderer _spriteRenderer;
        private int _entityId;
        private bool _initialized;
        private bool _lastActive;
        private bool _firstUpdate = true;
        private Coroutine _transition;

        // 레버 전용: 한 번 활성화되면 스테이지 종료까지 유지
        private bool _leverActivated;

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
            _leverActivated = false;
            _firstUpdate = true;

            if (stageManager == null)
                stageManager = FindFirstObjectByType<StageManager>();

            if (stageManager != null)
            {
                stageManager.Events.TurnExecuted -= OnTurnExecuted;
                stageManager.Events.UndoExecuted -= OnUndoExecuted;
                stageManager.Events.StageLoaded -= OnStageLoaded;
                stageManager.Events.TurnExecuted += OnTurnExecuted;
                stageManager.Events.UndoExecuted += OnUndoExecuted;
                stageManager.Events.StageLoaded += OnStageLoaded;
            }

            // 초기 상태: 비활성 프레임
            if (_spriteRenderer != null && frames != null && frames.Length > 0)
            {
                _spriteRenderer.sprite = frames[0];
                _spriteRenderer.sortingOrder = sortingOrder;
            }

            UpdateVisual();
        }

        private void OnStageLoaded(int idx) { UpdateVisual(); }
        private void OnTurnExecuted(TurnOutcome outcome) { UpdateVisual(); }
        private void OnUndoExecuted() { UpdateVisual(); }

        private void UpdateVisual()
        {
            if (!_initialized || stageManager == null) return;
            if (frames == null || frames.Length == 0) return;
            if (_spriteRenderer == null) return;

            StageState state = stageManager.CurrentState;
            if (state == null) return;
            if (!state.TryGetEntity(_entityId, out EntityState entity)) return;

            bool isActive = EvaluateActive(state, entity);

            // 상태가 안 변했으면 스킵
            if (!_firstUpdate && isActive == _lastActive) return;

            bool animate = frames.Length >= 3 && !_firstUpdate;

            if (animate)
            {
                // 프레임 3장 이상: 코루틴으로 순차 애니메이션
                if (_transition != null) StopCoroutine(_transition);
                _transition = StartCoroutine(
                    PlayTransition(isActive ? true : false));
            }
            else
            {
                // 프레임 2장 이하 또는 최초: 즉시 교체
                _spriteRenderer.sprite = isActive
                    ? frames[frames.Length - 1]
                    : frames[0];
            }

            _lastActive = isActive;
            _firstUpdate = false;
        }

        // 순방향(activate=true) 또는 역방향(activate=false) 프레임 재생
        private IEnumerator PlayTransition(bool forward)
        {
            int totalFrames = frames.Length;
            float elapsed = 0f;

            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / transitionDuration);

                int frameIndex;
                if (forward)
                    frameIndex = Mathf.FloorToInt(t * (totalFrames - 1));
                else
                    frameIndex = Mathf.FloorToInt((1f - t) * (totalFrames - 1));

                frameIndex = Mathf.Clamp(frameIndex, 0, totalFrames - 1);
                _spriteRenderer.sprite = frames[frameIndex];

                yield return null;
            }

            // 최종 프레임 확정
            _spriteRenderer.sprite = forward
                ? frames[totalFrames - 1]
                : frames[0];

            _transition = null;
        }

        // Kind별 활성화 조건

        private bool EvaluateActive(StageState state, EntityState self)
        {
            switch (self.Kind)
            {
                case EntityKind.ButtonEntity:
                    // 플레이어나 상자가 위에 있으면 눌림, 없으면 안 눌림
                    return HasOccupantOnTile(state, self, true);

                case EntityKind.LeverEntity:
                    // 한 번 플레이어가 밟으면 영구 활성화
                    if (_leverActivated) return true;
                    if (HasOccupantOnTile(state, self, false))
                    {
                        _leverActivated = true;
                        return true;
                    }
                    return false;

                case EntityKind.DoorEntity:
                    // 페어된 버튼/레버의 활성 상태를 따라감
                    return IsPairedActive(state, self);

                default:
                    CellData cell = state.GetCell(self.Position);
                    return cell.HasActive;
            }
        }

        private bool HasOccupantOnTile(StageState state, EntityState self, bool includeBoxes)
        {
            foreach (EntityState e in state.Entities)
            {
                if (e.Id == self.Id) continue;
                if (!e.IsAlive) continue;
                if (e.Position.X != self.Position.X || e.Position.Y != self.Position.Y) continue;

                if (e.IsPlayer) return true;
                if (includeBoxes && e.IsBox) return true;
            }
            return false;
        }

        private bool IsPairedActive(StageState state, EntityState door)
        {
            CellData cell = state.GetCell(door.Position);
            if (cell.HasActive) return true;

            if (state.TryGetCellPair(door.Position, out GridPos pairedPos))
            {
                foreach (EntityState e in state.Entities)
                {
                    if (!e.IsAlive) continue;
                    if (e.Position.X != pairedPos.X || e.Position.Y != pairedPos.Y) continue;

                    if (e.Kind == EntityKind.LeverEntity || e.Kind == EntityKind.ButtonEntity)
                    {
                        return HasOccupantOnTile(state, e,
                            e.Kind == EntityKind.ButtonEntity);
                    }
                }
            }

            return false;
        }
    }
}