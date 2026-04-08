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
        private readonly Dictionary<int, GridPos> _brokenPositions = new Dictionary<int, GridPos>();

        // 실행 중인 파괴 코루틴 추적 (Undo 시 취소용)
        private readonly Dictionary<int, Coroutine> _activeCoroutines = new Dictionary<int, Coroutine>();
        // 파괴 애니메이션 임시 오브젝트 추적 (코루틴 취소 시 정리용)
        private readonly Dictionary<int, GameObject> _activeBreakFX = new Dictionary<int, GameObject>();

        // 역재생 임시 오브젝트 추적 (다음 Undo 스텝에서 정리용)
        private readonly List<GameObject> _reverseEffects = new List<GameObject>();
        private readonly List<Coroutine> _reverseCoroutines = new List<Coroutine>();
        private readonly List<GridEntityView> _hiddenViews = new List<GridEntityView>();

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
            foreach (var kvp in _activeCoroutines)
            {
                if (kvp.Value != null) StopCoroutine(kvp.Value);
            }
            _activeCoroutines.Clear();
            foreach (var kvp in _activeBreakFX)
            {
                if (kvp.Value != null) Destroy(kvp.Value);
            }
            _activeBreakFX.Clear();
            CleanupReverseEffects();
            _trackedIds.Clear();
            _breakingIds.Clear();
            _brokenPositions.Clear();
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
            if (stageManager.CurrentState == null) return;

            if (stageManager.CurrentState.IsUndoProcessing)
            {
                CheckRestore();
                return;
            }

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
                    // 수정: View의 실제 자식 위치를 사용 (Fallen 상태면 내려간 위치)
                    Vector3 pos = GetActualVisualPosition(id, box.Position.ToWorld(1f));
                    int sortingOrder = GetActualSortingOrder(id);

                    _brokenPositions[id] = box.Position;
                    Coroutine co = StartCoroutine(AnimateBreak(pos, id, sortingOrder));
                    _activeCoroutines[id] = co;
                }
            }
        }

        // 추가: View의 실제 자식 위치를 가져옴 (Fallen이면 아래로 내려간 상태)
        private Vector3 GetActualVisualPosition(int entityId, Vector3 fallbackPos)
        {
            GridEntityView view = FindViewForEntity(entityId);
            if (view == null) return fallbackPos;

            if (view.transform.childCount > 0)
            {
                Transform child = view.transform.GetChild(0);
                return child.position;
            }

            return view.transform.position;
        }

        // 추가: Fallen 상태의 sortingOrder를 가져옴
        private int GetActualSortingOrder(int entityId)
        {
            GridEntityView view = FindViewForEntity(entityId);
            if (view == null) return 1;

            if (view.transform.childCount > 0)
            {
                SpriteRenderer sr = view.transform.GetChild(0).GetComponent<SpriteRenderer>();
                if (sr != null) return sr.sortingOrder;
            }

            return 1;
        }

        private IEnumerator AnimateBreak(Vector3 position, int entityId, int sortingOrder = 1)
        {
            if (frames == null || frames.Length < 2)
            {
                Debug.LogError("[BBM] frames 비어있음!");
                yield break;
            }

            // View 즉시 숨기기 (Fallen 상태의 원본 스프라이트가 보이지 않도록)
            GridEntityView view = FindViewForEntity(entityId);
            if (view != null)
                view.gameObject.SetActive(false);

            GameObject temp = new GameObject($"BreakFX_{entityId}");
            temp.transform.position = position;
            SpriteRenderer sr = temp.AddComponent<SpriteRenderer>();
            sr.sortingOrder = sortingOrder;
            sr.sprite = frames[0];

            // 임시 오브젝트 등록 (Undo 취소 시 정리용)
            _activeBreakFX[entityId] = temp;

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

            _activeBreakFX.Remove(entityId);
            Destroy(temp);

            StageState state = stageManager.CurrentState;
            if (state != null && state.TryGetEntity(entityId, out _))
            {
                state.RemoveEntity(entityId);
                state.SetViewDirty();
            }

            _trackedIds.Remove(entityId);
            _breakingIds.Remove(entityId);
            _activeCoroutines.Remove(entityId);
        }

        // Undo 매 스텝: 파괴됐던 상자가 복원됐으면 역재생
        private void CheckRestore()
{
    StageState state = stageManager.CurrentState;
    if (state == null) return;

    CleanupReverseEffects();

    // 현재 파괴 진행 중인 것도 Undo면 전부 취소
    List<int> cancelBreaking = null;
    foreach (int id in _breakingIds)
    {
        if (cancelBreaking == null) cancelBreaking = new List<int>(2);
        cancelBreaking.Add(id);
    }
    if (cancelBreaking != null)
    {
        for (int i = 0; i < cancelBreaking.Count; i++)
        {
            int id = cancelBreaking[i];
            if (_activeCoroutines.TryGetValue(id, out Coroutine co) && co != null)
            {
                StopCoroutine(co);
                _activeCoroutines.Remove(id);
            }
            if (_activeBreakFX.TryGetValue(id, out GameObject fx) && fx != null)
            {
                Destroy(fx);
                _activeBreakFX.Remove(id);
            }
            _breakingIds.Remove(id);

            GridEntityView view = FindViewForEntity(id);
            if (view != null) view.gameObject.SetActive(true);
        }
    }

    // 파괴 완료된 상자가 Undo로 되살아난 경우
    List<int> restored = null;
    foreach (var kvp in _brokenPositions)
    {
        int id = kvp.Key;
        if (state.TryGetEntity(id, out EntityState entity) && entity.IsAlive)
        {
            if (restored == null) restored = new List<int>(2);
            restored.Add(id);
            _trackedIds.Add(id);

            GridEntityView view = FindViewForEntity(id);
            if (view != null) view.gameObject.SetActive(true);
        }
    }
    if (restored != null)
    {
        for (int i = 0; i < restored.Count; i++)
            _brokenPositions.Remove(restored[i]);
    }
    
    // 스냅샷 복원 시 IsBreaking=true가 남아서 자동 파괴되는 버그 방지
    foreach (int id in _trackedIds)
    {
        if (state.TryGetEntity(id, out EntityState ent) && ent.Has<BreakableData>())
        {
            BreakableData bd = ent.Get<BreakableData>();
            if (bd.IsBreaking || bd.IsStepped)
            {
                bd.IsBreaking = false;
                bd.IsStepped = false;
                ent.Set(bd);
            }
        }
    }
}

        // 프레임 역순 재생 -> 끝나면 View 보이기
        private IEnumerator AnimateReverse(Vector3 position, GridEntityView view)
        {
            if (frames == null || frames.Length < 2) yield break;

            // View 숨기기 (역재생 끝날 때까지)
            if (view != null)
            {
                view.gameObject.SetActive(false);
                _hiddenViews.Add(view);
            }

            GameObject temp = new GameObject("BreakReverseFX");
            temp.transform.position = position;
            SpriteRenderer sr = temp.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 1;
            sr.sprite = frames[frames.Length - 1];

            // 추적 등록
            _reverseEffects.Add(temp);

            int total = frames.Length;
            float duration = 0.12f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                int idx = Mathf.Clamp(Mathf.FloorToInt((1f - t) * (total - 1)), 0, total - 1);
                sr.sprite = frames[idx];
                yield return null;
            }

            // 정상 완료 시 정리
            _reverseEffects.Remove(temp);
            Destroy(temp);

            // 역재생 끝 -> View 보이기
            if (view != null)
            {
                view.gameObject.SetActive(true);
                _hiddenViews.Remove(view);
            }
        }

        // 이전 역재생 잔존물 전부 정리
        private void CleanupReverseEffects()
        {
            for (int i = 0; i < _reverseCoroutines.Count; i++)
            {
                if (_reverseCoroutines[i] != null)
                    StopCoroutine(_reverseCoroutines[i]);
            }
            _reverseCoroutines.Clear();

            for (int i = 0; i < _reverseEffects.Count; i++)
            {
                if (_reverseEffects[i] != null)
                    Destroy(_reverseEffects[i]);
            }
            _reverseEffects.Clear();

            // 역재생 중 숨겨진 View 복원
            for (int i = 0; i < _hiddenViews.Count; i++)
            {
                if (_hiddenViews[i] != null)
                    _hiddenViews[i].gameObject.SetActive(true);
            }
            _hiddenViews.Clear();
        }

        // EntityId로 GridEntityView 찾기
        private GridEntityView FindViewForEntity(int entityId)
        {
            GridEntityView[] allViews = FindObjectsByType<GridEntityView>(FindObjectsSortMode.None);
            for (int i = 0; i < allViews.Length; i++)
            {
                if (allViews[i].EntityId == entityId)
                    return allViews[i];
            }
            return null;
        }
    }
}