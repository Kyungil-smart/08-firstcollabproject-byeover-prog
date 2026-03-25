using UnityEngine;

namespace MyGame2.Stage
{
    public class GridEntityView : MonoBehaviour
    {
        [Header("시각 설정")]
        [Tooltip("Facing 방향으로 스프라이트를 회전할지 여부")]
        [SerializeField] private bool rotateWithFacing = true;

        [Tooltip("활성 플레이어 표시용 자식 오브젝트 (없으면 비워둠)")]
        [SerializeField] private GameObject selectedMarker;

        [Header("슬라이딩 이동")]
        [Tooltip("이동 보간 속도 (높을수록 빠르게 도착)")]
        [SerializeField] private float slideSpeed = 12f;

        [Tooltip("목표 위치와의 거리가 이 값 이하면 즉시 도착 처리")]
        [SerializeField] private float snapThreshold = 0.01f;
        
        private Vector3 _targetPosition;
        private Quaternion _targetRotation;
        private bool _isSliding;

        public int EntityId { get; private set; }
        public EntityKind Kind { get; private set; }

        // 현재 슬라이딩 중인가?
        public bool IsSliding { get { return _isSliding; } }
        
        public void Bind(EntityState entity, float cellSize)
        {
            EntityId = entity.Id;
            Kind = entity.Kind;
            
            Vector3 worldPos = entity.Position.ToWorld(cellSize);
            transform.position = worldPos;
            _targetPosition = worldPos;

            if (rotateWithFacing)
            {
                Quaternion rot = Quaternion.Euler(0f, 0f, entity.Facing.ToZRotation());
                transform.rotation = rot;
                _targetRotation = rot;
            }

            gameObject.SetActive(entity.IsAlive);
            _isSliding = false;
        }

        // 상태 동기화. 목표 위치를 갱신하면 자동으로 슬라이딩 시작.
        public void Sync(EntityState entity, float cellSize)
        {
            // 사망 처리
            if (!entity.IsAlive)
            {
                gameObject.SetActive(false);
                _isSliding = false;
                return;
            }

            gameObject.SetActive(true);

            // 목표 위치 갱신
            Vector3 newTarget = entity.Position.ToWorld(cellSize);
            if ((_targetPosition - newTarget).sqrMagnitude > snapThreshold * snapThreshold)
            {
                _targetPosition = newTarget;
                _isSliding = true;
            }

            // 회전 목표 갱신
            if (rotateWithFacing)
            {
                _targetRotation = Quaternion.Euler(0f, 0f, entity.Facing.ToZRotation());
            }
        }

        // 선택 마커 표시/숨김.
        public void SyncSelection(bool isSelected)
        {
            if (selectedMarker != null)
            {
                selectedMarker.SetActive(isSelected);
            }
        }
        

        private void Update()
        {
            if (!_isSliding && !rotateWithFacing)
            {
                return;
            }

            float dt = Time.deltaTime;

            // 위치 보간 (슬라이딩)
            if (_isSliding)
            {
                transform.position = Vector3.Lerp(
                    transform.position, _targetPosition, slideSpeed * dt);

                // 도착 판정
                if ((transform.position - _targetPosition).sqrMagnitude <=
                    snapThreshold * snapThreshold)
                {
                    transform.position = _targetPosition;
                    _isSliding = false;
                }
            }

            // 회전 보간
            if (rotateWithFacing)
            {
                transform.rotation = Quaternion.Lerp(
                    transform.rotation, _targetRotation, slideSpeed * dt);
            }
        }
    }
}