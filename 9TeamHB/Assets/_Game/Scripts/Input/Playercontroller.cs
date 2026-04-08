using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MyGame2.Stage
{
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("씬 참조")]
        [SerializeField] private StageManager stageManager;
        [SerializeField] private GameManager gameManager;
        [SerializeField] private StageWarpEffect warpEffect;

        [Header("입력 설정")]
        [SerializeField] private float moveRepeatInterval = 0.2f;
        
        [Header("밀기 딜레이")]
        [SerializeField] private float pushDelay = 0.1f;

        private InputAction _moveUpAction;
        private InputAction _moveLeftAction;
        private InputAction _moveDownAction;
        private InputAction _moveRightAction;
        private InputAction _tagSwitchAction;
        private InputAction _undoAction;

        private readonly Dictionary<Direction, float> _pressedAt = new Dictionary<Direction, float>(4);
        private Direction _lockedDirection = Direction.None;
        private float _lastMoveTime;
        private bool _isUndoHeld;

        private float _lastMoveKeyLeaveTime;
        private float _lastUndoKeyLeaveTime;

        private const float _keyInputDelayTime = 0.2f;

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
            if (stageManager == null || stageManager.CurrentState == null) return;
            if (!IsPlayable()) return;
            if (stageManager.CurrentState.IsUndoProcessing) return;

            if (_lockedDirection != Direction.None && !_pressedAt.ContainsKey(_lockedDirection))
                _lockedDirection = Direction.None;

            HandleMoveInput();
        }

        private void CreateInputActions()
        {
            _moveUpAction = new InputAction("MoveUp", InputActionType.Button, "<Keyboard>/w");
            _moveLeftAction = new InputAction("MoveLeft", InputActionType.Button, "<Keyboard>/a");
            _moveDownAction = new InputAction("MoveDown", InputActionType.Button, "<Keyboard>/s");
            _moveRightAction = new InputAction("MoveRight", InputActionType.Button, "<Keyboard>/d");
            _tagSwitchAction = new InputAction("TagSwitch", InputActionType.Button, "<Keyboard>/tab");
            _undoAction = new InputAction("Undo", InputActionType.Button, "<Keyboard>/space");
        }

        private void EnableInputActions()
        {
            _moveUpAction.Enable();
            _moveLeftAction.Enable();
            _moveDownAction.Enable();
            _moveRightAction.Enable();
            _tagSwitchAction.Enable();
            _undoAction.Enable();
        }

        private void DisableInputActions()
        {
            _moveUpAction.Disable();
            _moveLeftAction.Disable();
            _moveDownAction.Disable();
            _moveRightAction.Disable();
            _tagSwitchAction.Disable();
            _undoAction.Disable();
        }

        private void DisposeInputActions()
        {
            _moveUpAction?.Dispose();
            _moveLeftAction?.Dispose();
            _moveDownAction?.Dispose();
            _moveRightAction?.Dispose();
            _tagSwitchAction?.Dispose();
            _undoAction?.Dispose();
        }

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
            _undoAction.started += OnUndoStarted;
            _undoAction.canceled += OnUndoCanceled;
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
            _undoAction.started -= OnUndoStarted;
            _undoAction.canceled -= OnUndoCanceled;
        }

        // 이동 콜백

        private void OnMoveUpStarted(InputAction.CallbackContext ctx)
        {
            if (Time.time < _lastUndoKeyLeaveTime + _keyInputDelayTime)
            {
                return;
            }

            _pressedAt[Direction.Up] = Time.time;
        }

        private void OnMoveUpCanceled(InputAction.CallbackContext ctx)
        {
            if (!_pressedAt.ContainsKey(Direction.Up))
            {
                return;
            }

            _pressedAt.Remove(Direction.Up);
            _lastMoveKeyLeaveTime = Time.time;
        }

        private void OnMoveLeftStarted(InputAction.CallbackContext ctx)
        {
            if (Time.time < _lastUndoKeyLeaveTime + _keyInputDelayTime)
            {
                return;
            }

            _pressedAt[Direction.Left] = Time.time;
        }

        private void OnMoveLeftCanceled(InputAction.CallbackContext ctx)
        {
            if (!_pressedAt.ContainsKey(Direction.Left))
            {
                return;
            }

            _pressedAt.Remove(Direction.Left); _lastMoveKeyLeaveTime = Time.time;
        }

        private void OnMoveDownStarted(InputAction.CallbackContext ctx)
        {
            if (Time.time < _lastUndoKeyLeaveTime + _keyInputDelayTime)
            {
                return;
            }

            _pressedAt[Direction.Down] = Time.time;
        }

        private void OnMoveDownCanceled(InputAction.CallbackContext ctx)
        {
            if (!_pressedAt.ContainsKey(Direction.Down))
            {
                return;
            }

            _pressedAt.Remove(Direction.Down); _lastMoveKeyLeaveTime = Time.time;
        }

        private void OnMoveRightStarted(InputAction.CallbackContext ctx)
        {
            if (Time.time < _lastUndoKeyLeaveTime + _keyInputDelayTime)
            {
                return;
            }

            _pressedAt[Direction.Right] = Time.time;
        }

        private void OnMoveRightCanceled(InputAction.CallbackContext ctx)
        {
            if (!_pressedAt.ContainsKey(Direction.Right))
            {
                return;
            }

            _pressedAt.Remove(Direction.Right); _lastMoveKeyLeaveTime = Time.time;
        }


        // 태그: InGameUIManager를 통해 횟수 차감

        private void OnTagSwitchStarted(InputAction.CallbackContext ctx)
        {
            if (stageManager == null || stageManager.CurrentState == null) return;
            if (warpEffect != null && warpEffect.IsWarping) return;

            if (InGameUIManager.Instance != null)
            {
                bool switched = InGameUIManager.Instance.TryTag();
                if (switched) _lastMoveTime = 0f;
            }
        }

        // Undo: InGameUIManager를 통해 시간 차감

        private void OnUndoStarted(InputAction.CallbackContext ctx)
        {
            if (Time.time < _lastMoveKeyLeaveTime + _keyInputDelayTime)
            {
                return;
            }

            if (!IsPlayable()) return;

            if (_lockedDirection != Direction.None) return;

            if (_isUndoHeld) return;
            _isUndoHeld = true;

            if (InGameUIManager.Instance != null)
                InGameUIManager.Instance.OnClickUndoButton();
        }

        private void OnUndoCanceled(InputAction.CallbackContext ctx)
        {
            if (_isUndoHeld)
            {
                _isUndoHeld = false;

                if (InGameUIManager.Instance != null)
                    InGameUIManager.Instance.OnReleaseUndoButton();

                _lastUndoKeyLeaveTime = Time.time;
            }
        }

        // 내부

        private bool IsPlayable()
        {
            if (warpEffect != null && warpEffect.IsWarping) return false;

            if (gameManager != null)
                return gameManager.CurrentState == GameFlowState.Playing;

            return !stageManager.CurrentState.IsGameOver &&
                   !stageManager.CurrentState.IsStageClear;
        }

        private void HandleMoveInput()
        {
            if (_lockedDirection == Direction.None)
            {
                _lockedDirection = PickDirection();
                if (_lockedDirection == Direction.None)
                    return;
            }

            if (Time.time >= _lastMoveTime + moveRepeatInterval)
            {
                _lastMoveTime = Time.time;
                ExecuteTurn(_lockedDirection);
            }
        }

        private void ExecuteTurn(Direction direction)
        {
            _lastMoveTime = Time.time;
            TurnOutcome outcome = stageManager.TryExecuteTurn(direction);

            // 상자를 밀었으면 추가 딜레이
            if (outcome.Executed && outcome.PlayerMove.IsPushAndMove)
            {
                _lastMoveTime = Time.time + pushDelay;
            }

            if (!outcome.Executed && !_pressedAt.ContainsKey(direction))
                _lockedDirection = Direction.None;
        }

        private Direction PickDirection()
        {
            if (_pressedAt.Count == 0) return Direction.None;

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

        private static int GetPriority(Direction direction)
        {
            switch (direction)
            {
                case Direction.Up: return 0;
                case Direction.Left: return 1;
                case Direction.Down: return 2;
                case Direction.Right: return 3;
                default: return 99;
            }
        }
    }
}