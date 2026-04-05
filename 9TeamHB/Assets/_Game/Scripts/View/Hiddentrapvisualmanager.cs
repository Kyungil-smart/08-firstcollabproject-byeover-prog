using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyGame2.Stage
{
    // 히든 함정 타일의 시각 연출 관리자

    public class HiddenTrapVisualManager : MonoBehaviour
    {
        [Header("씬 참조")]
        [SerializeField] private StageManager stageManager;

        [Header("스프라이트 프레임")]
        [Tooltip("trap_plate 슬라이스. [0]=바닥, [마지막]=함정 발동")]
        [SerializeField] private Sprite[] frames;

        [Header("연출 시간")]
        [SerializeField] private float activeDuration = 0.4f;
        [SerializeField] private float rewindDuration = 0.5f;
        [SerializeField] private float rewindDelay = 1.0f;

        [Header("렌더링")]
        [SerializeField] private int sortingOrder = -5;

        [Header("비활성화 표시")]
        [Tooltip("비활성화 시 스프라이트 색상 (반투명 녹색 등)")]
        [SerializeField] private Color deactivatedColor = new Color(0.3f, 0.8f, 0.3f, 0.5f);
        [Tooltip("활성 상태 기본 색상")]
        [SerializeField] private Color activeColor = Color.white;

        private Transform _root;
        private readonly Dictionary<long, TrapTile> _tiles = new Dictionary<long, TrapTile>();

        private struct TrapTile
        {
            public GameObject Obj;
            public SpriteRenderer Renderer;
            public GridPos Position;
            public bool Revealed;
            public bool Deactivated;
        }

        private void OnEnable()
        {
            if (stageManager == null) return;
            stageManager.Events.StageLoaded += OnStageLoaded;
            stageManager.Events.HiddenTrapRevealed += OnTrapRevealed;
            stageManager.Events.HiddenTrapPlayerKill += OnHiddenTrapPlayerKill;
            stageManager.Events.GameOverTriggered += OnGameOver;
            stageManager.Events.TurnExecuted += OnTurnExecuted;
        }

        private void OnDisable()
        {
            if (stageManager == null) return;
            stageManager.Events.StageLoaded -= OnStageLoaded;
            stageManager.Events.HiddenTrapRevealed -= OnTrapRevealed;
            stageManager.Events.HiddenTrapPlayerKill -= OnHiddenTrapPlayerKill;
            stageManager.Events.GameOverTriggered -= OnGameOver;
            stageManager.Events.TurnExecuted -= OnTurnExecuted;
        }

        private void Start()
        {
            if (stageManager != null && stageManager.CurrentState != null)
                BuildTiles(stageManager.CurrentState);
        }

        private void OnStageLoaded(int idx)
        {
            BuildTiles(stageManager.CurrentState);
        }

        private void BuildTiles(StageState state)
        {
            ClearTiles();

            if (_root == null)
            {
                GameObject obj = new GameObject("_HiddenTrapRoot");
                obj.transform.SetParent(transform, false);
                _root = obj.transform;
            }

            if (frames == null || frames.Length == 0) return;

            for (int y = 0; y < state.Height; y++)
            {
                for (int x = 0; x < state.Width; x++)
                {
                    GridPos pos = new GridPos(x, y);
                    CellData cell = state.GetCell(pos);
                    if (!cell.HasHiddenTrap) continue;

                    GameObject tileObj = new GameObject($"HT_{x}_{y}");
                    tileObj.transform.SetParent(_root, false);
                    tileObj.transform.position = pos.ToWorld(1f);

                    SpriteRenderer sr = tileObj.AddComponent<SpriteRenderer>();
                    sr.sprite = frames[0];
                    sr.sortingOrder = sortingOrder;
                    sr.color = activeColor;

                    long key = PosKey(pos);
                    _tiles[key] = new TrapTile
                    {
                        Obj = tileObj,
                        Renderer = sr,
                        Position = pos,
                        Revealed = false,
                        Deactivated = false
                    };
                }
            }
        }

        // 턴 실행 후: 비활성화/재활성화 상태 체크
        private void OnTurnExecuted(TurnOutcome outcome)
        {
            StageState state = stageManager.CurrentState;
            if (state == null) return;

            List<long> keys = new List<long>(_tiles.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                long key = keys[i];
                TrapTile tile = _tiles[key];
                if (tile.Renderer == null) continue;

                CellData cell = state.GetCell(tile.Position);
                bool isNowActive = cell.HasHiddenTrap;

                if (tile.Deactivated && isNowActive)
                {
                    // 재활성화됨 — 원래 색상 복원
                    tile.Deactivated = false;
                    tile.Renderer.color = activeColor;
                    _tiles[key] = tile;
                }
                else if (!tile.Deactivated && !isNowActive && !tile.Revealed)
                {
                    // 비활성화됨 — 비활성화 색상으로 전환
                    tile.Deactivated = true;
                    tile.Renderer.color = deactivatedColor;
                    _tiles[key] = tile;
                }
            }
        }

        // 함정 타일 비주얼만 전환 (Kill은 아직 안 함)
        private void OnTrapRevealed(GridPos pos)
        {
            long key = PosKey(pos);
            if (!_tiles.TryGetValue(key, out TrapTile tile)) return;
            if (tile.Revealed) return;

            tile.Revealed = true;
            _tiles[key] = tile;

            StartCoroutine(PlayActive(tile));
        }

        // 애니메이션 끝난 후 플레이어 Kill + GameOver 처리
        private void OnHiddenTrapPlayerKill(int playerId, GridPos trapPos)
        {
            StartCoroutine(DelayedKill(playerId, trapPos));
        }

        private IEnumerator DelayedKill(int playerId, GridPos trapPos)
        {
            // Active 애니메이션이 끝날 때까지 대기 + 여유 시간
            yield return new WaitForSeconds(activeDuration + 0.3f);

            StageState state = stageManager.CurrentState;
            if (state == null) yield break;

            // 플레이어 Kill + 게임오버 이벤트 발행 (팝업 표시)
            state.KillEntity(playerId);
            state.ConfirmGameOver();
            state.SetViewDirty();
        }

        // Active: 프레임 순방향 재생
        private IEnumerator PlayActive(TrapTile tile)
        {
            SpriteRenderer sr = tile.Renderer;
            if (sr == null || frames.Length <= 1) yield break;

            // 발동 시 색상 원래대로 복원 (비활성화 상태에서 발동될 일은 없지만 안전장치)
            sr.color = activeColor;

            int totalFrames = frames.Length;
            float elapsed = 0f;

            while (elapsed < activeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / activeDuration);

                int frameIndex = Mathf.FloorToInt(t * (totalFrames - 1));
                frameIndex = Mathf.Clamp(frameIndex, 0, totalFrames - 1);
                sr.sprite = frames[frameIndex];

                yield return null;
            }

            sr.sprite = frames[totalFrames - 1];
        }

        // Rewind: GameOver 후 프레임 역방향 재생
        private void OnGameOver()
        {
            StartCoroutine(PlayRewindAll());
        }

        private IEnumerator PlayRewindAll()
        {
            yield return new WaitForSeconds(rewindDelay);

            List<TrapTile> revealedTiles = new List<TrapTile>();
            foreach (var pair in _tiles)
            {
                if (pair.Value.Revealed)
                    revealedTiles.Add(pair.Value);
            }

            if (revealedTiles.Count == 0 || frames.Length <= 1) yield break;

            int totalFrames = frames.Length;
            float elapsed = 0f;

            while (elapsed < rewindDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / rewindDuration);

                int frameIndex = Mathf.FloorToInt((1f - t) * (totalFrames - 1));
                frameIndex = Mathf.Clamp(frameIndex, 0, totalFrames - 1);

                for (int i = 0; i < revealedTiles.Count; i++)
                {
                    SpriteRenderer sr = revealedTiles[i].Renderer;
                    if (sr != null)
                        sr.sprite = frames[frameIndex];
                }

                yield return null;
            }

            for (int i = 0; i < revealedTiles.Count; i++)
            {
                SpriteRenderer sr = revealedTiles[i].Renderer;
                if (sr != null)
                    sr.sprite = frames[0];
            }
        }

        private void ClearTiles()
        {
            StopAllCoroutines();
            foreach (var pair in _tiles)
            {
                if (pair.Value.Obj != null)
                    Destroy(pair.Value.Obj);
            }
            _tiles.Clear();
        }

        private static long PosKey(GridPos pos)
        {
            return ((long)pos.Y << 16) | (long)(pos.X & 0xFFFF);
        }
    }
}