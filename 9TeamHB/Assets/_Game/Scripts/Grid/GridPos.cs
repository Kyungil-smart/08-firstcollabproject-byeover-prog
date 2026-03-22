using System;
using UnityEngine;

namespace MyGame2.Stage
{
    [Serializable]
    public readonly struct GridPos : IEquatable<GridPos>
    {
        public readonly int X;
        public readonly int Y;

        public GridPos(int x, int y)
        {
            X = x;
            Y = y;
        }

        public GridPos Move(Direction direction)
        {
            Vector2Int offset = direction.ToOffset();
            return new GridPos(X + offset.x, Y + offset.y);
        }

        public Vector3 ToWorld(float cellSize)
        {
            return new Vector3(X * cellSize, -Y * cellSize, 0f);
        }

        public bool Equals(GridPos other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is GridPos other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }

        public override string ToString()
        {
            return $"({X}, {Y})";
        }

        public static bool operator ==(GridPos left, GridPos right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GridPos left, GridPos right)
        {
            return !left.Equals(right);
        }
    }
}