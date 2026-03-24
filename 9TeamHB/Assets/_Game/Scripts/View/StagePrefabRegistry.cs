using UnityEngine;

namespace MyGame2.Stage
{
    public sealed class StagePrefabRegistry : MonoBehaviour
    {
        [Header("플레이어")]
        [SerializeField] private GridEntityView player1Prefab;
        [SerializeField] private GridEntityView player2Prefab;

        [Header("상자")]
        [Tooltip("녹색 공용 상자 (양쪽 다 밀기 가능)")]
        [SerializeField] private GridEntityView boxSharedPrefab;

        [Tooltip("노란색 P1 전용 상자")]
        [SerializeField] private GridEntityView boxPlayer1Prefab;

        [Tooltip("주황색 P2 전용 상자")]
        [SerializeField] private GridEntityView boxPlayer2Prefab;

        [Tooltip("빨간색 철 상자 (아무도 못 밂)")]
        [SerializeField] private GridEntityView boxIronPrefab;

        [Header("카메라")]
        [Tooltip("카메라 프리팹")]
        [SerializeField] private GridEntityView cameraEnemyPrefab;

        [Header("적 (다른 스테이지용)")]
        [SerializeField] private GridEntityView robotEnemyPrefab;
        [SerializeField] private GridEntityView animalEnemyPrefab;

        // 엔티티 상태에 따라 적절한 프리팹을 반환한다.
        public GridEntityView GetPrefab(EntityState entity)
        {
            switch (entity.Kind)
            {
                case EntityKind.Player:
                    return entity.Player.Slot == 1 ? player1Prefab : player2Prefab;

                case EntityKind.Box:
                    switch (entity.Box.Ownership)
                    {
                        case BoxType.Shared:      return boxSharedPrefab;
                        case BoxType.Player1Only:  return boxPlayer1Prefab;
                        case BoxType.Player2Only:  return boxPlayer2Prefab;
                        case BoxType.Iron:         return boxIronPrefab;
                        default:                   return boxSharedPrefab;
                    }

                case EntityKind.CameraEnemy:  return cameraEnemyPrefab;
                case EntityKind.RobotEnemy:   return robotEnemyPrefab;
                case EntityKind.AnimalEnemy:  return animalEnemyPrefab;
                default:                      return null;
            }
        }
    }
}