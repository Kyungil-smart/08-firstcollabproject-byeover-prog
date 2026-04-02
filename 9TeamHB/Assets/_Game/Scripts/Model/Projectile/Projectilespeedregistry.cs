using System.Collections.Generic;

namespace MyGame2.Stage
{
    // 투사체 이동 속도 전달용 정적 레지스트리
    // 발사기가 SpawnEntity 후 등록, 투사체가 OnUpdate에서 읽음

    public static class ProjectileSpeedRegistry
    {
        private static readonly Dictionary<int, float> _speeds = new Dictionary<int, float>();

        public static void Register(int projectileEntityId, float speed)
        {
            _speeds[projectileEntityId] = speed;
        }

        public static float GetSpeed(int projectileEntityId, float defaultSpeed = 0.1f)
        {
            if (_speeds.TryGetValue(projectileEntityId, out float speed))
            {
                _speeds.Remove(projectileEntityId);
                return speed;
            }
            return defaultSpeed;
        }

        public static void Clear()
        {
            _speeds.Clear();
        }
    }
}