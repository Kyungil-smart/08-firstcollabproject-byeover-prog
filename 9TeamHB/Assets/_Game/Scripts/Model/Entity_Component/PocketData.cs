
using System.Collections.Generic;
using UnityEngine;

namespace MyGame2.Stage
{
    public class PocketData : IComponentData
    {
        private EntityState _owner;
        // 추종자 정보
        private readonly KeyFollower _keyPrefab;
        private readonly List<KeyFollower> _keys = new List<KeyFollower>();

        public List<KeyFollower> Keys { get { return _keys; } }

        // 생성자
        public PocketData(EntityState owner, KeyFollower keyPrefab)
        {
            _owner = owner;
            _keyPrefab = keyPrefab;
        }

        // 편의 프로퍼티
        public bool HasKey => _keys.Count > 0;

        public bool TryUseKey()
        {
            if (_keys.Count > 0)
            {
                RemoveKeyFollower();
                return true;
            }
            return false;
        }

        public void PickUp(StageState state, EntityState key)
        {
            var request = new ViewRequest
            {
                Id = _owner.Id,
                Callback = v => AddKeyFollower(v)
            };
            state.Events.RaiseViewRequest(request);


            // 필요한 연출
            //

            state.KillEntity(key.Id);

            // 필요한 연출
            //
        }

        // 키 추가
        public void AddKeyFollower(GridEntityView playerView)
        {
            Debug.Log($"키 추종자 생성 로직 :{playerView.name}");
            // 추종자의 타겟 설정을 위한 삼항 연산자
            Transform target = (_keys.Count == 0) ?
                playerView.transform : _keys[^1].transform;

            KeyFollower key = Object.Instantiate(_keyPrefab);
            key.target = target;
            key.transform.position = target.position;

            _keys.Add(key);

        }

        // 키 사용
        public void RemoveKeyFollower()
        {
            if (_keys.Count == 0) return;

            int index = _keys.Count - 1;
            KeyFollower key = _keys[index];

            _keys.RemoveAt(index);
            if (key) Object.Destroy(key.gameObject);
        }
        // 키 일괄 파괴
        public void ClearKeyFollowers()
        {
            foreach (var k in _keys)
            {
                if (k != null) Object.Destroy(k.gameObject);
            }
            _keys.Clear();
        }

    }
}