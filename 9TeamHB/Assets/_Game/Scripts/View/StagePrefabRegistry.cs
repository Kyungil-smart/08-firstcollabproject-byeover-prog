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
            return entity.Prefab;
        }
    }
}