using System;
using System.Collections.Generic;

namespace MyGame2.Stage
{
    // 한 턴의 실행 결과를 불변 객체로 캡슐화
    [Serializable]
    public sealed class TurnOutcome
    {
        public bool Executed { get; private set; }
        public MoveResult PlayerMove { get; private set; }
        public IReadOnlyList<MoveResult> RobotMoves { get; private set; }
        public IReadOnlyList<MoveResult> AnimalMoves { get; private set; }
        public IReadOnlyList<int> CameraDetectedPlayerIds { get; private set; }
        public bool GameOver { get; private set; }
        public bool StageClear { get; private set; }

        private TurnOutcome(
            bool executed,
            MoveResult playerMove,
            IReadOnlyList<MoveResult> robotMoves,
            IReadOnlyList<MoveResult> animalMoves,
            IReadOnlyList<int> cameraDetectedPlayerIds,
            bool gameOver,
            bool stageClear)
        {
            Executed = executed;
            PlayerMove = playerMove;
            RobotMoves = robotMoves;
            AnimalMoves = animalMoves;
            CameraDetectedPlayerIds = cameraDetectedPlayerIds;
            GameOver = gameOver;
            StageClear = stageClear;
        }

        
        // 재사용 가능한 싱글턴
       
        private static readonly MoveResult _dummyMove = MoveResult.Blocked(
            StageState.InvalidEntityId, new GridPos(0, 0), new GridPos(0, 0), MoveBlockReason.DeadEntity);

        private static readonly TurnOutcome _none = new TurnOutcome(
            false, _dummyMove,
            Array.Empty<MoveResult>(), Array.Empty<MoveResult>(), Array.Empty<int>(),
            false, false);

        // 아직 턴이 실행되지 않은 초기 상태.
        public static TurnOutcome None()
        {
            return _none;
        }

        // 이동 실패 등으로 턴이 무시되었을 때.
        public static TurnOutcome Ignored(MoveResult playerMove)
        {
            return new TurnOutcome(
                false,
                playerMove,
                Array.Empty<MoveResult>(),
                Array.Empty<MoveResult>(),
                Array.Empty<int>(),
                false,
                false);
        }

        // 턴이 정상 실행되었을 때.
        public static TurnOutcome Create(
            MoveResult playerMove,
            IReadOnlyList<MoveResult> robotMoves,
            IReadOnlyList<MoveResult> animalMoves,
            IReadOnlyList<int> cameraDetectedPlayerIds,
            bool gameOver,
            bool stageClear)
        {
            return new TurnOutcome(
                true,
                playerMove,
                robotMoves,
                animalMoves,
                cameraDetectedPlayerIds,
                gameOver,
                stageClear);
        }
    }
}
