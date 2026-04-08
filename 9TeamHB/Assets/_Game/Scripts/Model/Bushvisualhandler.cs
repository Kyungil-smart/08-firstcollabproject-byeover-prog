using System.Collections.Generic;
using UnityEngine;

namespace MyGame2.Stage
{
    // 플레이어가 부쉬에 진입하면 부쉬와 플레이어 모두 반투명 처리.
    // 부쉬에서 나오면 원래 알파로 복구.
    
    public sealed class BushVisualHandler : MonoBehaviour
    {
        [Header("씬 참조")]
        [SerializeField] private StageManager stageManager;

        [Header("투명도 설정")]
        [Tooltip("부쉬 안에 있을 때 알파값 (0~1)")]
        [SerializeField] private float hiddenAlpha = 0.4f;

        [Tooltip("알파 전환 속도")]
        [SerializeField] private float fadeSpeed = 8f;

        // 플레이어 ID -> 현재 부쉬에 있는지 여부
        private readonly Dictionary<int, bool> _playerInBush = new Dictionary<int, bool>();

        // 플레이어 ID -> 현재 점유 중인 부쉬 위치 (복구 시 다른 플레이어 부쉬 보호용)
        private readonly Dictionary<int, GridPos> _playerBushPos = new Dictionary<int, GridPos>();

        // 엔티티 ID -> 목표 알파
        private readonly Dictionary<int, float> _targetAlpha = new Dictionary<int, float>();

        // 부쉬 엔티티 View 캐시 (부쉬 위에 플레이어가 있을 때 같이 투명해질 대상)
        private readonly Dictionary<GridPos, GridEntityView> _bushViews = new Dictionary<GridPos, GridEntityView>();

        private void OnEnable()
        {
            if (stageManager != null)
            {
                stageManager.Events.TurnExecuted += OnTurnExecuted;
                stageManager.Events.StageLoaded += OnStageLoaded;
            }
        }

        private void OnDisable()
        {
            if (stageManager != null)
            {
                stageManager.Events.TurnExecuted -= OnTurnExecuted;
                stageManager.Events.StageLoaded -= OnStageLoaded;
            }
        }

        private void OnStageLoaded(int stageIndex)
        {
            _playerInBush.Clear();
            _playerBushPos.Clear();
            _targetAlpha.Clear();
            _bushViews.Clear();

            // 부쉬 엔티티의 View를 캐싱
            StageState state = stageManager.CurrentState;
            if (state == null) return;

            GridEntityView[] allViews = FindObjectsByType<GridEntityView>(FindObjectsSortMode.None);
            foreach (GridEntityView view in allViews)
            {
                if (view.Kind == EntityKind.Bush && state.TryGetEntity(view.EntityId, out EntityState bush))
                {
                    _bushViews[bush.Position] = view;
                }
            }
        }

        private void OnTurnExecuted(TurnOutcome outcome)
        {
            CheckBushOccupancy();
        }

        private void Update()
        {
            // 태그/Undo 등 어떤 상태 변경이든 즉시 반영하기 위해 매 프레임 체크
            CheckBushOccupancy();

            // 부드러운 알파 전환
            GridEntityView[] allViews = FindObjectsByType<GridEntityView>(FindObjectsSortMode.None);
            foreach (GridEntityView view in allViews)
            {
                if (!_targetAlpha.ContainsKey(view.EntityId)) continue;

                float target = _targetAlpha[view.EntityId];
                SpriteRenderer[] renderers = view.GetComponentsInChildren<SpriteRenderer>();
                foreach (SpriteRenderer sr in renderers)
                {
                    Color c = sr.color;
                    c.a = Mathf.MoveTowards(c.a, target, fadeSpeed * Time.deltaTime);
                    sr.color = c;
                }
            }
        }

        private void CheckBushOccupancy()
        {
            StageState state = stageManager.CurrentState;
            if (state == null) return;

            for (int i = 0; i < state.PlayerIds.Count; i++)
            {
                int playerId = state.PlayerIds[i];
                if (!state.TryGetEntity(playerId, out EntityState player)) continue;
                if (!player.IsAlive) continue;

                bool isInBush = state.HasBush(player.Position);
                bool wasInBush = _playerInBush.ContainsKey(playerId) && _playerInBush[playerId];

                if (isInBush && !wasInBush)
                {
                    // 부쉬 진입
                    _targetAlpha[playerId] = hiddenAlpha;
                    _playerBushPos[playerId] = player.Position;

                    if (_bushViews.TryGetValue(player.Position, out GridEntityView bushView))
                        _targetAlpha[bushView.EntityId] = hiddenAlpha;
                }
                else if (!isInBush && wasInBush)
                {
                    // 부쉬 이탈 — 이전 부쉬만 복구, 다른 플레이어가 점유 중이면 보호
                    _targetAlpha[playerId] = 1f;

                    if (_playerBushPos.TryGetValue(playerId, out GridPos oldBushPos))
                    {
                        if (!IsOccupiedByOtherPlayer(playerId, oldBushPos))
                        {
                            if (_bushViews.TryGetValue(oldBushPos, out GridEntityView oldBush))
                                _targetAlpha[oldBush.EntityId] = 1f;
                        }
                        _playerBushPos.Remove(playerId);
                    }
                }
                else if (isInBush && wasInBush)
                {
                    // 부쉬 -> 부쉬: 이전 부쉬 복구(보호 체크), 새 부쉬 반투명
                    if (_playerBushPos.TryGetValue(playerId, out GridPos oldBushPos)
                        && !oldBushPos.Equals(player.Position))
                    {
                        if (!IsOccupiedByOtherPlayer(playerId, oldBushPos))
                        {
                            if (_bushViews.TryGetValue(oldBushPos, out GridEntityView oldBush))
                                _targetAlpha[oldBush.EntityId] = 1f;
                        }
                    }

                    _playerBushPos[playerId] = player.Position;

                    if (_bushViews.TryGetValue(player.Position, out GridEntityView newBush))
                        _targetAlpha[newBush.EntityId] = hiddenAlpha;
                }

                _playerInBush[playerId] = isInBush;
            }
        }
        
        // 지정 위치의 부쉬에 다른 플레이어가 점유 중인지 확인.
        // 다른 플레이어가 있으면 true → 부쉬 알파를 복구하면 안 됨.

        private bool IsOccupiedByOtherPlayer(int excludePlayerId, GridPos bushPos)
        {
            foreach (var kvp in _playerBushPos)
            {
                if (kvp.Key != excludePlayerId && kvp.Value.Equals(bushPos))
                    return true;
            }
            return false;
        }
    }
}