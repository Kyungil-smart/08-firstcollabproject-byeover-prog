using System.Collections.Generic;

namespace MyGame2.Stage
{
    // A* 경로탐색 유틸리티.
    
    public sealed class GridPathfinder
    {
        private struct Node
        {
            public GridPos Pos;
            public int G;
            public int H;
            public int F { get { return G + H; } }
            public GridPos Parent;
            public bool HasParent;
        }

        private static readonly Direction[] SearchDirs =
        {
            Direction.Up, Direction.Right, Direction.Down, Direction.Left
        };
        
        public List<GridPos> FindPath(StageState state, GridPos from, GridPos to,
            bool ignoreOccupants = false, int maxSearch = 512)
        {
            if (from.Equals(to))
                return new List<GridPos>();

            // Open/Closed를 Dictionary로 관리 (소규모 그리드에 적합)
            var open = new Dictionary<long, Node>(64);
            var closed = new Dictionary<long, Node>(64);

            Node start = new Node
            {
                Pos = from,
                G = 0,
                H = Heuristic(from, to),
                HasParent = false
            };
            open[PosKey(from)] = start;

            int searched = 0;

            while (open.Count > 0 && searched < maxSearch)
            {
                searched++;

                // F값이 가장 작은 노드 선택
                Node current = default;
                bool first = true;
                foreach (var kv in open)
                {
                    if (first || kv.Value.F < current.F ||
                        (kv.Value.F == current.F && kv.Value.H < current.H))
                    {
                        current = kv.Value;
                        first = false;
                    }
                }

                long currentKey = PosKey(current.Pos);
                open.Remove(currentKey);
                closed[currentKey] = current;

                // 목표 도달
                if (current.Pos.Equals(to))
                    return ReconstructPath(closed, from, to);

                // 인접 4방향 탐색
                for (int d = 0; d < SearchDirs.Length; d++)
                {
                    GridPos neighbor = current.Pos.Move(SearchDirs[d]);
                    long neighborKey = PosKey(neighbor);

                    if (closed.ContainsKey(neighborKey))
                        continue;

                    if (!IsWalkable(state, neighbor, to, ignoreOccupants))
                        continue;

                    int tentativeG = current.G + 1;

                    if (open.TryGetValue(neighborKey, out Node existing))
                    {
                        if (tentativeG >= existing.G)
                            continue;
                    }

                    Node next = new Node
                    {
                        Pos = neighbor,
                        G = tentativeG,
                        H = Heuristic(neighbor, to),
                        Parent = current.Pos,
                        HasParent = true
                    };
                    open[neighborKey] = next;
                }
            }

            return null; // 경로 없음
        }

        // from에서 to 방향으로 한 칸만 이동할 때의 다음 칸을 반환.
        // A* 전체 경로 중 첫 번째 칸만 필요할 때 사용.
        public GridPos? GetNextStep(StageState state, GridPos from, GridPos to,
            bool ignoreOccupants = false)
        {
            List<GridPos> path = FindPath(state, from, to, ignoreOccupants);
            if (path == null || path.Count == 0) return null;
            return path[0];
        }

        private bool IsWalkable(StageState state, GridPos pos, GridPos target,
            bool ignoreOccupants)
        {
            if (!state.IsInside(pos)) return false;

            CellData cell = state.GetCell(pos);
            if (cell.IsBlocked || cell.HasBush) return false;

            // 목표 칸은 점유되어 있어도 도달 가능
            if (pos.Equals(target)) return true;

            if (!ignoreOccupants && cell.IsOccupied)
                return false;

            return true;
        }

        // 맨해튼 거리 휴리스틱
        private static int Heuristic(GridPos a, GridPos b)
        {
            int dx = a.X > b.X ? a.X - b.X : b.X - a.X;
            int dy = a.Y > b.Y ? a.Y - b.Y : b.Y - a.Y;
            return dx + dy;
        }
        
        private static long PosKey(GridPos pos)
        {
            return ((long)pos.Y << 16) | (long)(pos.X & 0xFFFF);
        }

        private List<GridPos> ReconstructPath(Dictionary<long, Node> closed,
            GridPos from, GridPos to)
        {
            var path = new List<GridPos>(16);
            GridPos current = to;

            while (!current.Equals(from))
            {
                path.Add(current);
                long key = PosKey(current);
                if (!closed.TryGetValue(key, out Node node) || !node.HasParent)
                    break;
                current = node.Parent;
            }

            path.Reverse();
            return path;
        }
    }
}