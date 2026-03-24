using UnityEngine;

namespace MyGame2.Stage
{
    public sealed class StagePrefabRegistry : MonoBehaviour
    {
        [Header("플레이어")]
        [SerializeField] private GridEntityView player1Prefab;
        [SerializeField] private GridEntityView player2Prefab;

        [Header("상자")]
        [Tooltip("녹색 공용 상자")]
        [SerializeField] private GridEntityView boxSharedPrefab;
        [Tooltip("노란색 P1 전용 상자")]
        [SerializeField] private GridEntityView boxPlayer1Prefab;
        [Tooltip("주황색 P2 전용 상자")]
        [SerializeField] private GridEntityView boxPlayer2Prefab;
        [Tooltip("빨간색 철 상자")]
        [SerializeField] private GridEntityView boxIronPrefab;

        [Header("카메라")]
        [SerializeField] private GridEntityView cameraEnemyPrefab;

        [Header("적")]
        [SerializeField] private GridEntityView robotEnemyPrefab;
        [SerializeField] private GridEntityView animalEnemyPrefab;

        public GridEntityView GetPrefab(EntityState entity)
        {
            switch (entity.Kind)
            {
                case EntityKind.Player:
                    return entity.Get<PlayerData>().Slot == 1 ? player1Prefab : player2Prefab;

                case EntityKind.Box:
                    switch (entity.Get<BoxData>().Ownership)
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