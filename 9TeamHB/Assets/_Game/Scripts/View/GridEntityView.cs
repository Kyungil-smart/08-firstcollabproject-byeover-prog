using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

namespace MyGame2.Stage
{
    public class GridEntityView : MonoBehaviour
    {
        [Header("시각 설정")]
        [SerializeField] private bool rotateWithFacing = true;
        [SerializeField] private GameObject selectedMarker;

        [Header("슬라이딩 이동")]
        [SerializeField] private float slideSpeed = 12f;
        [SerializeField] private float snapThreshold = 0.01f;

        [Header("방향 애니메이션")]
        [SerializeField] private bool useDirectionAnim = false;
        [SerializeField] private Animator targetAnimator;

        private static readonly int AnimDirection = Animator.StringToHash("Direction");
        private static readonly int AnimIsMoving = Animator.StringToHash("IsMoving");

        private Vector3 _targetPosition;
        private Quaternion _targetRotation;
        private bool _isSliding;
        private Animator _animator;
        private Direction _lastFacing;

        public int EntityId { get; private set; }
        public EntityKind Kind { get; private set; }
        public bool IsSliding { get { return _isSliding; } }

        private void Awake()
        {
            _animator = targetAnimator != null
                ? targetAnimator
                : GetComponentInChildren<Animator>();

            // ── 디버그: Animator 연결 확인 ──
            // if (useDirectionAnim)
            // {
            //     if (_animator == null)
            //         Debug.LogError($"[GridEntityView] {name}: useDirectionAnim이 켜져있는데 Animator를 찾을 수 없음!", this);
            //     else
            //         Debug.Log($"[GridEntityView] {name}: Animator 연결됨 → {_animator.name}", this);
            // }
        }

        public void Bind(EntityState entity, float cellSize)
        {
            EntityId = entity.Id;
            Kind = entity.Kind;

            if (_animator == null)
            {
                _animator = targetAnimator != null
                    ? targetAnimator
                    : GetComponentInChildren<Animator>();
            }

            Vector3 worldPos = entity.Position.ToWorld(cellSize);
            transform.position = worldPos;
            _targetPosition = worldPos;

            if (rotateWithFacing)
            {
                Quaternion rot = Quaternion.Euler(0f, 0f, entity.Facing.ToZRotation());
                transform.rotation = rot;
                _targetRotation = rot;
            }

            _lastFacing = entity.Facing;
            UpdateDirectionAnim(entity.Facing);
            UpdateMovingAnim(false);

            gameObject.SetActive(entity.IsAlive);
            _isSliding = false;

            // ── 디버그: Bind 확인 ──
            Debug.Log($"[GridEntityView] Bind: {name}, Kind={entity.Kind}, Facing={entity.Facing}, Pos={entity.Position}", this);
        }

        public void Sync(EntityState entity, float cellSize)
        {
            if (!entity.IsAlive)
            {
                gameObject.SetActive(false);
                _isSliding = false;
                UpdateMovingAnim(false);
                return;
            }

            gameObject.SetActive(true);

            Vector3 newTarget = entity.Position.ToWorld(cellSize);
            if ((_targetPosition - newTarget).sqrMagnitude > snapThreshold * snapThreshold)
            {
                _targetPosition = newTarget;
                _isSliding = true;
                UpdateMovingAnim(true);

                // ── 디버그: 이동 시작 ──
                //Debug.Log($"[GridEntityView] {name}: 이동 시작 → {entity.Position}, Facing={entity.Facing}", this);
            }

            if (rotateWithFacing)
            {
                _targetRotation = Quaternion.Euler(0f, 0f, entity.Facing.ToZRotation());
            }

            if (entity.Facing != _lastFacing)
            {
                // ── 디버그: 방향 변경 ──
                //Debug.Log($"[GridEntityView] {name}: 방향 변경 {_lastFacing} → {entity.Facing}", this);

                _lastFacing = entity.Facing;
                UpdateDirectionAnim(entity.Facing);
            }
        }

        public void SyncSelection(bool isSelected)
        {
            if (selectedMarker != null)
                selectedMarker.SetActive(isSelected);
        }

        private void Update()
        {
            if (!_isSliding && !rotateWithFacing)
                return;

            float dt = Time.deltaTime;

            if (_isSliding)
            {
                transform.position = Vector3.Lerp(
                    transform.position, _targetPosition, slideSpeed * dt);

                if ((transform.position - _targetPosition).sqrMagnitude <=
                    snapThreshold * snapThreshold)
                {
                    transform.position = _targetPosition;
                    _isSliding = false;
                    UpdateMovingAnim(false);
                }
            }

            if (rotateWithFacing)
            {
                transform.rotation = Quaternion.Lerp(
                    transform.rotation, _targetRotation, slideSpeed * dt);
            }
        }

        private void UpdateDirectionAnim(Direction facing)
        {
            if (!useDirectionAnim || _animator == null) return;

            int dirValue;
            switch (facing)
            {
                case Direction.Up:    dirValue = 1; break;
                case Direction.Left:  dirValue = 2; break;
                case Direction.Right: dirValue = 3; break;
                case Direction.Down:  dirValue = 0; break;
                default:              dirValue = 0; break;
            }

            _animator.SetInteger(AnimDirection, dirValue);

            // ── 디버그: Direction 파라미터 설정 ──
            //Debug.Log($"[GridEntityView] {name}: Animator.Direction = {dirValue} ({facing})", this);
        }

        private void UpdateMovingAnim(bool isMoving)
        {
            if (!useDirectionAnim || _animator == null) return;
            _animator.SetBool(AnimIsMoving, isMoving);

            // ── 디버그: IsMoving 파라미터 설정 ──
            //Debug.Log($"[GridEntityView] {name}: Animator.IsMoving = {isMoving}", this);
        }

        public void OnRequestView(ViewRequest request)
        {
            request.Callback(this);
        }
    }
}