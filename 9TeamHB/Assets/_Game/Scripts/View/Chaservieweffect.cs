using UnityEngine;

namespace MyGame2.Stage
{
    // 추격 감시자 프리팹에 부착하는 등장/퇴장 연출

    public class ChaserViewEffect : MonoBehaviour
    {
        [Header("연출 설정")]
        [SerializeField] private float spawnDuration = 0.3f;
        [SerializeField] private float despawnDuration = 0.5f;

        private float _timer;
        private bool _isSpawning;
        private bool _isDespawning;
        private Vector3 _originalScale;

        // GridEntityView에서 entityId를 읽기 위해 캐싱
        private GridEntityView _entityView;
        private int _entityId = -1;
        private StageEvents _events;

        private void Awake()
        {
            _originalScale = transform.localScale;
            _entityView = GetComponent<GridEntityView>();
        }

        private void Start()
        {
            // 등장 시작: 스케일 0에서 시작
            transform.localScale = Vector3.zero;
            _isSpawning = true;
            _isDespawning = false;
            _timer = 0f;
        }

        // GridEntityView.Bind() 이후 호출되므로 OnEnable 대신 LateUpdate에서 1회 구독
        private void LateUpdate()
        {
            // 이벤트 구독 (1회)
            if (_events == null && _entityView != null && _entityView.EntityId >= 0)
            {
                _entityId = _entityView.EntityId;

                // StageManager를 씬에서 찾아서 이벤트 구독
                StageManager sm = FindFirstObjectByType<StageManager>();
                if (sm != null && sm.Events != null)
                {
                    _events = sm.Events;
                    _events.EnemyDespawnStarted += OnDespawnStarted;
                }
            }

            // 등장 모션
            if (_isSpawning)
            {
                _timer += Time.deltaTime;
                float t = Mathf.Clamp01(_timer / spawnDuration);
                float scale = t * t * (3f - 2f * t); // smoothstep
                transform.localScale = _originalScale * scale;

                if (t >= 1f)
                {
                    transform.localScale = _originalScale;
                    _isSpawning = false;
                }
            }

            // 퇴장 모션
            if (_isDespawning)
            {
                _timer += Time.deltaTime;
                float t = Mathf.Clamp01(_timer / despawnDuration);

                // 깜빡임 + 축소
                float scale = 1f - t;
                float blink = (Mathf.Sin(t * Mathf.PI * 8f) > 0f) ? 1f : 0.3f;
                transform.localScale = _originalScale * scale * blink;

                if (t >= 1f)
                {
                    transform.localScale = Vector3.zero;
                    _isDespawning = false;
                }
            }
        }

        private void OnDespawnStarted(int entityId)
        {
            if (entityId != _entityId) return;
            if (_isDespawning) return;

            _isDespawning = true;
            _isSpawning = false;
            _timer = 0f;
        }

        private void OnDestroy()
        {
            if (_events != null)
            {
                _events.EnemyDespawnStarted -= OnDespawnStarted;
            }
        }
    }
}