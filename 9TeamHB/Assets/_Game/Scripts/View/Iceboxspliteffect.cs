using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyGame2.Stage
{
    public class IceBoxSplitEffect : MonoBehaviour
    {
        [Header("씬 참조")]
        [SerializeField] private StageManager stageManager;

        [Header("스프라이트")]
        [Tooltip("얼음 상자 스프라이트 (Read/Write Enabled 필요)")]
        [SerializeField] private Sprite iceBoxSprite;

        [Header("연출 설정")]
        [SerializeField] private float splitDistance = 0.8f;
        [SerializeField] private float splitDuration = 0.5f;
        [SerializeField] private float rotationAngle = 25f;
        [SerializeField] private float dropDistance = 0.3f;

        // Undo 역재생용: 파괴된 얼음 상자 정보
        private struct DestroyedIceInfo
        {
            public GridPos Position;
            public Direction SawFacing;
        }
        private readonly Dictionary<int, DestroyedIceInfo> _destroyedIce = new Dictionary<int, DestroyedIceInfo>();

        // 정방향 쪼개짐 코루틴/오브젝트 추적 (Undo 시 취소+정리용)
        private readonly Dictionary<int, Coroutine> _activeSplitCoroutines = new Dictionary<int, Coroutine>();
        private readonly Dictionary<int, List<GameObject>> _activeSplitFX = new Dictionary<int, List<GameObject>>();

        // 역재생 잔존물 추적 (다음 Undo 스텝에서 정리용)
        private readonly List<GameObject> _reverseEffects = new List<GameObject>();
        private readonly List<Coroutine> _reverseCoroutines = new List<Coroutine>();
        private readonly List<GridEntityView> _hiddenViews = new List<GridEntityView>();

        private void OnEnable()
        {
            if (stageManager == null) return;
            stageManager.Events.IceBoxSawDestroyed += OnIceBoxSawDestroyed;
            stageManager.Events.StageLoaded += OnStageLoaded;
            stageManager.Events.TurnExecuted += OnTurnExecuted;
        }

        private void OnDisable()
        {
            if (stageManager == null) return;
            stageManager.Events.IceBoxSawDestroyed -= OnIceBoxSawDestroyed;
            stageManager.Events.StageLoaded -= OnStageLoaded;
            stageManager.Events.TurnExecuted -= OnTurnExecuted;
        }

        private void OnStageLoaded(int idx)
        {
            CleanupAllSplitFX();
            CleanupReverseEffects();
            _destroyedIce.Clear();
        }

        private void OnIceBoxSawDestroyed(int entityId, GridPos position, Direction sawFacing)
        {
            if (iceBoxSprite == null) return;

            _destroyedIce[entityId] = new DestroyedIceInfo
            {
                Position = position,
                SawFacing = sawFacing
            };

            Coroutine co = StartCoroutine(PlaySplit(position, sawFacing, entityId));
            _activeSplitCoroutines[entityId] = co;
        }

        private void OnTurnExecuted(TurnOutcome outcome)
        {
            if (stageManager.CurrentState == null) return;
            if (!stageManager.CurrentState.IsUndoProcessing) return;

            // 이전 역재생 잔존물 정리
            CleanupReverseEffects();

            StageState state = stageManager.CurrentState;
            List<int> restored = null;

            foreach (var kvp in _destroyedIce)
            {
                int id = kvp.Key;
                if (state.TryGetEntity(id, out EntityState entity) && entity.IsAlive)
                {
                    if (restored == null) restored = new List<int>(2);
                    restored.Add(id);

                    // 실행 중인 정방향 코루틴 취소 + 임시 오브젝트 파괴
                    StopSplitForEntity(id);

                    // View 숨기고 역재생 후 보이기
                    GridEntityView view = FindViewForEntity(id);
                    Vector3 worldPos = entity.Position.ToWorld(1f);
                    bool horizontalCut = (kvp.Value.SawFacing == Direction.Left || kvp.Value.SawFacing == Direction.Right);
                    Coroutine revCo = StartCoroutine(PlayReverseSplit(worldPos, horizontalCut, view));
                    _reverseCoroutines.Add(revCo);
                }
            }

            if (restored != null)
            {
                for (int i = 0; i < restored.Count; i++)
                    _destroyedIce.Remove(restored[i]);
            }
        }

        // 정방향: 쪼개지는 연출

        private IEnumerator PlaySplit(GridPos position, Direction sawFacing, int entityId)
        {
            Texture2D tex = iceBoxSprite.texture;
            Rect rect = iceBoxSprite.textureRect;
            float ppu = iceBoxSprite.pixelsPerUnit;
            Vector3 worldPos = position.ToWorld(1f);

            bool horizontalCut = (sawFacing == Direction.Left || sawFacing == Direction.Right);

            if (!tex.isReadable)
            {
                yield return PlayFallbackSplit(worldPos, horizontalCut, entityId);
                _activeSplitCoroutines.Remove(entityId);
                yield break;
            }

            int fullWidth = (int)rect.width;
            int fullHeight = (int)rect.height;

            Sprite halfA, halfB;
            Texture2D texA, texB;

            if (horizontalCut)
            {
                int halfH = fullHeight / 2;
                texA = new Texture2D(fullWidth, halfH, TextureFormat.RGBA32, false);
                texA.SetPixels(tex.GetPixels((int)rect.x, (int)rect.y, fullWidth, halfH));
                texA.Apply(); texA.filterMode = FilterMode.Point;
                halfA = Sprite.Create(texA, new Rect(0, 0, fullWidth, halfH), new Vector2(0.5f, 1f), ppu);

                texB = new Texture2D(fullWidth, halfH, TextureFormat.RGBA32, false);
                texB.SetPixels(tex.GetPixels((int)rect.x, (int)rect.y + halfH, fullWidth, halfH));
                texB.Apply(); texB.filterMode = FilterMode.Point;
                halfB = Sprite.Create(texB, new Rect(0, 0, fullWidth, halfH), new Vector2(0.5f, 0f), ppu);
            }
            else
            {
                int halfW = fullWidth / 2;
                texA = new Texture2D(halfW, fullHeight, TextureFormat.RGBA32, false);
                texA.SetPixels(tex.GetPixels((int)rect.x, (int)rect.y, halfW, fullHeight));
                texA.Apply(); texA.filterMode = FilterMode.Point;
                halfA = Sprite.Create(texA, new Rect(0, 0, halfW, fullHeight), new Vector2(1f, 0.5f), ppu);

                texB = new Texture2D(halfW, fullHeight, TextureFormat.RGBA32, false);
                texB.SetPixels(tex.GetPixels((int)rect.x + halfW, (int)rect.y, halfW, fullHeight));
                texB.Apply(); texB.filterMode = FilterMode.Point;
                halfB = Sprite.Create(texB, new Rect(0, 0, halfW, fullHeight), new Vector2(0f, 0.5f), ppu);
            }

            GameObject objA = CreateHalf("IceSplit_A", worldPos, halfA, Color.white, 10);
            GameObject objB = CreateHalf("IceSplit_B", worldPos, halfB, Color.white, 10);
            RegisterSplitFX(entityId, objA, objB);

            yield return AnimateSplit(objA, objB, horizontalCut);

            UnregisterSplitFX(entityId);
            Destroy(objA); Destroy(objB);
            Destroy(texA); Destroy(texB);
            _activeSplitCoroutines.Remove(entityId);
        }

        private IEnumerator PlayFallbackSplit(Vector3 worldPos, bool horizontalCut, int entityId)
        {
            GameObject objA = CreateHalf("IceSplit_A", worldPos, iceBoxSprite, Color.white, 10);
            GameObject objB = CreateHalf("IceSplit_B", worldPos, iceBoxSprite, Color.white, 10);
            RegisterSplitFX(entityId, objA, objB);

            if (horizontalCut)
            {
                objA.transform.localScale = new Vector3(1f, 0.5f, 1f);
                objB.transform.localScale = new Vector3(1f, 0.5f, 1f);
                objA.transform.position = worldPos + new Vector3(0, 0.25f, 0);
                objB.transform.position = worldPos + new Vector3(0, -0.25f, 0);
            }
            else
            {
                objA.transform.localScale = new Vector3(0.5f, 1f, 1f);
                objB.transform.localScale = new Vector3(0.5f, 1f, 1f);
                objA.transform.position = worldPos + new Vector3(-0.25f, 0, 0);
                objB.transform.position = worldPos + new Vector3(0.25f, 0, 0);
            }

            yield return AnimateSplit(objA, objB, horizontalCut);

            UnregisterSplitFX(entityId);
            Destroy(objA); Destroy(objB);
        }

        private IEnumerator AnimateSplit(GameObject objA, GameObject objB, bool horizontalCut)
        {
            SpriteRenderer srA = objA.GetComponent<SpriteRenderer>();
            SpriteRenderer srB = objB.GetComponent<SpriteRenderer>();
            Vector3 startA = objA.transform.position;
            Vector3 startB = objB.transform.position;
            float elapsed = 0f;

            while (elapsed < splitDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / splitDuration);
                float eased = t * (2f - t);

                if (horizontalCut)
                {
                    objA.transform.position = startA + new Vector3(dropDistance * eased, splitDistance * eased, 0);
                    objB.transform.position = startB + new Vector3(-dropDistance * eased, -splitDistance * eased, 0);
                    objA.transform.rotation = Quaternion.Euler(0, 0, -rotationAngle * eased);
                    objB.transform.rotation = Quaternion.Euler(0, 0, rotationAngle * eased);
                }
                else
                {
                    objA.transform.position = startA + new Vector3(-splitDistance * eased, -dropDistance * eased, 0);
                    objB.transform.position = startB + new Vector3(splitDistance * eased, -dropDistance * eased, 0);
                    objA.transform.rotation = Quaternion.Euler(0, 0, rotationAngle * eased);
                    objB.transform.rotation = Quaternion.Euler(0, 0, -rotationAngle * eased);
                }

                float alpha = 1f - eased;
                Color ca = srA.color; ca.a = alpha; srA.color = ca;
                Color cb = srB.color; cb.a = alpha; srB.color = cb;
                yield return null;
            }
        }

        // 역방향: 조각이 합쳐지는 연출 (0.12초)

        private IEnumerator PlayReverseSplit(Vector3 worldPos, bool horizontalCut, GridEntityView view)
        {
            // View 숨기기
            if (view != null)
            {
                view.gameObject.SetActive(false);
                _hiddenViews.Add(view);
            }

            Vector3 endA, endB;
            float endRotA, endRotB;

            if (horizontalCut)
            {
                endA = worldPos + new Vector3(dropDistance, splitDistance, 0);
                endB = worldPos + new Vector3(-dropDistance, -splitDistance, 0);
                endRotA = -rotationAngle; endRotB = rotationAngle;
            }
            else
            {
                endA = worldPos + new Vector3(-splitDistance, -dropDistance, 0);
                endB = worldPos + new Vector3(splitDistance, -dropDistance, 0);
                endRotA = rotationAngle; endRotB = -rotationAngle;
            }

            GameObject objA = CreateHalf("IceReverse_A", endA, iceBoxSprite, new Color(1, 1, 1, 0), 10);
            GameObject objB = CreateHalf("IceReverse_B", endB, iceBoxSprite, new Color(1, 1, 1, 0), 10);
            _reverseEffects.Add(objA);
            _reverseEffects.Add(objB);

            if (horizontalCut)
            {
                objA.transform.localScale = new Vector3(1f, 0.5f, 1f);
                objB.transform.localScale = new Vector3(1f, 0.5f, 1f);
            }
            else
            {
                objA.transform.localScale = new Vector3(0.5f, 1f, 1f);
                objB.transform.localScale = new Vector3(0.5f, 1f, 1f);
            }

            objA.transform.rotation = Quaternion.Euler(0, 0, endRotA);
            objB.transform.rotation = Quaternion.Euler(0, 0, endRotB);

            SpriteRenderer srA = objA.GetComponent<SpriteRenderer>();
            SpriteRenderer srB = objB.GetComponent<SpriteRenderer>();

            float duration = 0.12f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = t * (2f - t);

                objA.transform.position = Vector3.Lerp(endA, worldPos, eased);
                objB.transform.position = Vector3.Lerp(endB, worldPos, eased);
                objA.transform.rotation = Quaternion.Euler(0, 0, Mathf.Lerp(endRotA, 0, eased));
                objB.transform.rotation = Quaternion.Euler(0, 0, Mathf.Lerp(endRotB, 0, eased));

                float alpha = eased;
                Color ca = srA.color; ca.a = alpha; srA.color = ca;
                Color cb = srB.color; cb.a = alpha; srB.color = cb;
                yield return null;
            }

            // 정상 완료 시 정리
            _reverseEffects.Remove(objA);
            _reverseEffects.Remove(objB);
            Destroy(objA); Destroy(objB);

            if (view != null)
            {
                view.gameObject.SetActive(true);
                _hiddenViews.Remove(view);
            }
        }

        // 정리 유틸리티

        private void RegisterSplitFX(int entityId, GameObject a, GameObject b)
        {
            _activeSplitFX[entityId] = new List<GameObject> { a, b };
        }

        private void UnregisterSplitFX(int entityId)
        {
            _activeSplitFX.Remove(entityId);
        }

        // 특정 엔티티의 정방향 코루틴 + FX 정리
        private void StopSplitForEntity(int entityId)
        {
            if (_activeSplitCoroutines.TryGetValue(entityId, out Coroutine co) && co != null)
            {
                StopCoroutine(co);
                _activeSplitCoroutines.Remove(entityId);
            }
            if (_activeSplitFX.TryGetValue(entityId, out List<GameObject> fxList))
            {
                for (int i = 0; i < fxList.Count; i++)
                {
                    if (fxList[i] != null) Destroy(fxList[i]);
                }
                _activeSplitFX.Remove(entityId);
            }
        }

        // 모든 정방향 FX 정리
        private void CleanupAllSplitFX()
        {
            foreach (var kvp in _activeSplitCoroutines)
            {
                if (kvp.Value != null) StopCoroutine(kvp.Value);
            }
            _activeSplitCoroutines.Clear();

            foreach (var kvp in _activeSplitFX)
            {
                for (int i = 0; i < kvp.Value.Count; i++)
                {
                    if (kvp.Value[i] != null) Destroy(kvp.Value[i]);
                }
            }
            _activeSplitFX.Clear();
        }

        // 역재생 잔존물 전부 정리
        private void CleanupReverseEffects()
        {
            for (int i = 0; i < _reverseCoroutines.Count; i++)
            {
                if (_reverseCoroutines[i] != null) StopCoroutine(_reverseCoroutines[i]);
            }
            _reverseCoroutines.Clear();

            for (int i = 0; i < _reverseEffects.Count; i++)
            {
                if (_reverseEffects[i] != null) Destroy(_reverseEffects[i]);
            }
            _reverseEffects.Clear();

            for (int i = 0; i < _hiddenViews.Count; i++)
            {
                if (_hiddenViews[i] != null) _hiddenViews[i].gameObject.SetActive(true);
            }
            _hiddenViews.Clear();
        }

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

        private GameObject CreateHalf(string name, Vector3 position, Sprite sprite, Color color, int order)
        {
            GameObject obj = new GameObject(name);
            obj.transform.position = position;
            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            return obj;
        }
    }
}