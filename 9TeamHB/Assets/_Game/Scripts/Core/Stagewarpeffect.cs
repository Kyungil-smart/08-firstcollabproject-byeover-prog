using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyGame2.Stage
{
    // 스테이지 워프 연출.
    // 시작: 파티클이 먼저 내려옴 → 플레이어가 서서히 나타남
    // 클리어: 플레이어가 아래→위로 분해 → 파티클이 수직 상승 → 사라짐
    
    public sealed class StageWarpEffect : MonoBehaviour
    {
        [Header("씬 참조")]
        [SerializeField] private StageManager stageManager;

        [Header("색상")]
        [Tooltip("워프 색상")]
        [SerializeField] private Color warpColor = new Color(0.3f, 0.7f, 1f, 1f);

        [Header("시작 연출 타이밍")]
        [Tooltip("파티클만 내려오는 시간 (플레이어 안 보임)")]
        [SerializeField] private float particleLeadDuration = 1.0f;

        [Tooltip("플레이어 페이드인 시간")]
        [SerializeField] private float spawnFadeInDuration = 0.8f;

        [Tooltip("워프색 → 원본색 복원 시간")]
        [SerializeField] private float spawnColorRestoreDuration = 0.3f;

        [Header("클리어 연출 타이밍")]
        [Tooltip("클리어 판정 후 워프 시작까지 대기")]
        [SerializeField] private float startDelay = 0.8f;

        [Tooltip("분해 전 색 전환 시간")]
        [SerializeField] private float colorShiftDuration = 0.3f;

        [Tooltip("전체 분해 시간 (아래→위 순차)")]
        [SerializeField] private float shatterDuration = 1.5f;

        [Tooltip("파티클 잔류 시간")]
        [SerializeField] private float particleTrailDuration = 0.6f;

        [Tooltip("스테이지 전환 전 대기")]
        [SerializeField] private float pauseBetween = 0.5f;

        [Header("분해 설정")]
        [Tooltip("조각 크기 (픽셀, 작을수록 세밀)")]
        [SerializeField] private int chunkPixelSize = 4;

        [Tooltip("조각 상승 높이")]
        [SerializeField] private float shatterRiseHeight = 5f;

        [Tooltip("조각 좌우 흔들림")]
        [SerializeField] private float shatterDrift = 0.3f;

        [Header("파티클 설정")]
        [Tooltip("파티클 입자 수")]
        [SerializeField] private int particleCount = 40;

        [Tooltip("파티클 속도")]
        [SerializeField] private float particleSpeed = 4f;

        [Tooltip("파티클 크기")]
        [SerializeField] private float particleSize = 0.1f;

        public bool IsWarping { get; private set; }

        private void OnEnable()
        {
            if (stageManager == null) return;
            stageManager.Events.StageClearTriggered += OnStageClear;
            stageManager.Events.StageLoaded += OnStageLoaded;
        }

        private void OnDisable()
        {
            if (stageManager == null) return;
            stageManager.Events.StageClearTriggered -= OnStageClear;
            stageManager.Events.StageLoaded -= OnStageLoaded;
        }

        private void OnStageClear()
        {
            if (!IsWarping)
                StartCoroutine(DissolveSequence());
        }

        private void OnStageLoaded(int stageIndex)
        {
            // 즉시 플레이어 투명 처리 (1프레임도 보이지 않게)
            HideAllPlayers();
            StartCoroutine(SpawnSequence());
        }

        // 모든 플레이어 View를 즉시 투명으로
        private void HideAllPlayers()
        {
            GridEntityView[] allViews = FindObjectsByType<GridEntityView>(
                FindObjectsSortMode.None);
            for (int i = 0; i < allViews.Length; i++)
            {
                if (allViews[i].Kind != EntityKind.Player) continue;
                SpriteRenderer[] srs = allViews[i].GetComponentsInChildren<SpriteRenderer>();
                for (int j = 0; j < srs.Length; j++)
                {
                    Color c = srs[j].color;
                    c.a = 0f;
                    srs[j].color = c;
                }
            }
        }

        // 스테이지 시작: 파티클 선행 → 플레이어 페이드인

        private IEnumerator SpawnSequence()
        {
            yield return null; // Bind 완료 대기

            List<GridEntityView> playerViews = CollectPlayerViews();
            if (playerViews.Count == 0) yield break;

            IsWarping = true;

            List<SpriteRenderer[]> allRenderers = new List<SpriteRenderer[]>();
            List<Color[]> originalColors = new List<Color[]>();

            for (int i = 0; i < playerViews.Count; i++)
            {
                SpriteRenderer[] srs = playerViews[i].GetComponentsInChildren<SpriteRenderer>();
                allRenderers.Add(srs);
                Color[] colors = new Color[srs.Length];
                for (int j = 0; j < srs.Length; j++)
                {
                    colors[j] = srs[j].color;
                    // 알파가 0으로 저장됐을 수 있으니 강제로 1 복원
                    if (colors[j].a < 0.01f)
                        colors[j].a = 1f;
                }
                originalColors.Add(colors);
            }

            // 원본 색상 저장 완료 후 투명으로
            for (int i = 0; i < allRenderers.Count; i++)
            for (int j = 0; j < allRenderers[i].Length; j++)
            {
                Color c = allRenderers[i][j].color;
                c.a = 0f;
                allRenderers[i][j].color = c;
            }


            // 하강 파티클 생성 (플레이어 위치에서)
            List<ParticleSystem> particles = new List<ParticleSystem>();
            for (int i = 0; i < playerViews.Count; i++)
            {
                ParticleSystem ps = CreateMaterializeParticles(
                    playerViews[i].transform.position);
                particles.Add(ps);
            }

            // 1단계: 파티클만 내려옴 (플레이어 안 보임)
            yield return new WaitForSeconds(particleLeadDuration);

            // 2단계: 플레이어 페이드인 (워프색 → 불투명)
            float elapsed = 0f;
            while (elapsed < spawnFadeInDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / spawnFadeInDuration);

                float alpha = EaseOutQuad(t);

                for (int i = 0; i < allRenderers.Count; i++)
                {
                    SpriteRenderer[] srs = allRenderers[i];
                    Color[] orig = originalColors[i];
                    for (int j = 0; j < srs.Length; j++)
                    {
                        Color c = Color.Lerp(warpColor, orig[j], t * 0.3f);
                        c.a = orig[j].a * alpha;
                        srs[j].color = c;
                    }
                }
                yield return null;
            }

            // 3단계: 워프색 → 원본색 복원
            elapsed = 0f;
            while (elapsed < spawnColorRestoreDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / spawnColorRestoreDuration);

                for (int i = 0; i < allRenderers.Count; i++)
                {
                    SpriteRenderer[] srs = allRenderers[i];
                    Color[] orig = originalColors[i];
                    for (int j = 0; j < srs.Length; j++)
                    {
                        Color warpBlend = Color.Lerp(warpColor, orig[j], 0.3f);
                        srs[j].color = Color.Lerp(warpBlend, orig[j], t);
                    }
                }
                yield return null;
            }

            // 완전 복원
            for (int i = 0; i < allRenderers.Count; i++)
                for (int j = 0; j < allRenderers[i].Length; j++)
                    allRenderers[i][j].color = originalColors[i][j];

            // 파티클 정리
            for (int i = 0; i < particles.Count; i++)
                if (particles[i] != null)
                    Destroy(particles[i].gameObject);

            IsWarping = false;
        }

        // 클리어: 분해 연출
        // 아래→위 순차 분해 + 파티클 수직 상승

        private IEnumerator DissolveSequence()
        {
            IsWarping = true;

            // 0: 딜레이
            yield return new WaitForSeconds(startDelay);

            List<GridEntityView> playerViews = CollectPlayerViews();
            if (playerViews.Count == 0) { IsWarping = false; yield break; }

            List<SpriteRenderer[]> allRenderers = new List<SpriteRenderer[]>();
            List<Color[]> originalColors = new List<Color[]>();

            for (int i = 0; i < playerViews.Count; i++)
            {
                SpriteRenderer[] srs = playerViews[i].GetComponentsInChildren<SpriteRenderer>();
                allRenderers.Add(srs);
                Color[] colors = new Color[srs.Length];
                for (int j = 0; j < srs.Length; j++) colors[j] = srs[j].color;
                originalColors.Add(colors);
            }

            // 1: 색 전환 (원본 → 밝은 워프색)
            float elapsed = 0f;
            while (elapsed < colorShiftDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / colorShiftDuration);
                for (int i = 0; i < allRenderers.Count; i++)
                {
                    SpriteRenderer[] srs = allRenderers[i];
                    Color[] orig = originalColors[i];
                    for (int j = 0; j < srs.Length; j++)
                    {
                        Color target = Color.Lerp(Color.white, warpColor, 0.4f);
                        srs[j].color = Color.Lerp(orig[j], target, t);
                    }
                }
                yield return null;
            }

            // 2: 스프라이트 분해 (아래→위 순차)
            List<List<ShatterChunk>> allChunks = new List<List<ShatterChunk>>();
            List<ParticleSystem> trailParticles = new List<ParticleSystem>();
            List<int> maxRows = new List<int>();

            for (int i = 0; i < playerViews.Count; i++)
            {
                SpriteRenderer mainSr = FindMainRenderer(allRenderers[i]);
                Vector3 playerPos = playerViews[i].transform.position;

                List<ShatterChunk> chunks = CreateShatterChunks(mainSr, playerPos);
                allChunks.Add(chunks);

                int mr = 0;
                for (int j = 0; j < chunks.Count; j++)
                    if (chunks[j].Row > mr) mr = chunks[j].Row;
                maxRows.Add(mr);

                // 원본 숨김
                playerViews[i].gameObject.SetActive(false);

                // 수직 파티클
                ParticleSystem ps = CreateVerticalParticles(playerPos);
                trailParticles.Add(ps);
            }

            // 분해 애니메이션
            elapsed = 0f;
            while (elapsed < shatterDuration)
            {
                elapsed += Time.deltaTime;
                float globalT = Mathf.Clamp01(elapsed / shatterDuration);

                for (int i = 0; i < allChunks.Count; i++)
                {
                    List<ShatterChunk> chunks = allChunks[i];
                    int totalRows = maxRows[i] + 1;

                    for (int j = 0; j < chunks.Count; j++)
                    {
                        ShatterChunk chunk = chunks[j];
                        if (chunk.Obj == null) continue;

                        float rowDelay = (float)chunk.Row / totalRows * 0.5f;
                        float localT = Mathf.Clamp01((globalT - rowDelay) / (1f - rowDelay));

                        if (localT <= 0f) continue;

                        float rise = EaseInQuad(localT) * shatterRiseHeight;
                        float drift = Mathf.Sin(localT * Mathf.PI * 2f + chunk.DriftPhase)
                            * shatterDrift * localT;

                        chunk.Obj.transform.position = chunk.StartPos
                            + Vector3.up * rise
                            + Vector3.right * drift;

                        float scale = Mathf.Lerp(1f, 0f, EaseInQuad(localT));
                        chunk.Obj.transform.localScale = chunk.StartScale * scale;

                        if (chunk.Renderer != null)
                        {
                            Color c = chunk.Renderer.color;
                            c.a = 1f - EaseInQuad(localT);
                            chunk.Renderer.color = c;
                        }
                    }
                }
                yield return null;
            }

            // 조각 정리
            for (int i = 0; i < allChunks.Count; i++)
                for (int j = 0; j < allChunks[i].Count; j++)
                    if (allChunks[i][j].Obj != null)
                        Destroy(allChunks[i][j].Obj);

            // 3: 파티클 잔류
            yield return new WaitForSeconds(particleTrailDuration);

            for (int i = 0; i < trailParticles.Count; i++)
                if (trailParticles[i] != null)
                    Destroy(trailParticles[i].gameObject);

            // 4: 대기 → 다음 스테이지
            yield return new WaitForSeconds(pauseBetween);

            stageManager.Events?.RaiseWarpComplete();
            yield return null;
            yield return null;

            IsWarping = false;
        }

        // 스프라이트 분해

        private struct ShatterChunk
        {
            public GameObject Obj;
            public SpriteRenderer Renderer;
            public Vector3 StartPos;
            public Vector3 StartScale;
            public int Row;
            public float DriftPhase;
        }

        private List<ShatterChunk> CreateShatterChunks(SpriteRenderer source, Vector3 worldPos)
        {
            List<ShatterChunk> chunks = new List<ShatterChunk>();

            if (source == null || source.sprite == null)
                return chunks;

            Sprite sprite = source.sprite;
            Texture2D tex = sprite.texture;

            if (!tex.isReadable)
                return CreateFallbackChunks(source, worldPos);

            Rect rect = sprite.textureRect;
            float ppu = sprite.pixelsPerUnit;
            Vector2 pivot = sprite.pivot;

            int cols = Mathf.CeilToInt(rect.width / chunkPixelSize);
            int rows = Mathf.CeilToInt(rect.height / chunkPixelSize);

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    int px = (int)rect.x + col * chunkPixelSize;
                    int py = (int)rect.y + row * chunkPixelSize;
                    int w = Mathf.Min(chunkPixelSize, (int)(rect.x + rect.width) - px);
                    int h = Mathf.Min(chunkPixelSize, (int)(rect.y + rect.height) - py);

                    if (w <= 0 || h <= 0) continue;

                    Color[] pixels = tex.GetPixels(px, py, w, h);
                    float avgAlpha = 0f;
                    for (int i = 0; i < pixels.Length; i++) avgAlpha += pixels[i].a;
                    avgAlpha /= pixels.Length;
                    if (avgAlpha < 0.1f) continue;

                    Texture2D chunkTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                    chunkTex.SetPixels(pixels);
                    chunkTex.Apply();
                    chunkTex.filterMode = FilterMode.Point;

                    Sprite chunkSprite = Sprite.Create(chunkTex,
                        new Rect(0, 0, w, h),
                        new Vector2(0.5f, 0.5f), ppu);

                    float localX = (col * chunkPixelSize + w * 0.5f - pivot.x) / ppu;
                    float localY = (row * chunkPixelSize + h * 0.5f - pivot.y) / ppu;

                    Vector3 chunkPos = worldPos + new Vector3(
                        localX * source.transform.lossyScale.x,
                        localY * source.transform.lossyScale.y, 0f);

                    GameObject obj = new GameObject($"Chunk_{row}_{col}");
                    obj.transform.position = chunkPos;
                    obj.transform.localScale = source.transform.lossyScale;

                    SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
                    sr.sprite = chunkSprite;
                    Color avgColor = Color.clear;
                    for (int i = 0; i < pixels.Length; i++) avgColor += pixels[i];
                    avgColor /= pixels.Length;
                    sr.color = Color.Lerp(avgColor, warpColor, 0.3f);
                    sr.sortingOrder = 15;

                    chunks.Add(new ShatterChunk
                    {
                        Obj = obj,
                        Renderer = sr,
                        StartPos = chunkPos,
                        StartScale = obj.transform.localScale,
                        Row = row,
                        DriftPhase = Random.Range(0f, Mathf.PI * 2f)
                    });
                }
            }

            return chunks;
        }

        // 텍스처 Read/Write 안 켜져 있을 때 대체 조각
        private List<ShatterChunk> CreateFallbackChunks(SpriteRenderer source, Vector3 worldPos)
        {
            List<ShatterChunk> chunks = new List<ShatterChunk>();

            Texture2D whiteTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Color[] px = new Color[4];
            for (int i = 0; i < 4; i++) px[i] = Color.white;
            whiteTex.SetPixels(px); whiteTex.Apply();
            Sprite ws = Sprite.Create(whiteTex, new Rect(0, 0, 2, 2),
                new Vector2(0.5f, 0.5f), 16f);

            int gridSize = 4;
            float cellSize = 0.2f;

            for (int row = 0; row < gridSize; row++)
            {
                for (int col = 0; col < gridSize; col++)
                {
                    Vector3 offset = new Vector3(
                        (col - gridSize * 0.5f + 0.5f) * cellSize,
                        (row - gridSize * 0.5f + 0.5f) * cellSize, 0f);

                    Vector3 chunkPos = worldPos + offset;
                    GameObject obj = new GameObject($"FB_{row}_{col}");
                    obj.transform.position = chunkPos;
                    obj.transform.localScale = Vector3.one * cellSize * 0.9f;

                    SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
                    sr.sprite = ws;
                    sr.color = Color.Lerp(source.color, warpColor, 0.5f);
                    sr.sortingOrder = 15;

                    chunks.Add(new ShatterChunk
                    {
                        Obj = obj, Renderer = sr,
                        StartPos = chunkPos,
                        StartScale = obj.transform.localScale,
                        Row = row,
                        DriftPhase = Random.Range(0f, Mathf.PI * 2f)
                    });
                }
            }

            return chunks;
        }

        // 파티클 — 분해 시 수직 상승
        private ParticleSystem CreateVerticalParticles(Vector3 position)
        {
            GameObject obj = new GameObject("WarpTrail");
            obj.transform.position = position;

            ParticleSystem ps = obj.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            float dur = shatterDuration + particleTrailDuration;

            var main = ps.main;
            main.playOnAwake = false;
            main.duration = dur;
            main.loop = false;
            main.startLifetime = 2f;
            main.startSpeed = particleSpeed;
            main.startSize = particleSize;
            main.startColor = warpColor;
            main.maxParticles = particleCount * 3;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0f;

            var emission = ps.emission;
            emission.rateOverTime = particleCount;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 5f;
            shape.radius = 0.15f;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            AnimationCurve sc = new AnimationCurve();
            sc.AddKey(0f, 0.8f);
            sc.AddKey(0.3f, 1f);
            sc.AddKey(0.8f, 0.4f);
            sc.AddKey(1f, 0f);
            sol.size = new ParticleSystem.MinMaxCurve(1f, sc);

            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient g = new Gradient();
            Color bright = warpColor * 1.5f; bright.a = 1f;
            g.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(warpColor, 0f),
                    new GradientColorKey(bright, 0.2f),
                    new GradientColorKey(Color.white, 0.5f),
                    new GradientColorKey(warpColor, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0.8f, 0f),
                    new GradientAlphaKey(1f, 0.2f),
                    new GradientAlphaKey(0.5f, 0.7f),
                    new GradientAlphaKey(0f, 1f)
                });
            col.color = g;

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.08f;
            noise.frequency = 2f;

            var renderer = obj.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.material.color = warpColor;
            renderer.sortingOrder = 12;

            ps.Play();
            return ps;
        }

        // 파티클 — 실체화 시 수직 하강
        private ParticleSystem CreateMaterializeParticles(Vector3 position)
        {
            GameObject obj = new GameObject("WarpMaterialize");
            obj.transform.position = position + Vector3.up * 6f;

            ParticleSystem ps = obj.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            // 파티클 선행 + 페이드인 전체 시간 동안 지속
            float totalDur = particleLeadDuration + spawnFadeInDuration + spawnColorRestoreDuration;

            var main = ps.main;
            main.playOnAwake = false;
            main.duration = totalDur;
            main.loop = false;
            main.startLifetime = 1.5f;
            main.startSpeed = particleSpeed;
            main.startSize = particleSize;
            main.startColor = warpColor;
            main.maxParticles = particleCount * 3;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0f;

            var emission = ps.emission;
            emission.rateOverTime = particleCount;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 5f;
            shape.radius = 0.2f;
            shape.rotation = new Vector3(90f, 0f, 0f);

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            AnimationCurve sc = new AnimationCurve();
            sc.AddKey(0f, 0.3f);
            sc.AddKey(0.4f, 1f);
            sc.AddKey(1f, 0f);
            sol.size = new ParticleSystem.MinMaxCurve(1f, sc);

            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient g = new Gradient();
            Color bright = warpColor * 1.5f; bright.a = 1f;
            g.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(bright, 0f),
                    new GradientColorKey(warpColor, 0.5f),
                    new GradientColorKey(warpColor, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.6f, 0.6f),
                    new GradientAlphaKey(0f, 1f)
                });
            col.color = g;

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.1f;
            noise.frequency = 2f;

            var renderer = obj.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.material.color = warpColor;
            renderer.sortingOrder = 12;

            ps.Play();
            return ps;
        }

        // 유틸리티

        private List<GridEntityView> CollectPlayerViews()
        {
            List<GridEntityView> result = new List<GridEntityView>(2);
            GridEntityView[] allViews = FindObjectsByType<GridEntityView>(
                FindObjectsSortMode.None);
            for (int i = 0; i < allViews.Length; i++)
                if (allViews[i].Kind == EntityKind.Player && allViews[i].gameObject.activeSelf)
                    result.Add(allViews[i]);
            return result;
        }

        private SpriteRenderer FindMainRenderer(SpriteRenderer[] renderers)
        {
            SpriteRenderer best = null;
            float bestArea = 0f;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].sprite == null) continue;
                float area = renderers[i].sprite.rect.width * renderers[i].sprite.rect.height;
                if (area > bestArea) { bestArea = area; best = renderers[i]; }
            }
            return best;
        }

        private float EaseInQuad(float t) { return t * t; }
        private float EaseOutQuad(float t) { return 1f - (1f - t) * (1f - t); }
    }
}