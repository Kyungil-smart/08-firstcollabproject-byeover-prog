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
        [Tooltip("Left일 때 스프라이트 X 반전 (AD 애니메이션이 Right 기준일 때 체크)")]
        [SerializeField] private bool flipXOnLeft = true;
        [Tooltip("FlipX를 적용할 SpriteRenderer (비우면 자식에서 자동 탐색)")]
        [SerializeField] private SpriteRenderer targetSpriteRenderer;

        private static readonly int AnimDirection = Animator.StringToHash("Direction");
        private static readonly int AnimIsMoving = Animator.StringToHash("IsMoving");
        private static readonly int AnimIsDead = Animator.StringToHash("IsDead");

        private Vector3 _targetPosition;
        private Quaternion _targetRotation;
        private bool _isSliding;
        private Animator _animator;
        private SpriteRenderer _spriteRenderer;
        private Direction _lastFacing;

        // Animator 파라미터 존재 여부 캐싱
        private bool _hasDirection;
        private bool _hasIsMoving;
        private bool _hasIsDead;

        public int EntityId { get; private set; }
        public EntityKind Kind { get; private set; }
        public bool IsSliding { get { return _isSliding; } }

        private void Awake()
        {
            _animator = targetAnimator != null
                ? targetAnimator
                : GetComponentInChildren<Animator>();

            _spriteRenderer = targetSpriteRenderer != null
                ? targetSpriteRenderer
                : GetComponentInChildren<SpriteRenderer>();
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

            if (_spriteRenderer == null)
            {
                _spriteRenderer = targetSpriteRenderer != null
                    ? targetSpriteRenderer
                    : GetComponentInChildren<SpriteRenderer>();
            }

            // Animator 파라미터 캐싱 (Bind 시 1회)
            CacheAnimParams();

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
        }

        public void Sync(EntityState entity, float cellSize)
        {
            if (!entity.IsAlive)
            {
                // 사망 애니메이션 트리거
                if (useDirectionAnim && _animator != null && _hasIsDead)
                    _animator.SetTrigger(AnimIsDead);

                gameObject.SetActive(false);
                _isSliding = false;
                UpdateMovingAnim(false);
                return;
            }

            gameObject.SetActive(true);

            Vector3 newTarget = entity.Position.ToWorld(cellSize);

            // 텔레포트
            if (entity.CanTeleport && entity.Get<Teleportable>().IsTeleporting)
            {
                _targetPosition = newTarget;
                transform.position = _targetPosition;
                entity.Get<Teleportable>().IsTeleporting = false;
            }

            // 일반 이동
            if ((_targetPosition - newTarget).sqrMagnitude > snapThreshold * snapThreshold)
            {
                _targetPosition = newTarget;
                _isSliding = true;
                UpdateMovingAnim(true);
            }

            if (rotateWithFacing)
            {
                _targetRotation = Quaternion.Euler(0f, 0f, entity.Facing.ToZRotation());
            }

            if (entity.Facing != _lastFacing)
            {
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
        
        // Animator Controller에 실제로 존재하는 파라미터만 캐싱.
        // Bind 시 1회 호출되어, 파라미터가 없는 엔티티에서는 SetBool/SetFloat/SetTrigger를 스킵.
        private void CacheAnimParams()
        {
            _hasDirection = false;
            _hasIsMoving = false;
            _hasIsDead = false;

            if (_animator == null || _animator.runtimeAnimatorController == null)
                return;

            foreach (var p in _animator.parameters)
            {
                if (p.nameHash == AnimIsMoving)       _hasIsMoving = true;
                else if (p.nameHash == AnimDirection)  _hasDirection = true;
                else if (p.nameHash == AnimIsDead)     _hasIsDead = true;
            }
        }

        private void UpdateDirectionAnim(Direction facing)
        {
            if (!useDirectionAnim || _animator == null) return;

            // Direction 파라미터가 있을 때만 Blend Tree 값 설정
            if (_hasDirection)
            {
                // Blend Tree용 Float 파라미터
                // 0=Down(S), 1=Up(W), 2=Left(AD), 3=Right(AD)
                float dirValue;
                switch (facing)
                {
                    case Direction.Down:  dirValue = 0f; break;
                    case Direction.Up:    dirValue = 1f; break;
                    case Direction.Left:  dirValue = 2f; break;
                    case Direction.Right: dirValue = 2f; break; // Left와 같은 AD 애니메이션
                    default:              dirValue = 0f; break;
                }

                _animator.SetFloat(AnimDirection, dirValue);
            }

            // 좌우 반전 처리 (파라미터와 무관하므로 항상 실행)
            if (flipXOnLeft && _spriteRenderer != null)
            {
                if (facing == Direction.Left)
                    _spriteRenderer.flipX = true;
                else if (facing == Direction.Right)
                    _spriteRenderer.flipX = false;
            }
        }

        private void UpdateMovingAnim(bool isMoving)
        {
            if (!useDirectionAnim || _animator == null || !_hasIsMoving) return;
            _animator.SetBool(AnimIsMoving, isMoving);
        }

        public void OnRequestView(ViewRequest request)
        {
            request.Callback(this);
        }
    }
}