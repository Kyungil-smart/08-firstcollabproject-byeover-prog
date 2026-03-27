using UnityEngine;

namespace MyGame2.Stage
{
    // 엔티티 설정 템플릿 (ScriptableObject).
    // 인스펙터에서 엔티티의 종류, 속성, 컴포넌트를 조합할 수 있다.
    // 런타임에 CreateEntity()로 EntityState를 생성한다.
   
    [CreateAssetMenu(
        fileName = "NewEntityConfig",
        menuName = "Stage/Entity Config",
        order = 0)]
    public class EntityConfigSO : ScriptableObject
    {
        [Header("기본 속성")]
        [Tooltip("엔티티 종류")]
        public EntityKind kind = EntityKind.None;

        [Tooltip("이 엔티티가 셀을 점유하는가? (이동 차단)")]
        public bool isBlocking = true;

        [Tooltip("이 엔티티가 카메라 시야를 차단하는가?")]
        public bool blocksCameraSight = false;

        [Tooltip("접촉 시 플레이어를 죽이는가?")]
        public bool isLethal = false;
        
        // 컴포넌트 설정 (체크한 것만 EntityState에 부착)

        [Header("Player 컴포넌트")]
        [Tooltip("체크하면 PlayerData 부착")]
        public bool usePlayerData;

        [Tooltip("플레이어 슬롯 (1 또는 2)")]
        public int playerSlot = 1;

        [Header("Box 컴포넌트")]
        [Tooltip("체크하면 BoxData 부착")]
        public bool useBoxData;

        [Tooltip("상자 소유권")]
        public BoxType boxOwnership = BoxType.Shared;

        [Header("얼음 미끄러짐 컴포넌트")]
        [Tooltip("체크하면 IceSlideData 부착 — 밀었을 때 벽/오브젝트에 닿을 때까지 미끄러짐")]
        public bool useIceSlide;

        [Header("Camera 컴포넌트")]
        [Tooltip("체크하면 CameraData 부착")]
        public bool useCameraData;

        [Tooltip("감지 패턴")]
        public CameraType cameraPattern = CameraType.LineShort;

        [Tooltip("반시계방향 회전")]
        public bool reverseRotation;

        [Header("Patrol 컴포넌트")]
        [Tooltip("체크하면 PatrolData 부착 (웨이포인트는 Stage Config에서 런타임 주입)")]
        public bool usePatrolData;

        [Header("View 연결")]
        [Tooltip("이 엔티티의 프리팹 (GridEntityView 부착된 것)")]
        public GridEntityView viewPrefab;
        
        // 런타임 변환
        // SO 설정으로부터 EntityState를 생성한다.
        // position, facing은 맵 로드 시 결정되므로 파라미터로 받는다.
      
        public EntityState CreateEntity(GridPos position, Direction facing)
        {
            // 기본 팩토리로 생성
            EntityState entity;

            switch (kind)
            {
                case EntityKind.Player:
                    entity = EntityState.CreatePlayer(position, facing, playerSlot);
                    break;

                case EntityKind.Box:
                    entity = EntityState.CreateBox(position, boxOwnership);
                    break;

                case EntityKind.CameraEnemy:
                    entity = EntityState.CreateCamera(position, facing,
                        cameraPattern, reverseRotation);
                    break;

                case EntityKind.RobotEnemy:
                    entity = EntityState.CreateRobot(position, facing);
                    break;

                case EntityKind.AnimalEnemy:
                    entity = EntityState.CreateAnimal(position, facing);
                    break;

                default:
                    // 커스텀 엔티티: 기본값으로 생성 후 컴포넌트 부착
                    entity = CreateCustom(position, facing);
                    break;
            }

            // SO에서 지정한 공통 속성 덮어쓰기
            entity.IsBlocking = isBlocking;
            entity.BlocksCameraSight = blocksCameraSight;

            // 추가 컴포넌트 부착 (체크된 것만)
            if (usePlayerData && !entity.Has<PlayerData>())
                entity.Set(new PlayerData(playerSlot));

            if (useBoxData && !entity.Has<BoxData>())
                entity.Set(new BoxData(boxOwnership));

            if (useIceSlide && !entity.Has<IceSlideData>())
                entity.Set(new IceSlideData(false, Direction.None));

            if (useCameraData && !entity.Has<CameraData>())
                entity.Set(new CameraData(cameraPattern, reverseRotation));

            if (usePatrolData && !entity.Has<PatrolData>())
                entity.Set(new PatrolData(null));

            return entity;
        }

        // Kind가 None이거나 새로운 타입일 때 사용
        private EntityState CreateCustom(GridPos position, Direction facing)
        {
            // EntityState 생성은 private 생성자라서 기존 팩토리 중 하나를 빌려야 함
            // AnimalEnemy를 기반으로 생성 후 Kind를 덮어쓴다
            var entity = EntityState.CreateAnimal(position, facing);
            entity.Kind = kind;
            return entity;
        }
    }
}