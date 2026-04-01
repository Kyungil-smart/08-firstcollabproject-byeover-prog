using UnityEngine;

namespace MyGame2.Stage
{
    // 얼음 상자의 미끄러짐을 한 칸씩 처리하는 프로세서.
    // RobotAutoMover와 동일한 패턴으로 타이머 기반 이동.
    // GridEntityView의 Lerp 애니메이션과 연동되어 부드럽게 보임.
    public sealed class IceSlideProcessor : MonoBehaviour
    {
        [Header("씬 참조")]
        [SerializeField] private StageManager stageManager;

        [Header("미끄러짐 설정")]
        [Tooltip("한 칸 이동 간격 (초) — GridEntityView slideSpeed와 맞춰서 조정")]
        [SerializeField] private float slideInterval = 0.08f;

        private float _timer;
        private bool _isProcessing;

        private void Update()
        {
            if (stageManager == null || stageManager.CurrentState == null) return;
            if (stageManager.CurrentState.IsUpdatable()) return;

            StageState state = stageManager.CurrentState;

            // 미끄러지는 얼음 상자가 있는지 체크
            _isProcessing = false;
            for (int i = 0; i < state.BoxIds.Count; i++)
            {
                int boxId = state.BoxIds[i];
                if (!state.TryGetEntity(boxId, out EntityState box)) continue;
                if (!box.IsAlive || !box.Has<IceSlideData>()) continue;

                IceSlideData ice = box.Get<IceSlideData>();
                if (!ice.IsSliding) continue;

                _isProcessing = true;
                break;
            }

            if (!_isProcessing)
            {
                _timer = 0f;
                return;
            }

            _timer += Time.deltaTime;
            if (_timer < slideInterval) return;
            _timer -= slideInterval;

            // 미끄러지는 모든 얼음 상자를 1칸씩 이동
            bool viewDirty = false;
            for (int i = state.BoxIds.Count - 1; i >= 0; i--)
            {
                if (i >= state.BoxIds.Count) continue; // 중간에 제거될 수 있음
                int boxId = state.BoxIds[i];
                if (!state.TryGetEntity(boxId, out EntityState box)) continue;
                if (!box.IsAlive || !box.Has<IceSlideData>()) continue;

                IceSlideData ice = box.Get<IceSlideData>();
                if (!ice.IsSliding) continue;

                viewDirty |= SlideOneStep(state, boxId, box, ice);
            }

            if (viewDirty)
                stageManager.Events?.RaiseTurnExecuted(TurnOutcome.None());
        }

        // 얼음 상자를 1칸 이동. 막히면 정지.
        private bool SlideOneStep(StageState state, int boxId, EntityState box, IceSlideData ice)
        {
            GridPos next = box.Position.Move(ice.SlideDirection);

            // 맵 밖 -> 정지
            if (!state.IsInside(next))
            {
                StopSliding(box);
                return false;
            }

            CellData cell = state.GetCell(next);

            // 벽 -> 정지
            if (cell.HasWall)
            {
                StopSliding(box);
                return false;
            }

            // 다른 엔티티(상자/감시자/캐릭터) -> 정지
            if (cell.IsOccupied)
            {
                StopSliding(box);
                return false;
            }

            // 이동
            state.TryMoveEntity(boxId, next);

            // 파괴 함정 -> 상자 파괴 (함정은 유지)
            if (state.HasDestroyTrap(next))
            {
                state.RemoveEntity(boxId);
                state.SetViewDirty();
                return true;
            }

            // 일반 함정 -> 함정 비활성화 + 정지
            if (state.HasTrap(next))
            {
                state.DisableTrap(next);
                StopSliding(box);
                return true;
            }

            return true;
        }

        private void StopSliding(EntityState box)
        {
            IceSlideData ice = box.Get<IceSlideData>();
            ice.IsSliding = false;
            ice.SlideDirection = Direction.None;
            box.Set(ice);
        }
    }
}