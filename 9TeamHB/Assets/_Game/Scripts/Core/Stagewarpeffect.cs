using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyGame2.Stage
{
    /// <summary>
    /// 스테이지 클리어 시 플레이어가 파란 빛 입자로 분해되어 사라지고,
    /// 다음 스테이지에서 입자가 모여 실체화되는 워프 연출.
    ///
    /// [Hierarchy] 빈 오브젝트에 부착
    /// [인스펙터]
    /// - Stage Manager → StageManager 드래그
    /// - Warp Color → 워프 색상 (기본: 하늘색)
    /// - Dissolve Duration → 분해 시간 (기본: 1.2초)
    /// - Materialize Duration → 실체화 시간 (기본: 0.8초)
    /// - Pause Between → 분해 후 대기 시간 (기본: 0.5초)
    /// </summary>
    public sealed class StageWarpEffect : MonoBehaviour
    {
        [Header("씬 참조")]
        [SerializeField] private StageManager stageManager;

        [Header("파티클 설정")]
        [Tooltip("워프 파티클 프리팹 (비워두면 런타임 자동 생성)")]
        [SerializeField] private ParticleSystem warpParticlePrefab;

        [Tooltip("워프 색상")]
        [SerializeField] private Color warpColor = new Color(0.3f, 0.7f, 1f, 1f);

        [Header("타이밍")]
        [Tooltip("분해(사라짐) 시간")]
        [SerializeField] private float dissolveDuration = 1.2f;

        [Tooltip("실체화(나타남) 시간")]
        [SerializeField] private float materializeDuration = 0.8f;

        [Tooltip("분해 후 스테이지 전환 전 대기 시간")]
        [SerializeField] private float pauseBetween = 0.5f;

        [Header("파티클 세부 설정")]
        [Tooltip("입자 개수")]
        [SerializeField] private int particleCount = 30;

        [Tooltip("입자 상승 높이")]
        [SerializeField] private float riseHeight = 3f;

        [Tooltip("입자 퍼짐 범위")]
        [SerializeField] private float spreadRadius = 0.5f;

        // 워프 중인지 여부 (외부에서 확인용)
        public bool IsWarping { get; private set; }

        private void OnEnable()
        {
            if (stageManager != null)
                stageManager.Events.StageClearTriggered += OnStageClear;
        }

        private void OnDisable()
        {
            if (stageManager != null)
                stageManager.Events.StageClearTriggered -= OnStageClear;
        }

        private void OnStageClear()
        {
            if (!IsWarping)
                StartCoroutine(WarpSequence());
        }

        // ── 워프 시퀀스 ──

        private IEnumerator WarpSequence()
        {
            IsWarping = true;

            // 플레이어 View 수집
            List<GridEntityView> playerViews = CollectPlayerViews();
            List<SpriteRenderer[]> playerRenderers = new List<SpriteRenderer[]>();

            for (int i = 0; i < playerViews.Count; i++)
                playerRenderers.Add(playerViews[i].GetComponentsInChildren<SpriteRenderer>());

            // ── 1단계: 분해 (Dissolve) ──
            List<ParticleSystem> dissolveParticles = new List<ParticleSystem>();

            for (int i = 0; i < playerViews.Count; i++)
            {
                ParticleSystem ps = SpawnParticles(
                    playerViews[i].transform.position, true);
                dissolveParticles.Add(ps);
            }

            // 페이드 아웃
            float elapsed = 0f;
            while (elapsed < dissolveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / dissolveDuration);
                float alpha = 1f - EaseInQuad(t);

                for (int i = 0; i < playerRenderers.Count; i++)
                    SetAlpha(playerRenderers[i], alpha);

                yield return null;
            }

            for (int i = 0; i < playerRenderers.Count; i++)
                SetAlpha(playerRenderers[i], 0f);

            // ── 2단계: 대기 ──
            yield return new WaitForSeconds(pauseBetween);

            for (int i = 0; i < dissolveParticles.Count; i++)
                if (dissolveParticles[i] != null)
                    Destroy(dissolveParticles[i].gameObject);

            // ── 3단계: 다음 스테이지 로드 트리거 ──
            stageManager.Events?.RaiseWarpComplete();

            yield return null; // 1프레임 대기 (새 View 생성 기다림)

            playerViews = CollectPlayerViews();
            playerRenderers.Clear();

            for (int i = 0; i < playerViews.Count; i++)
                playerRenderers.Add(playerViews[i].GetComponentsInChildren<SpriteRenderer>());

            for (int i = 0; i < playerRenderers.Count; i++)
                SetAlpha(playerRenderers[i], 0f);

            // ── 4단계: 실체화 (Materialize) ──
            List<ParticleSystem> materializeParticles = new List<ParticleSystem>();

            for (int i = 0; i < playerViews.Count; i++)
            {
                ParticleSystem ps = SpawnParticles(
                    playerViews[i].transform.position, false);
                materializeParticles.Add(ps);
            }

            elapsed = 0f;
            while (elapsed < materializeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / materializeDuration);
                float alpha = EaseOutQuad(t);

                for (int i = 0; i < playerRenderers.Count; i++)
                    SetAlpha(playerRenderers[i], alpha);

                yield return null;
            }

            for (int i = 0; i < playerRenderers.Count; i++)
                SetAlpha(playerRenderers[i], 1f);

            for (int i = 0; i < materializeParticles.Count; i++)
                if (materializeParticles[i] != null)
                    Destroy(materializeParticles[i].gameObject);

            IsWarping = false;
        }

        // ── 플레이어 View 수집 ──

        private List<GridEntityView> CollectPlayerViews()
        {
            List<GridEntityView> result = new List<GridEntityView>(2);
            GridEntityView[] allViews = FindObjectsByType<GridEntityView>(
                FindObjectsSortMode.None);

            for (int i = 0; i < allViews.Length; i++)
            {
                if (allViews[i].Kind == EntityKind.Player)
                    result.Add(allViews[i]);
            }

            return result;
        }

        // ── 파티클 생성 ──

        private ParticleSystem SpawnParticles(Vector3 position, bool isDissolve)
        {
            if (warpParticlePrefab != null)
            {
                ParticleSystem ps = Instantiate(warpParticlePrefab, position,
                    Quaternion.identity);
                ps.Play();
                return ps;
            }

            return CreateDefaultParticles(position, isDissolve);
        }

        private ParticleSystem CreateDefaultParticles(Vector3 position, bool isDissolve)
        {
            GameObject obj = new GameObject(isDissolve ? "WarpDissolve" : "WarpMaterialize");
            obj.transform.position = position;

            ParticleSystem ps = obj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = isDissolve ? dissolveDuration : materializeDuration;
            main.loop = false;
            main.startLifetime = isDissolve ? dissolveDuration * 0.8f : materializeDuration * 0.8f;
            main.startSpeed = isDissolve ? riseHeight / dissolveDuration : riseHeight / materializeDuration;
            main.startSize = 0.08f;
            main.startColor = warpColor;
            main.maxParticles = particleCount;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = isDissolve ? -0.3f : 0.3f;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBurst(0, new ParticleSystem.Burst(0f, particleCount));

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = spreadRadius;

            if (isDissolve)
                shape.rotation = new Vector3(-90f, 0f, 0f);
            else
            {
                shape.rotation = new Vector3(90f, 0f, 0f);
                obj.transform.position = position + Vector3.up * riseHeight;
            }

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            if (isDissolve)
            {
                sizeCurve.AddKey(0f, 1f);
                sizeCurve.AddKey(0.7f, 0.6f);
                sizeCurve.AddKey(1f, 0f);
            }
            else
            {
                sizeCurve.AddKey(0f, 0f);
                sizeCurve.AddKey(0.3f, 0.8f);
                sizeCurve.AddKey(1f, 0f);
            }
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(warpColor, 0f),
                    new GradientColorKey(Color.white, 0.5f),
                    new GradientColorKey(warpColor, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 0.7f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = gradient;

            var renderer = obj.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.material.color = warpColor;
            renderer.sortingOrder = 10;

            ps.Play();
            return ps;
        }

        // ── 유틸리티

        private void SetAlpha(SpriteRenderer[] renderers, float alpha)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                Color c = renderers[i].color;
                c.a = alpha;
                renderers[i].color = c;
            }
        }

        private float EaseInQuad(float t) { return t * t; }
        private float EaseOutQuad(float t) { return 1f - (1f - t) * (1f - t); }
    }
}