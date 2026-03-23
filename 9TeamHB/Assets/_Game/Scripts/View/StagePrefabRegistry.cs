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

        [Tooltip("주황색 P1 전용 상자")]
        [SerializeField] private GridEntityView boxPlayer1Prefab;

        [Tooltip("노란색 P2 전용 상자")]
        [SerializeField] private GridEntityView boxPlayer2Prefab;

        [Header("카메라")]
        [Tooltip("카메라 프리팹 (4종류 공용, 색상/크기로 구분하거나 별도 분리)")]
        [SerializeField] private GridEntityView cameraEnemyPrefab;

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
                        default:                   return boxSharedPrefab;
                    }

                case EntityKind.CameraEnemy:
                    return cameraEnemyPrefab;

                default:
                    return null;
            }
        }
    }
}