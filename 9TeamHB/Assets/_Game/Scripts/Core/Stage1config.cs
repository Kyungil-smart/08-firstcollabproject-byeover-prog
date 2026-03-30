using UnityEngine;

namespace MyGame2.Stage
{
    // 스테이지1 전용 설정.
    // 맵 로드 후 로봇 경로를 웨이포인트로 주입한다.
    // 텔레포트 타일의 연결지점을 주입한다
  
    public sealed class Stage1Config : MonoBehaviour
    {
        [Header("씬 참조")]
        [SerializeField] private StageManager stageManager;

        private void OnEnable()
        {
            if (stageManager != null)
                stageManager.Events.StageLoaded += OnStageLoaded;
        }

        private void OnDisable()
        {
            if (stageManager != null)
                stageManager.Events.StageLoaded -= OnStageLoaded;
        }

        private void OnStageLoaded(int stageIndex)
        {
            if (stageIndex != 0) return;
            ConfigureRobotPatrols();
            ConfiqureVentPair();
        }

        private void ConfigureRobotPatrols()
        {
            StageState state = stageManager.CurrentState;
            if (state == null) return;

            for (int i = 0; i < state.RobotIds.Count; i++)
            {
                if (!state.TryGetEntity(state.RobotIds[i], out EntityState robot))
                    continue;

                // ECS-lite: Set으로 컴포넌트 교체
                robot.Set(new PatrolData(new GridPos[]
                {
                    new GridPos(7, 4),  // 시작점 (R 위치)
                    new GridPos(9, 4),  // 우상단
                    new GridPos(9, 9),  // 우하단
                    new GridPos(4, 9),  // 좌하단
                    new GridPos(4, 4),  // 좌상단
                }));

                state.SetFacing(robot.Id, Direction.Right);
            }
        }

        // 텔레포트 스팟 타일의 연결을 주입하는 함수
        private void ConfiqureVentPair()
        {
            StageState state = stageManager.CurrentState;
            if (state == null) return;
            // pos 기준으로 셀을 찾고 pair 할당
            // 정의 할때 모든 텔레포트 스팟 위치를 정확히 알고있는 상태로 부여해주어야한다.
            BindPair(new GridPos(1, 4), new GridPos(12, 11), state);
        }

        private void BindPair(GridPos a, GridPos b, StageState state)
        {
            state.SetCellPair(a, b);
        }
    }
}