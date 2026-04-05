using UnityEngine;

namespace MyGame2.Stage
{
    // 스테이지1 전용 설정.
    // 맵 로드 후 로봇 경로를 웨이포인트로 주입한다.
    // 텔레포트 타일의 연결지점을 주입한다

    public sealed class StageConfig : MonoBehaviour
    {
        [Header("씬 참조")] [SerializeField] private StageManager stageManager;


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
            ConfigureInject(stageIndex);
        }

        private void ConfigureInject(int stageIndex)
        {
            switch (stageIndex)
            {
                case 0: ConfigureStage7(); break;
                case 7: ConfigureTutorial7(); break;
                case 9: ConfigureStage2(); break;
                case 11: ConfigureStage4(); break;
                case 14: ConfigureStage7(); break;
                

            }
        }

        private void ConfigureTutorial7()
        {
            //로봇 경로 주입
            ConfigureRobotPatrols(tutorial7);
        }

        private void ConfigureStage2()
        {
            // 벤트 페어링
            StageState state = stageManager.CurrentState;
            if (state == null) return;
            state.SetCellPair(new GridPos(9,6), new GridPos(13,5));
            state.SetCellPair(new GridPos(9, 13), new GridPos(10, 14));
            state.SetCellPair(new GridPos(17, 12), new GridPos(16, 14));
        }

        private void ConfigureStage3()
        {
            // 로봇 경로 주입
        }

        private void ConfigureStage4()
        {
            // 로봇 경로 주입
            ConfigureRobotPatrols(stage4);
        }

        private void ConfigureStage7()
        {
            // 로봇 경로 주입
            ConfigureRobotPatrols(stage7Robot);
            ConfigureSummonerPatrols(stage7Summon);
        }

        private void ConfigureRobotPatrols(PatrolData[] patrols)
        {
            StageState state = stageManager.CurrentState;
            if (state == null) return;
            if (patrols.Length != state.RobotIds.Count) return;
            

            for (int i = 0; i < state.RobotIds.Count; i++)
            {
                if (!state.TryGetEntity(state.RobotIds[i], out EntityState robot))
                    continue;

                // ECS-lite: Set으로 컴포넌트 교체
                robot.Set(patrols[i]);

                state.SetFacing(robot.Id, Direction.Right);
            }
        }

        private void ConfigureSummonerPatrols(PatrolData[] patrols)
        {
            StageState state = stageManager.CurrentState;
            if (state == null) return;
            if (patrols.Length != state.SummonerIds.Count) return;
            
            for (int i = 0; i < state.SummonerIds.Count; i++)
            {
                if (!state.TryGetEntity(state.SummonerIds[i], out EntityState robot))
                    continue;

                // ECS-lite: Set으로 컴포넌트 교체
                robot.Set(patrols[i]);

                state.SetFacing(robot.Id, Direction.Right);
            }
        }

        //----------------------------정찰 경로 ----------------
        private PatrolData[] tutorial7 = new PatrolData[]
        {
            new PatrolData(new GridPos[]
            {
                new GridPos(1, 1),
                new GridPos(5, 1),
                new GridPos(5, 3),
                new GridPos(1, 3)
            }),
            new PatrolData(new GridPos[]
            {
                new GridPos(8, 4),
                new GridPos(4, 4),
                new GridPos(4, 2),
                new GridPos(8, 2)
            }),
            new PatrolData(new GridPos[]
            {
                new GridPos(4, 5),
                new GridPos(8, 5),
                new GridPos(8, 7),
                new GridPos(4, 7)
            }),
            new PatrolData(new GridPos[]
            {
                new GridPos(5, 8),
                new GridPos(1, 8),
                new GridPos(1, 6),
                new GridPos(5, 6)
            })
        };
        
        private PatrolData[] stage3 = new PatrolData[]
        {
            new PatrolData(new GridPos[]
            {
                new GridPos(16, 2),
                new GridPos(18, 2),
                new GridPos(18, 4),
                new GridPos(16, 4)
            })
        };

        private PatrolData[] stage4 = new PatrolData[]
        {
            new PatrolData(new GridPos[]
            {
                new GridPos(2, 9),
                new GridPos(5, 9),
                new GridPos(5, 10),
                new GridPos(2, 10)
            })
        };
        
        private PatrolData[] stage7Robot = new PatrolData[]
        {
            
            new PatrolData(new GridPos[]
            {
                new GridPos(10, 10),
                new GridPos(14, 10),
                new GridPos(14, 12),
                new GridPos(10, 12)
            }),
            new PatrolData(new GridPos[]
            {
                new GridPos(16, 10),
                new GridPos(20, 10),
                new GridPos(20, 12),
                new GridPos(16, 12)
            })
        };
        private PatrolData[] stage7Summon = new PatrolData[]
        {
            new PatrolData(new GridPos[]
            {
                new GridPos(11, 3),
                new GridPos(21, 3),
                new GridPos(21, 5),
                new GridPos(11, 5)
            }),
            new PatrolData(new GridPos[]
            {
                new GridPos(3, 6),
                new GridPos(7, 6),
                new GridPos(7, 15),
                new GridPos(3, 15)
            }),
            new PatrolData(new GridPos[]
            {
                new GridPos(2, 9),
                new GridPos(5, 9),
                new GridPos(2, 10),
                new GridPos(5, 10)
            }),
            new PatrolData(new GridPos[]
            {
                new GridPos(3, 17),
                new GridPos(10, 17),
                new GridPos(10, 20),
                new GridPos(3, 20)
            }),
            new PatrolData(new GridPos[]
            {
                new GridPos(20, 17),
                new GridPos(27, 17),
                new GridPos(27, 20),
                new GridPos(20, 20)
            })
        };
    }
}