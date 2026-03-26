using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MyGame2.Stage
{
    // [입력 규칙]
    // WASD 4방향 이동, 대각선 금지
    // 선입력 우선: 이미 잠긴 방향이 있으면 해당 키를 떼기 전까지 유지
    // 최근 입력 우선: 새 키가 눌리면 그 방향이 우선
    // 동률 시 W > A > S > D 우선순위
    // 0.2초 간격 반복 실행

    public sealed class PlayerController : MonoBehaviour
    {
        [Header("씬 참조")]
        [Tooltip("턴 실행 및 태그 전환 대상")]
        [SerializeField] private StageManager stageManager;

        [Tooltip("게임 흐름 상태 확인용 (null이면 StageState에서 직접 확인)")]
        [SerializeField] private GameManager gameManager;

        [Tooltip("워프 연출 중 입력 차단용")]
        [SerializeField] private StageWarpEffect warpEffect;

        [Header("입력 설정")]
        [Tooltip("이동 반복 간격 (초)")]
        [SerializeField] private float moveRepeatInterval = 0.2f;
        
        // InputAction 정의
        
        private InputAction _moveUpAction;
        private InputAction _moveLeftAction;
        private InputAction _moveDownAction;
        private InputAction _moveRightAction;
        private InputAction _tagSwitchAction;
        
        // 입력 추적 상태
        
        private readonly Dictionary<Direction, float> _pressedAt = new Dictionary<Direction, float>(4);
        private Direction _lockedDirection = Direction.None;
        private float _nextMoveTime;

        private void Awake()
        {
            CreateInputActions();
        }

        private void OnEnable()
        {
            EnableInputActions();
            SubscribeInputCallbacks();
        }

        private void OnDisable()
        {
            UnsubscribeInputCallbacks();
            DisableInputActions();
        }

        private void OnDestroy()
        {
            DisposeInputActions();
        }

        private void Update()
        {
            if (stageManager == null || stageManager.CurrentState == null)
            {
                return;
            }

            if (!IsPlayable())
            {
                return;
            }

            // 잠긴 방향의 키가 떼어졌으면 잠금 해제
            if (_lockedDirection != Direction.None && !_pressedAt.ContainsKey(_lockedDirection))
            {
                _lockedDirection = Direction.None;
            }

            HandleMoveInput();
        }
     
        private void CreateInputActions()
        {
            _moveUpAction = new InputAction("MoveUp", InputActionType.Button,
                "<Keyboard>/w");
            _moveLeftAction = new InputAction("MoveLeft", InputActionType.Button,
                "<Keyboard>/a");
            _moveDownAction = new InputAction("MoveDown", InputActionType.Button,
                "<Keyboard>/s");
            _moveRightAction = new InputAction("MoveRight", InputActionType.Button,
                "<Keyboard>/d");
            _tagSwitchAction = new InputAction("TagSwitch", InputActionType.Button,
                "<Keyboard>/tab");
        }

        private void EnableInputActions()
        {
            _moveUpAction.Enable();
            _moveLeftAction.Enable();
            _moveDownAction.Enable();
            _moveRightAction.Enable();
            _tagSwitchAction.Enable();
        }

        private void DisableInputActions()
        {
            _moveUpAction.Disable();
            _moveLeftAction.Disable();
            _moveDownAction.Disable();
            _moveRightAction.Disable();
            _tagSwitchAction.Disable();
        }

        private void DisposeInputActions()
        {
            _moveUpAction?.Dispose();
            _moveLeftAction?.Dispose();
            _moveDownAction?.Dispose();
            _moveRightAction?.Dispose();
            _tagSwitchAction?.Dispose();
        }
        
        // 콜백 구독 / 해제

        private void SubscribeInputCallbacks()
        {
            _moveUpAction.started += OnMoveUpStarted;
            _moveUpAction.canceled += OnMoveUpCanceled;

            _moveLeftAction.started += OnMoveLeftStarted;
            _moveLeftAction.canceled += OnMoveLeftCanceled;

            _moveDownAction.started += OnMoveDownStarted;
            _moveDownAction.canceled += OnMoveDownCanceled;

            _moveRightAction.started += OnMoveRightStarted;
            _moveRightAction.canceled += OnMoveRightCanceled;

            _tagSwitchAction.started += OnTagSwitchStarted;
        }

        private void UnsubscribeInputCallbacks()
        {
            _moveUpAction.started -= OnMoveUpStarted;
            _moveUpAction.canceled -= OnMoveUpCanceled;

            _moveLeftAction.started -= OnMoveLeftStarted;
            _moveLeftAction.canceled -= OnMoveLeftCanceled;

            _moveDownAction.started -= OnMoveDownStarted;
            _moveDownAction.canceled -= OnMoveDownCanceled;

            _moveRightAction.started -= OnMoveRightStarted;
            _moveRightAction.canceled -= OnMoveRightCanceled;

            _tagSwitchAction.started -= OnTagSwitchStarted;
        }
        
        // InputAction 콜백 (키 누름 / 뗌 추적)

        private void OnMoveUpStarted(InputAction.CallbackContext ctx)
        {
            _pressedAt[Direction.Up] = Time.time;
        }

        private void OnMoveUpCanceled(InputAction.CallbackContext ctx)
        {
            _pressedAt.Remove(Direction.Up);
        }

        private void OnMoveLeftStarted(InputAction.CallbackContext ctx)
        {
            _pressedAt[Direction.Left] = Time.time;
        }

        private void OnMoveLeftCanceled(InputAction.CallbackContext ctx)
        {
            _pressedAt.Remove(Direction.Left);
        }

        private void OnMoveDownStarted(InputAction.CallbackContext ctx)
        {
            _pressedAt[Direction.Down] = Time.time;
        }

        private void OnMoveDownCanceled(InputAction.CallbackContext ctx)
        {
            _pressedAt.Remove(Direction.Down);
        }

        private void OnMoveRightStarted(InputAction.CallbackContext ctx)
        {
            _pressedAt[Direction.Right] = Time.time;
        }

        private void OnMoveRightCanceled(InputAction.CallbackContext ctx)
        {
            _pressedAt.Remove(Direction.Right);
        }

        private void OnTagSwitchStarted(InputAction.CallbackContext ctx)
        {
            if (stageManager == null || stageManager.CurrentState == null)
            {
                return;
            }

            // 워프 중에는 태그 전환도 차단
            if (warpEffect != null && warpEffect.IsWarping)
            {
                return;
            }

            bool switched = stageManager.SwitchActivePlayer();
            if (switched)
            {
                _nextMoveTime = 0f;
            }
        }
        
        private bool IsPlayable()
        {
            // 워프 연출 중에는 입력 차단
            if (warpEffect != null && warpEffect.IsWarping)
            {
                return false;
            }

            if (gameManager != null)
            {
                return gameManager.CurrentState == GameFlowState.Playing;
            }

            return !stageManager.CurrentState.IsGameOver &&
                   !stageManager.CurrentState.IsStageClear;
        }

        private void HandleMoveInput()
        {
            if (_lockedDirection == Direction.None)
            {
                _lockedDirection = PickDirection();
                if (_lockedDirection == Direction.None)
                {
                    return;
                }

                ExecuteTurn(_lockedDirection);
                return;
            }

            if (Time.time >= _nextMoveTime)
            {
                ExecuteTurn(_lockedDirection);
            }
        }

        private void ExecuteTurn(Direction direction)
        {
            TurnOutcome outcome = stageManager.TryExecuteTurn(direction);
            _nextMoveTime = Time.time + moveRepeatInterval;

            // 이동 실패 + 해당 키도 안 눌린 상태면 잠금 해제
            if (!outcome.Executed && !_pressedAt.ContainsKey(direction))
            {
                _lockedDirection = Direction.None;
            }
        }
        
        // 방향 선택 (최근 입력 우선, 동률 시 W>A>S>D)
        
        private Direction PickDirection()
        {
            if (_pressedAt.Count == 0)
            {
                return Direction.None;
            }

            float bestTime = float.MinValue;
            Direction bestDirection = Direction.None;

            foreach (KeyValuePair<Direction, float> pair in _pressedAt)
            {
                if (pair.Value > bestTime)
                {
                    bestTime = pair.Value;
                    bestDirection = pair.Key;
                    continue;
                }

                if (Mathf.Approximately(pair.Value, bestTime) &&
                    GetPriority(pair.Key) < GetPriority(bestDirection))
                {
                    bestDirection = pair.Key;
                }
            }

            return bestDirection;
        }

        // 동률 시 우선순위: W(0) > A(1) > S(2) > D(3)
        private static int GetPriority(Direction direction)
        {
            switch (direction)
            {
                case Direction.Up:    return 0;
                case Direction.Left:  return 1;
                case Direction.Down:  return 2;
                case Direction.Right: return 3;
                default:              return 99;
            }
        }
    }
}