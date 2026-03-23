using System;
using System.Collections.Generic;

namespace MyGame2.Stage
{
    // 동물형 적 엔티티.
    // 이동 방향 우선순위: 1차축 → 2차축 → 현재 방향 → 시계방향 회전.
    public sealed class AnimalEnemy
    {
        // 방향 우선순위 버퍼 (재사용)
        private readonly List<Direction> _directionBuffer = new List<Direction>(4);

        public MoveResult ResolveTurn(StageState state, int animalId, MovementRule movementRule)
        {
            if (!state.TryGetEntity(animalId, out EntityState animal) || !animal.IsAlive)
            {
                return MoveResult.Blocked(animalId, new GridPos(0, 0), new GridPos(0, 0), MoveBlockReason.DeadEntity);
            }

            BuildDirectionPriority(state, animal, _directionBuffer);

            MoveResult lastBlockedResult = MoveResult.Blocked(
                animalId, animal.Position, animal.Position, MoveBlockReason.BlockedByEntity);

            for (int i = 0; i < _directionBuffer.Count; i++)
            {
                Direction direction = _directionBuffer[i];
                MoveResult result = movementRule.TryMove(state, animalId, direction);

                if (result.Succeeded)
                {
                    state.SetFacing(animalId, direction);
                    state.MoveEntity(animalId, result.To);
                    return result;
                }

                if (result.IsContactKill)
                {
                    state.SetFacing(animalId, direction);
                    return result;
                }

                lastBlockedResult = result;
            }

            return lastBlockedResult;
        }
        
        // 방향 우선순위 리스트를 외부 버퍼에 채운다.
        
        private void BuildDirectionPriority(StageState state, EntityState animal, List<Direction> outBuffer)
        {
            outBuffer.Clear();

            GridPos target = state.GetNearestLivingPlayerPosition(animal.Position);

            int deltaX = target.X - animal.Position.X;
            int deltaY = target.Y - animal.Position.Y;

            Direction horizontal = deltaX < 0 ? Direction.Left : Direction.Right;
            Direction vertical = deltaY < 0 ? Direction.Up : Direction.Down;

            // 주축 → 보조축
            if (Math.Abs(deltaX) >= Math.Abs(deltaY))
            {
                AddIfUnique(outBuffer, deltaX == 0 ? Direction.None : horizontal);
                AddIfUnique(outBuffer, deltaY == 0 ? Direction.None : vertical);
            }
            else
            {
                AddIfUnique(outBuffer, deltaY == 0 ? Direction.None : vertical);
                AddIfUnique(outBuffer, deltaX == 0 ? Direction.None : horizontal);
            }

            // 현재 방향 → 시계방향 순회
            AddIfUnique(outBuffer, animal.Facing);

            Direction rotated = animal.Facing;
            for (int i = 0; i < 3; i++)
            {
                rotated = rotated.RotateClockwise();
                AddIfUnique(outBuffer, rotated);
            }

            // 모든 방향이 None이었을 경우 기본 방향
            if (outBuffer.Count == 0)
            {
                outBuffer.Add(Direction.Up);
                outBuffer.Add(Direction.Left);
                outBuffer.Add(Direction.Down);
                outBuffer.Add(Direction.Right);
            }
        }

        private static void AddIfUnique(List<Direction> directions, Direction direction)
        {
            if (direction == Direction.None)
            {
                return;
            }

            if (!directions.Contains(direction))
            {
                directions.Add(direction);
            }
        }
    }
}
