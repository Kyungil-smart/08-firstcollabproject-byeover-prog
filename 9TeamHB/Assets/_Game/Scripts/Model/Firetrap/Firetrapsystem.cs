using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyGame2.Stage
{
    // 불 함정 시스템: 주기적으로 불을 발사하여 범위 내 엔티티에 피해.
    // 부서지는 상자, 얼음 상자 -> 파괴
    // 플레이어 -> 즉사 (게임오버)
    // 일반 상자, P1 상자, 철 상자 ->불을 차단 (뒤쪽은 안전)
    // 벽 -> 차단

    public sealed class FireTrapSystem : MonoBehaviour
    {
        [Header("씬 참조")]
        [SerializeField] private StageManager stageManager;

        [Header("방향별 스프라이트 프레임")]
        [Tooltip("[0]=시작, [마지막]=최대 불꽃. 스프라이트 시트에서 슬라이스")]
        [SerializeField] private Sprite[] framesUp;
        [SerializeField] private Sprite[] framesDown;
        [SerializeField] private Sprite[] framesLeft;
        [SerializeField] private Sprite[] framesRight;

        [Header("렌더링")]
        [SerializeField] private int sortingOrder = 3;

        private Transform _root;
        private readonly Dictionary<int, TrapState> _traps = new Dictionary<int, TrapState>(8);

        private class TrapState
        {
            public int EntityId;
            public GridPos Position;
            public Direction Facing;
            public float FireInterval;
            public float FireDuration;
            public int Range;

            public float Timer;
            public bool IsActive;
            public float ActiveTimer;
            public List<GameObject> FireVisuals;
            public Coroutine AnimCoroutine;
        }

        private void OnEnable()
        {
            if (stageManager == null) return;
            stageManager.Events.StageLoaded += OnStageLoaded;
        }

        private void OnDisable()
        {
            if (stageManager == null) return;
            stageManager.Events.StageLoaded -= OnStageLoaded;
        }

        private void Start()
        {
            if (stageManager != null && stageManager.CurrentState != null)
                BuildTraps(stageManager.CurrentState);
        }

        private void OnStageLoaded(int idx)
        {
            BuildTraps(stageManager.CurrentState);
        }

        private void BuildTraps(StageState state)
        {
            ClearTraps();

            if (_root == null)
            {
                GameObject obj = new GameObject("_FireTrapRoot");
                obj.transform.SetParent(transform, false);
                _root = obj.transform;
            }

            foreach (EntityState e in state.Entities)
            {
                if (e.Kind != EntityKind.FireTrap) continue;
                if (!e.Has<FireTrapData>()) continue;

                FireTrapData data = e.Get<FireTrapData>();

                _traps[e.Id] = new TrapState
                {
                    EntityId = e.Id,
                    Position = e.Position,
                    Facing = e.Facing,
                    FireInterval = data.FireInterval,
                    FireDuration = data.FireDuration,
                    Range = data.Range,
                    Timer = 0f,
                    IsActive = false,
                    ActiveTimer = 0f,
                    FireVisuals = new List<GameObject>(data.Range)
                };
            }
        }

        private void Update()
        {
            if (stageManager == null || stageManager.CurrentState == null) return;
            if (!stageManager.CurrentState.IsUpdatable()) return;

            StageState state = stageManager.CurrentState;
            float dt = Time.deltaTime;

            foreach (var kvp in _traps)
            {
                TrapState trap = kvp.Value;

                if (!trap.IsActive)
                {
                    // 대기 중: 타이머 틱
                    trap.Timer += dt;
                    if (trap.Timer >= trap.FireInterval)
                    {
                        trap.Timer = 0f;
                        ActivateFire(state, trap);
                    }
                }
                else
                {
                    // 불 활성 중: 데미지 체크 + 지속 시간 관리
                    trap.ActiveTimer += dt;
                    CheckDamage(state, trap);

                    if (trap.ActiveTimer >= trap.FireDuration)
                    {
                        DeactivateFire(trap);
                    }
                }
            }
        }

        // 불 활성화

        private void ActivateFire(StageState state, TrapState trap)
        {
            trap.IsActive = true;
            trap.ActiveTimer = 0f;

            // 불 범위 계산 (차단 오브젝트 고려)
            List<GridPos> fireCells = CalcFireCells(state, trap);

            // 비주얼 생성
            Sprite[] frames = GetFrames(trap.Facing);
            if (frames == null || frames.Length == 0) return;

            for (int i = 0; i < fireCells.Count; i++)
            {
                GameObject obj = new GameObject($"Fire_{trap.EntityId}_{i}");
                obj.transform.SetParent(_root, false);
                obj.transform.position = fireCells[i].ToWorld(1f);

                SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
                sr.sprite = frames[0];
                sr.sortingOrder = sortingOrder;
                trap.FireVisuals.Add(obj);
            }

            // 애니메이션 시작
            if (trap.AnimCoroutine != null) StopCoroutine(trap.AnimCoroutine);
            trap.AnimCoroutine = StartCoroutine(PlayFireAnim(trap, frames));

            // 즉시 데미지 체크
            CheckDamage(state, trap);
        }

        private void DeactivateFire(TrapState trap)
        {
            trap.IsActive = false;
            trap.ActiveTimer = 0f;

            if (trap.AnimCoroutine != null)
            {
                StopCoroutine(trap.AnimCoroutine);
                trap.AnimCoroutine = null;
            }

            // 비주얼 제거
            for (int i = 0; i < trap.FireVisuals.Count; i++)
            {
                if (trap.FireVisuals[i] != null)
                    Destroy(trap.FireVisuals[i]);
            }
            trap.FireVisuals.Clear();
        }

        // 불 범위 계산

        private List<GridPos> CalcFireCells(StageState state, TrapState trap)
        {
            List<GridPos> cells = new List<GridPos>(trap.Range);
            GridPos current = trap.Position;

            for (int i = 0; i < trap.Range; i++)
            {
                // 첫 번째 셀은 발사대 본체 위치
                if (i > 0)
                    current = current.Move(trap.Facing);

                // 맵 밖이면 중단
                if (!state.IsInside(current)) break;

                // 벽이면 중단
                CellData cell = state.GetCell(current);
                if (cell.HasWall) break;

                cells.Add(current);

                // 차단 오브젝트 체크 (발사대 본체 위치는 스킵)
                if (i > 0 && HasFireBlocker(state, current))
                    break;
            }

            return cells;
        }

        // 불을 차단하는 오브젝트가 있는지 (일반 상자, P1 상자, 철 상자)
        // 부서지는 상자는 차단하지 않음 (불에 파괴됨)
        private bool HasFireBlocker(StageState state, GridPos pos)
        {
            foreach (EntityState e in state.Entities)
            {
                if (!e.IsAlive) continue;
                if (e.Position.X != pos.X || e.Position.Y != pos.Y) continue;

                if (e.IsBox && e.Has<BoxData>() && !e.Has<BreakableData>())
                {
                    BoxType bt = e.Get<BoxData>().Ownership;
                    // 일반(Shared), P1전용, 철 상자 → 차단
                    if (bt == BoxType.Shared || bt == BoxType.Player1Only || bt == BoxType.Iron)
                        return true;
                }
            }
            return false;
        }

        // 데미지 체크

        private void CheckDamage(StageState state, TrapState trap)
        {
            List<GridPos> fireCells = CalcFireCells(state, trap);
            bool viewDirty = false;

            for (int i = 0; i < fireCells.Count; i++)
            {
                GridPos pos = fireCells[i];
                // 발사대 본체 위치(i=0)는 데미지 스킵
                if (i == 0) continue;

                List<int> toDamage = null;
                foreach (EntityState e in state.Entities)
                {
                    if (!e.IsAlive) continue;
                    if (e.Position.X != pos.X || e.Position.Y != pos.Y) continue;

                    // 플레이어 → 즉사
                    if (e.IsPlayer)
                    {
                        state.KillEntity(e.Id);
                        state.MarkGameOver();
                        viewDirty = true;
                        continue;
                    }

                    // 부서지는 상자 / 얼음 상자 → 파괴
                    if (e.IsBox && e.Has<BoxData>())
                    {
                        BoxType bt = e.Get<BoxData>().Ownership;
                        if (bt == BoxType.Ice)
                        {
                            if (toDamage == null) toDamage = new List<int>(2);
                            toDamage.Add(e.Id);
                        }
                    }

                    // 부서지는 상자 (BreakableBox) 체크
                    if (e.IsBox && e.Has<BreakableData>())
                    {
                        if (toDamage == null) toDamage = new List<int>(2);
                        toDamage.Add(e.Id);
                    }
                }

                if (toDamage != null)
                {
                    for (int j = 0; j < toDamage.Count; j++)
                    {
                        state.RemoveEntity(toDamage[j]);
                        viewDirty = true;
                    }
                }
            }

            if (viewDirty)
                state.SetViewDirty();
        }

        // 애니메이션

        private IEnumerator PlayFireAnim(TrapState trap, Sprite[] frames)
        {
            if (frames.Length <= 1) yield break;

            int totalFrames = frames.Length;
            float halfDuration = trap.FireDuration * 0.5f;

            // 전반: 불 성장 (순방향)
            float elapsed = 0f;
            while (elapsed < halfDuration && trap.IsActive)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                int frameIndex = Mathf.Clamp(
                    Mathf.FloorToInt(t * (totalFrames - 1)), 0, totalFrames - 1);

                SetFireSprites(trap, frames[frameIndex]);
                yield return null;
            }

            // 후반: 불 소멸 (역방향)
            elapsed = 0f;
            while (elapsed < halfDuration && trap.IsActive)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                int frameIndex = Mathf.Clamp(
                    Mathf.FloorToInt((1f - t) * (totalFrames - 1)), 0, totalFrames - 1);

                SetFireSprites(trap, frames[frameIndex]);
                yield return null;
            }

            trap.AnimCoroutine = null;
        }

        private void SetFireSprites(TrapState trap, Sprite sprite)
        {
            for (int i = 0; i < trap.FireVisuals.Count; i++)
            {
                if (trap.FireVisuals[i] == null) continue;
                SpriteRenderer sr = trap.FireVisuals[i].GetComponent<SpriteRenderer>();
                if (sr != null) sr.sprite = sprite;
            }
        }

        private Sprite[] GetFrames(Direction facing)
        {
            switch (facing)
            {
                case Direction.Up:    return framesUp;
                case Direction.Down:  return framesDown;
                case Direction.Left:  return framesLeft;
                case Direction.Right: return framesRight;
                default:              return framesUp;
            }
        }

        private void ClearTraps()
        {
            StopAllCoroutines();
            foreach (var kvp in _traps)
            {
                TrapState trap = kvp.Value;
                for (int i = 0; i < trap.FireVisuals.Count; i++)
                {
                    if (trap.FireVisuals[i] != null)
                        Destroy(trap.FireVisuals[i]);
                }
            }
            _traps.Clear();
        }
    }
}