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
                case 6: ConfigureStage2(); break;
                case 8: ConfigureTutorial7(); break;
                case 9: ConfigureStage3(); break;
                case 10: ConfigureStage4(); break;
                case 11: ConfigureStage5(); break;
                case 12: ConfigureStage6(); break;
                case 14: ConfigureStage7(); break;
                case 15: ConfigureStage8(); break;
            }
        }

        private void ConfigureTutorial7()
        {
            //경로 주입
            ConfigureRobotPatrols(tutorial7);
        }

        private void ConfigureStage2()
        {
            // 텔레포트 스팟 연결
            StageState state = stageManager.CurrentState;
            if (state == null) return;
            state.SetCellDuo(new GridPos(9,8), new GridPos(13,5));
            state.SetCellDuo(new GridPos(9, 13), new GridPos(10, 14));
            state.SetCellDuo(new GridPos(17, 12), new GridPos(16, 14));
        }
        private void ConfigureStage3()
        {
            // 경로 주입
            ConfigureRobotPatrols(stage3);
        }

        private void ConfigureStage4()
        {
            // 경로 주입
            ConfigureRobotPatrols(stage4Robot);
            ConfigureSummonerPatrols(stage4Summon);
        }

        private void ConfigureStage5()
        {
            // 경로 주입
            ConfigureRobotPatrols(stage5Robot);
        }

        private void ConfigureStage6()
        {
            // 경로 주입
            ConfigureRobotPatrols(stage6Robot);
            ConfigureSummonerPatrols(stage6Summon);
        }

        private void ConfigureStage7()
        {
            // 경로 주입
            ConfigureRobotPatrols(stage7Robot);
            ConfigureAnimalPatrols(stage7Summon);
        }

        private void ConfigureStage8()
        {
            // 경로 주입
            ConfigureRobotPatrols(stage8Robot);
            ConfigureSummonerPatrols(stage8Summon);
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
                if (!state.TryGetEntity(state.SummonerIds[i], out EntityState summoner))
                    continue;

                // ECS-lite: Set으로 컴포넌트 교체
                summoner.Set(patrols[i]);

                state.SetFacing(summoner.Id, Direction.Right);
            }
        }
        
        private void ConfigureAnimalPatrols(PatrolData[] patrols)
        {
            StageState state = stageManager.CurrentState;
            if (state == null) return;
            if (patrols.Length != state.AnimalIds.Count) return;
            
            for (int i = 0; i < state.AnimalIds.Count; i++)
            {
                if (!state.TryGetEntity(state.AnimalIds[i], out EntityState animal))
                    continue;

                // ECS-lite: Set으로 컴포넌트 교체
                animal.Set(patrols[i]);

                state.SetFacing(animal.Id, Direction.Right);
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

        private PatrolData[] stage4Robot = new PatrolData[]
        {
            new PatrolData(new GridPos[]
            {
                new GridPos(2, 9),
                new GridPos(5, 9),
                new GridPos(5, 10),
                new GridPos(2, 10)
            })
        };
        
        private PatrolData[] stage4Summon = new PatrolData[]
        {
            new PatrolData(new GridPos[]
            {
                new GridPos(2, 2),
                new GridPos(5, 2),
                new GridPos(5, 6),
                new GridPos(2, 6)
            })
        };
        
        private PatrolData[] stage5Robot = new PatrolData[]
        {
            new PatrolData(new GridPos[]
            {
                new GridPos(7, 10),
                new GridPos(9, 10),
                new GridPos(9, 12),
                new GridPos(7, 12)
            })
        };
        
        private PatrolData[] stage6Robot = new PatrolData[]
        {
            new PatrolData(new GridPos[]
            {
                new GridPos(10, 6),
                new GridPos(13, 6),
                new GridPos(13, 8),
                new GridPos(10, 8)
            })
        };
        
        private PatrolData[] stage6Summon = new PatrolData[]
        {
            new PatrolData(new GridPos[]
            {
                new GridPos(5, 1),
                new GridPos(8, 1),
                new GridPos(8, 4),
                new GridPos(5, 4)
            }),
            new PatrolData(new GridPos[]
            {
                new GridPos(7, 10),
                new GridPos(10, 10),
                new GridPos(10, 13),
                new GridPos(7, 13)
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
                new GridPos(23, 6),
                new GridPos(27, 6),
                new GridPos(27, 15),
                new GridPos(23, 15)
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

        private PatrolData[] stage8Robot = new PatrolData[]
        {
            new PatrolData(new GridPos[]
            {
                new GridPos(2, 11),
                new GridPos(4, 11),
                new GridPos(4, 13),
                new GridPos(2, 13)
            }),
            new PatrolData(new GridPos[]
            {
                new GridPos(6,16),
                new GridPos(8,16),
                new GridPos(8,18),
                new GridPos(6,18)
            }),
            new PatrolData(new GridPos[]
            {
                new GridPos(15, 16),
                new GridPos(19, 16),
                new GridPos(19, 19),
                new GridPos(15, 19)
            })
        };

        private PatrolData[] stage8Summon = new PatrolData[]
        {
            new PatrolData(new GridPos[]
            {
                new GridPos(20, 4),
                new GridPos(23, 4),
                new GridPos(23, 7),
                new GridPos(20, 7)
            }),
            new PatrolData(new GridPos[]
            {
                new GridPos(2, 16),
                new GridPos(4, 16),
                new GridPos(4, 18),
                new GridPos(2, 18)
            }),
            new PatrolData(new GridPos[]
            {
                new GridPos(21, 16),
                new GridPos(25, 16),
                new GridPos(25, 19),
                new GridPos(21, 19)
            })
        };
    }
}