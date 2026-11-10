using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bunker.Simulation
{
    // Shortest walk between two grid cells around blocked cells (trees, limits). Soldiers never move diagonally, so steps
    // are the four neighbours; among equally short walks the one with fewest turns wins, so the route reads as clean "L"s.
    public static class RoutePlanner
    {
        static readonly Vector3Int[] Steps = { Vector3Int.right, Vector3Int.up, Vector3Int.left, Vector3Int.down };
        // Far below one step: it only orders walks of the same length
        const float TurnCost = 0.001f;

        // Cells from start to goal, both included; empty when the goal can't be reached inside `bounds`.
        // Start and goal are walkable even when blocked (the spawner and the bunker door sit on the edge of the map art)
        public static List<Vector3Int> FindPath(Vector3Int start, Vector3Int goal, Func<Vector3Int, bool> blocked, BoundsInt bounds)
        {
            var path = new List<Vector3Int>();
            if (start == goal)
            {
                path.Add(start);
                return path;
            }

            // A state is a cell plus the direction it was entered from (4 = none, the start)
            var cost = new Dictionary<(Vector3Int, int), float>();
            var cameFrom = new Dictionary<(Vector3Int, int), (Vector3Int, int)>();
            var open = new SortedSet<(float f, int order, Vector3Int cell, int dir)>(
                Comparer<(float f, int order, Vector3Int cell, int dir)>.Create((a, b) =>
                    a.f != b.f ? a.f.CompareTo(b.f) : a.order.CompareTo(b.order)));
            int order = 0;
            var startState = (start, 4);
            cost[startState] = 0f;
            open.Add((Heuristic(start, goal), order++, start, 4));

            while (open.Count > 0)
            {
                var current = open.Min;
                open.Remove(current);
                var state = (current.cell, current.dir);
                float g = cost[state];
                if (current.cell == goal)
                    return Rebuild(state, cameFrom, startState);
                if (current.f > g + Heuristic(current.cell, goal) + 1e-4f)
                    continue; // stale entry

                for (int d = 0; d < Steps.Length; d++)
                {
                    var next = current.cell + Steps[d];
                    if (!bounds.Contains(next)) continue;
                    if (next != goal && blocked(next)) continue;
                    float ng = g + 1f + (current.dir != 4 && current.dir != d ? TurnCost : 0f);
                    var nextState = (next, d);
                    if (cost.TryGetValue(nextState, out float old) && old <= ng) continue;
                    cost[nextState] = ng;
                    cameFrom[nextState] = state;
                    open.Add((ng + Heuristic(next, goal), order++, next, d));
                }
            }
            return path;
        }

        // Only the cells where the walk turns, plus both ends: the route's flags
        public static List<Vector3Int> Corners(IReadOnlyList<Vector3Int> path)
        {
            var corners = new List<Vector3Int>();
            for (int i = 0; i < path.Count; i++)
            {
                bool end = i == 0 || i == path.Count - 1;
                if (end || path[i] - path[i - 1] != path[i + 1] - path[i])
                    corners.Add(path[i]);
            }
            return corners;
        }

        static float Heuristic(Vector3Int a, Vector3Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

        static List<Vector3Int> Rebuild((Vector3Int, int) state, Dictionary<(Vector3Int, int), (Vector3Int, int)> cameFrom, (Vector3Int, int) start)
        {
            var path = new List<Vector3Int> { state.Item1 };
            while (state != start)
            {
                state = cameFrom[state];
                path.Add(state.Item1);
            }
            path.Reverse();
            return path;
        }
    }
}
