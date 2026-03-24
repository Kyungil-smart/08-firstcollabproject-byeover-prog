using UnityEngine;

namespace MyGame2.Stage
{
    // 스테이지1 전용 설정.
    // 맵 로드 후 로봇 경로를 웨이포인트로 주입한다.
    //
    // [Hierarchy] 빈 오브젝트에 부착
    // [인스펙터] Stage Manager → StageManager 드래그
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
        }

        // 로봇 순찰 경로 (웨이포인트).
        //
        // 맵 빈 통로: col 4, col 9가 세로로 비어있음.
        //
        // 경로 (시계방향 직사각형):
        //   (7,4) → (9,4) → (9,9) → (4,9) → (4,4) → (7,4) 루프
        //
        //   (4,4)────────(9,4)
        //     │    (7,4)=R  │
        //     │  col4통로   │ col9통로
        //     │             │
        //   (4,9)────────(9,9)
        //
        // 상자에 막히면 자동으로 역방향(반시계) 전환.
        //
        private void ConfigureRobotPatrols()
        {
            StageState state = stageManager.CurrentState;
            if (state == null) return;

            for (int i = 0; i < state.RobotIds.Count; i++)
            {
                if (!state.TryGetEntity(state.RobotIds[i], out EntityState robot))
                    continue;

                robot.Patrol = new PatrolData(new GridPos[]
                {
                    new GridPos(7, 4),  // 시작점 (R 위치)
                    new GridPos(9, 4),  // 우상단
                    new GridPos(9, 9),  // 우하단 (col 9 통로)
                    new GridPos(4, 9),  // 좌하단
                    new GridPos(4, 4),  // 좌상단 (col 4 통로)
                });

                state.SetFacing(robot.Id, Direction.Right);
            }
        }
    }
}