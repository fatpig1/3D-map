using System.Collections.Generic;
using UnityEngine;

namespace IndoorNav
{
    public static class AStarPlanner
    {
        public static List<int> Plan(
            IList<Vector3> positions,
            IList<List<int>> adjacency,
            int startIdx,
            int goalIdx)
        {
            if (startIdx < 0 || goalIdx < 0 ||
                startIdx >= positions.Count || goalIdx >= positions.Count)
                return null;
            if (startIdx == goalIdx) return new List<int> { startIdx };

            int n = positions.Count;
            var gScore = new float[n];
            var fScore = new float[n];
            var cameFrom = new int[n];
            var closed = new bool[n];
            for (int i = 0; i < n; i++)
            {
                gScore[i] = float.PositiveInfinity;
                fScore[i] = float.PositiveInfinity;
                cameFrom[i] = -1;
            }

            gScore[startIdx] = 0f;
            fScore[startIdx] = Vector3.Distance(positions[startIdx], positions[goalIdx]);

            var open = new SortedSet<(float f, int idx)>(Comparer<(float f, int idx)>.Create((a, b) =>
            {
                int c = a.f.CompareTo(b.f);
                return c != 0 ? c : a.idx.CompareTo(b.idx);
            }));
            open.Add((fScore[startIdx], startIdx));

            while (open.Count > 0)
            {
                var cur = open.Min;
                open.Remove(cur);
                int u = cur.idx;
                if (u == goalIdx) return Reconstruct(cameFrom, goalIdx);
                if (closed[u]) continue;
                closed[u] = true;

                var neighbors = adjacency[u];
                for (int k = 0; k < neighbors.Count; k++)
                {
                    int v = neighbors[k];
                    if (closed[v]) continue;
                    float tentative = gScore[u] + Vector3.Distance(positions[u], positions[v]);
                    if (tentative >= gScore[v]) continue;
                    cameFrom[v] = u;
                    gScore[v] = tentative;
                    fScore[v] = tentative + Vector3.Distance(positions[v], positions[goalIdx]);
                    open.Add((fScore[v], v));
                }
            }
            return null;
        }

        private static List<int> Reconstruct(int[] cameFrom, int goal)
        {
            var path = new List<int> { goal };
            int cur = goal;
            while (cameFrom[cur] != -1)
            {
                cur = cameFrom[cur];
                path.Add(cur);
            }
            path.Reverse();
            return path;
        }

        public static int NearestNode(IList<Vector3> positions, Vector3 point)
        {
            int best = -1;
            float bestSq = float.PositiveInfinity;
            for (int i = 0; i < positions.Count; i++)
            {
                float d = (positions[i] - point).sqrMagnitude;
                if (d < bestSq) { bestSq = d; best = i; }
            }
            return best;
        }
    }
}
