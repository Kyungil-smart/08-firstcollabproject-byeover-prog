using UnityEngine;


   // 우선은 반복문 Switch로 짜보긴 했습니다만 세환님이 더 좋은 알고리즘이 있으시면
   // 이 스크립트 통째로 수정 하셔도 됩니다.

namespace MyGame2.Stage
{
    public enum Direction
    {
        None = 0,
        Up = 1,
        Left = 2,
        Down = 3,
        Right = 4
    }

    public static class DirectionExtensions
    {
        public static Vector2Int ToOffset(this Direction direction)
        {
            switch (direction)
            {
                case Direction.Up:
                    return new Vector2Int(0, -1);
                case Direction.Left:
                    return new Vector2Int(-1, 0);
                case Direction.Down:
                    return new Vector2Int(0, 1);
                case Direction.Right:
                    return new Vector2Int(1, 0);
                default:
                    return Vector2Int.zero;
            }
        }

        public static Direction Opposite(this Direction direction)
        {
            switch (direction)
            {
                case Direction.Up:
                    return Direction.Down;
                case Direction.Left:
                    return Direction.Right;
                case Direction.Down:
                    return Direction.Up;
                case Direction.Right:
                    return Direction.Left;
                default:
                    return Direction.None;
            }
        }

        public static Direction RotateClockwise(this Direction direction)
        {
            switch (direction)
            {
                case Direction.Up:
                    return Direction.Right;
                case Direction.Right:
                    return Direction.Down;
                case Direction.Down:
                    return Direction.Left;
                case Direction.Left:
                    return Direction.Up;
                default:
                    return Direction.None;
            }
        }

        public static float ToZRotation(this Direction direction)
        {
            switch (direction)
            {
                case Direction.Up:
                    return 0f;
                case Direction.Left:
                    return 90f;
                case Direction.Down:
                    return 180f;
                case Direction.Right:
                    return 270f;
                default:
                    return 0f;
            }
        }
    }
}
