using System.Collections;
using UnityEngine;

namespace MyGame2.Stage
{
    // 얼음 상자가 톱날 함정에 의해 파괴될 때 쪼개지는 연출.
    // 톱날 진행 방향에 수직으로 쪼개진다.
    // 톱날이 좌→우(수평) → 상자가 위/아래로 쪼개짐
    // 톱날이 위→아래(수직) → 상자가 좌/우로 쪼개짐

    public class IceBoxSplitEffect : MonoBehaviour
    {
        [Header("씬 참조")]
        [SerializeField] private StageManager stageManager;

        [Header("스프라이트")]
        [Tooltip("얼음 상자 스프라이트 (Read/Write Enabled 필요)")]
        [SerializeField] private Sprite iceBoxSprite;

        [Header("연출 설정")]
        [Tooltip("쪼개지는 거리 (셀 단위)")]
        [SerializeField] private float splitDistance = 0.8f;

        [Tooltip("연출 시간")]
        [SerializeField] private float splitDuration = 0.5f;

        [Tooltip("회전 각도 (조각이 약간 기울어짐)")]
        [SerializeField] private float rotationAngle = 25f;

        [Tooltip("떨어지는 거리 (쪼개짐 방향과 수직)")]
        [SerializeField] private float dropDistance = 0.3f;

        private void OnEnable()
        {
            if (stageManager == null) return;
            stageManager.Events.IceBoxSawDestroyed += OnIceBoxSawDestroyed;
        }

        private void OnDisable()
        {
            if (stageManager == null) return;
            stageManager.Events.IceBoxSawDestroyed -= OnIceBoxSawDestroyed;
        }

        private void OnIceBoxSawDestroyed(int entityId, GridPos position, Direction sawFacing)
        {
            if (iceBoxSprite == null)
            {
                Debug.LogWarning("[IceBoxSplitEffect] iceBoxSprite가 할당되지 않음");
                return;
            }

            StartCoroutine(PlaySplit(position, sawFacing));
        }

        private IEnumerator PlaySplit(GridPos position, Direction sawFacing)
        {
            Texture2D tex = iceBoxSprite.texture;
            Rect rect = iceBoxSprite.textureRect;
            float ppu = iceBoxSprite.pixelsPerUnit;
            Vector3 worldPos = position.ToWorld(1f);

            // 톱날 방향에 따라 수평/수직 분할 결정
            bool horizontalCut = (sawFacing == Direction.Left || sawFacing == Direction.Right);

            if (!tex.isReadable)
            {
                yield return PlayFallbackSplit(worldPos, horizontalCut);
                yield break;
            }

            int fullWidth = (int)rect.width;
            int fullHeight = (int)rect.height;

            Sprite halfA;
            Sprite halfB;
            Texture2D texA;
            Texture2D texB;

            if (horizontalCut)
            {
                // 톱날이 수평(좌/우) → 위/아래로 쪼개짐
                int halfH = fullHeight / 2;

                // 아래쪽 반
                texA = new Texture2D(fullWidth, halfH, TextureFormat.RGBA32, false);
                texA.SetPixels(tex.GetPixels((int)rect.x, (int)rect.y, fullWidth, halfH));
                texA.Apply();
                texA.filterMode = FilterMode.Point;
                halfA = Sprite.Create(texA, new Rect(0, 0, fullWidth, halfH),
                    new Vector2(0.5f, 1f), ppu);

                // 위쪽 반
                texB = new Texture2D(fullWidth, halfH, TextureFormat.RGBA32, false);
                texB.SetPixels(tex.GetPixels((int)rect.x, (int)rect.y + halfH, fullWidth, halfH));
                texB.Apply();
                texB.filterMode = FilterMode.Point;
                halfB = Sprite.Create(texB, new Rect(0, 0, fullWidth, halfH),
                    new Vector2(0.5f, 0f), ppu);
            }
            else
            {
                // 톱날이 수직(위/아래) → 좌/우로 쪼개짐
                int halfW = fullWidth / 2;

                // 왼쪽 반
                texA = new Texture2D(halfW, fullHeight, TextureFormat.RGBA32, false);
                texA.SetPixels(tex.GetPixels((int)rect.x, (int)rect.y, halfW, fullHeight));
                texA.Apply();
                texA.filterMode = FilterMode.Point;
                halfA = Sprite.Create(texA, new Rect(0, 0, halfW, fullHeight),
                    new Vector2(1f, 0.5f), ppu);

                // 오른쪽 반
                texB = new Texture2D(halfW, fullHeight, TextureFormat.RGBA32, false);
                texB.SetPixels(tex.GetPixels((int)rect.x + halfW, (int)rect.y, halfW, fullHeight));
                texB.Apply();
                texB.filterMode = FilterMode.Point;
                halfB = Sprite.Create(texB, new Rect(0, 0, halfW, fullHeight),
                    new Vector2(0f, 0.5f), ppu);
            }

            GameObject objA = CreateHalf("IceSplit_A", worldPos, halfA, Color.white, 10);
            GameObject objB = CreateHalf("IceSplit_B", worldPos, halfB, Color.white, 10);

            yield return AnimateSplit(objA, objB, horizontalCut);

            Destroy(objA);
            Destroy(objB);
            Destroy(texA);
            Destroy(texB);
        }

        // 텍스처 Read/Write 꺼져있을 때 대체 연출
        private IEnumerator PlayFallbackSplit(Vector3 worldPos, bool horizontalCut)
        {
            GameObject objA = CreateHalf("IceSplit_A", worldPos, iceBoxSprite, Color.white, 10);
            GameObject objB = CreateHalf("IceSplit_B", worldPos, iceBoxSprite, Color.white, 10);

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

            Destroy(objA);
            Destroy(objB);
        }

        // 공통 애니메이션
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
                float eased = t * (2f - t); // EaseOutQuad

                if (horizontalCut)
                {
                    // 위/아래로 벌어짐 (Unity 2D: Y가 위)
                    objA.transform.position = startA + new Vector3(dropDistance * eased, splitDistance * eased, 0);
                    objB.transform.position = startB + new Vector3(-dropDistance * eased, -splitDistance * eased, 0);

                    // 기울어짐 (좌우 방향으로)
                    objA.transform.rotation = Quaternion.Euler(0, 0, -rotationAngle * eased);
                    objB.transform.rotation = Quaternion.Euler(0, 0, rotationAngle * eased);
                }
                else
                {
                    // 좌/우로 벌어짐
                    objA.transform.position = startA + new Vector3(-splitDistance * eased, -dropDistance * eased, 0);
                    objB.transform.position = startB + new Vector3(splitDistance * eased, -dropDistance * eased, 0);

                    // 기울어짐 (상하 방향으로)
                    objA.transform.rotation = Quaternion.Euler(0, 0, rotationAngle * eased);
                    objB.transform.rotation = Quaternion.Euler(0, 0, -rotationAngle * eased);
                }

                // 페이드아웃
                float alpha = 1f - eased;
                Color ca = srA.color; ca.a = alpha; srA.color = ca;
                Color cb = srB.color; cb.a = alpha; srB.color = cb;

                yield return null;
            }
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