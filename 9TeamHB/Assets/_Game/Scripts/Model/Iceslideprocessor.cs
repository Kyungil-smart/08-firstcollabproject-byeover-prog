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

        [Tooltip("셀 크기 (StageManager의 cellSize와 동일하게)")]
        [SerializeField] private float cellSize = 1f;

        private float _timer;
        private bool _isProcessing;
        private bool _wasSliding;
        // PushRule Lerp 완료 대기 중
        private bool _waitingForLerp;

        private void Update()
        {
            if (stageManager == null || stageManager.CurrentState == null) return;
            if (!stageManager.CurrentState.IsUpdatable()) return;

            StageState state = stageManager.CurrentState;

            // Undo 중이면 슬라이드 즉시 중단
            if (state.IsUndoProcessing)
            {
                StopAllSliding(state);
                _timer = 0f;
                _isProcessing = false;
                _wasSliding = false;
                _waitingForLerp = false;
                return;
            }

            // 미끄러지는 얼음 상자가 있는지 체크
            _isProcessing = false;
            int slidingBoxId = -1;
            for (int i = 0; i < state.BoxIds.Count; i++)
            {
                int boxId = state.BoxIds[i];
                if (!state.TryGetEntity(boxId, out EntityState box)) continue;
                if (!box.IsAlive || !box.Has<IceSlideData>()) continue;

                IceSlideData ice = box.Get<IceSlideData>();
                if (!ice.IsSliding) continue;

                _isProcessing = true;
                slidingBoxId = boxId;
                break;
            }

            if (!_isProcessing)
            {
                _timer = 0f;
                _waitingForLerp = false;

                if (_wasSliding)
                {
                    _wasSliding = false;
                    stageManager.Events?.RaiseTurnExecuted(TurnOutcome.None());
                }
                return;
            }

            // 슬라이딩 시작 직후: PushRule이 1칸 밀었으므로 View Lerp 완료 대기
            if (!_wasSliding)
            {
                _wasSliding = true;
                _waitingForLerp = true;
                _timer = 0f;
            }

            // View의 Lerp가 아직 진행 중이면 대기
            if (_waitingForLerp)
            {
                GridEntityView view = FindSlidingView(slidingBoxId);
                if (view != null && view.IsSliding)
                    return; // Lerp 진행 중 -> 대기
                _waitingForLerp = false;
                _timer = 0f; // Lerp 끝난 직후부터 타이머 시작
            }

            _timer += Time.deltaTime;
            if (_timer < slideInterval) return;
            _timer -= slideInterval;

            // 미끄러지는 모든 얼음 상자를 1칸씩 이동
            bool viewDirty = false;
            for (int i = state.BoxIds.Count - 1; i >= 0; i--)
            {
                if (i >= state.BoxIds.Count) continue;
                int boxId = state.BoxIds[i];
                if (!state.TryGetEntity(boxId, out EntityState box)) continue;
                if (!box.IsAlive || !box.Has<IceSlideData>()) continue;

                IceSlideData ice = box.Get<IceSlideData>();
                if (!ice.IsSliding) continue;

                viewDirty |= SlideOneStep(state, boxId, box, ice);
            }

            if (viewDirty)
            {
                // 중간 스텝: View를 직접 Sync (TurnExecuted 없이 스냅샷 방지)
                SyncSlidingViews(state);
            }
        }

        // 미끄러지는 얼음 상자의 View만 직접 동기화 (이벤트 없이)
        private void SyncSlidingViews(StageState state)
        {
            GridEntityView[] views = FindObjectsByType<GridEntityView>(FindObjectsSortMode.None);
            for (int i = 0; i < views.Length; i++)
            {
                GridEntityView view = views[i];
                if (!state.TryGetEntity(view.EntityId, out EntityState entity)) continue;
                if (!entity.Has<IceSlideData>()) continue;

                IceSlideData ice = entity.Get<IceSlideData>();
                if (ice.IsSliding || _wasSliding)
                    view.Sync(entity, cellSize);
            }
        }

        private GridEntityView FindSlidingView(int entityId)
        {
            GridEntityView[] views = FindObjectsByType<GridEntityView>(FindObjectsSortMode.None);
            for (int i = 0; i < views.Length; i++)
            {
                if (views[i].EntityId == entityId)
                    return views[i];
            }
            return null;
        }

        // Undo 시 모든 얼음 상자 슬라이딩 즉시 정지
        private void StopAllSliding(StageState state)
        {
            for (int i = 0; i < state.BoxIds.Count; i++)
            {
                int boxId = state.BoxIds[i];
                if (!state.TryGetEntity(boxId, out EntityState box)) continue;
                if (!box.Has<IceSlideData>()) continue;

                IceSlideData ice = box.Get<IceSlideData>();
                if (ice.IsSliding)
                    StopSliding(box);
            }
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
            if (cell.HasWall || cell.IsClosedDoor)
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

            // 틈새 타일 -> 상자가 끼면서 정지
            if (state.HasCrackNotCovered(next))
            {
                state.SetCrackMovable(next, boxId);
                StopSliding(box);
                return true;
            }

            // 톱날 함정 범위 진입 -> 반 쪼개짐 파괴
            if (state.IsInSawTrapRange(next))
            {
                Direction sawFacing = state.GetSawTrapFacingAt(next);
                // 이벤트 발행 -> IceBoxSplitEffect가 수신하여 쪼개짐 연출
                state.Events?.RaiseIceBoxSawDestroyed(boxId, next, sawFacing);
                state.RemoveEntity(boxId);
                state.SetViewDirty();
                return true;
            }

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