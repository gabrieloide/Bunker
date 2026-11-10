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

        // How a route winds, the way tower defense routes do: long, with room for towers between its legs
        [Serializable]
        public struct MeanderSettings
        {
            [Tooltip("Route length as a multiple of the shortest walk; 1 keeps the shortest walk")]
            [Range(1f, 4f)] public float lengthFactor;
            [Tooltip("Cells every straight leg runs at least: no one-cell steps")]
            [Min(1)] public int minSegment;
            [Tooltip("Free cells kept between two legs that pass by each other: room for towers")]
            [Min(1)] public int gap;
            [Tooltip("Free cells kept between a detour and the decoration or limits")]
            [Min(0)] public int margin;
            [Tooltip("Same seed, same route; change it for another variant")]
            public int seed;

            public static MeanderSettings Default => new MeanderSettings { lengthFactor = 2f, minSegment = 3, gap = 2, margin = 1, seed = 1 };
        }

        // Lengthens `path` towards lengthFactor times its length by pushing U-shaped detours out of its straight legs into free
        // ground, again and again (detours grow detours, which gives the S shapes). A detour only needs its own three legs free,
        // so it can wrap around a clump of trees. Deterministic for a seed. Returns a new list; `path` is left alone.
        public static List<Vector3Int> Meander(IReadOnlyList<Vector3Int> path, Func<Vector3Int, bool> blocked, BoundsInt bounds, MeanderSettings settings)
        {
            var route = new List<Vector3Int>(path);
            if (route.Count < 2 || settings.lengthFactor <= 1f) return route;

            int minSegment = Mathf.Max(1, settings.minSegment);
            int gap = Mathf.Max(1, settings.gap);
            int margin = Mathf.Max(0, settings.margin);
            // Two legs of one detour face each other, so its width must leave the gap between them too
            int minWidth = Mathf.Max(minSegment, gap + 1);
            int target = Mathf.RoundToInt((route.Count - 1) * settings.lengthFactor);
            var rng = new System.Random(settings.seed);

            int width = bounds.size.x, height = bounds.size.y;
            // Ground a detour may use: inside the bounds, not blocked, and `margin` cells clear of anything blocked
            var free = new bool[width * height];
            var wall = new bool[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    wall[y * width + x] = blocked(new Vector3Int(bounds.xMin + x, bounds.yMin + y, bounds.zMin));
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    bool clear = true;
                    for (int dy = -margin; dy <= margin && clear; dy++)
                        for (int dx = -margin; dx <= margin && clear; dx++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (nx < 0 || ny < 0 || nx >= width || ny >= height || wall[ny * width + nx]) clear = false;
                        }
                    free[y * width + x] = clear;
                }

            var indexAt = new int[width * height];
            var candidates = new List<(int a, int b, Vector3Int side, int depth)>();

            for (int iteration = 0; iteration < 64; iteration++)
            {
                int remaining = target - (route.Count - 1);
                if (remaining < 2 * minSegment) break;

                for (int i = 0; i < indexAt.Length; i++) indexAt[i] = -1;
                for (int i = 0; i < route.Count; i++)
                {
                    int x = route[i].x - bounds.xMin, y = route[i].y - bounds.yMin;
                    if (x >= 0 && y >= 0 && x < width && y < height) indexAt[y * width + x] = i;
                }

                // A cell a detour between route[a] and route[b] may take: free ground, and no other part of the route within the gap
                // (the stretch being replaced and the corners where the detour leaves and rejoins don't count)
                bool Usable(Vector3Int cell, int a, int b)
                {
                    int x = cell.x - bounds.xMin, y = cell.y - bounds.yMin;
                    if (x < 0 || y < 0 || x >= width || y >= height || !free[y * width + x]) return false;
                    for (int dy = -gap; dy <= gap; dy++)
                        for (int dx = -gap; dx <= gap; dx++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
                            int index = indexAt[ny * width + nx];
                            if (index >= 0 && (index < a - gap || index > b + gap)) return false;
                        }
                    return true;
                }

                candidates.Clear();
                int maxDepth = remaining / 2;
                int runStart = 0;
                for (int i = 1; i <= route.Count; i++)
                {
                    // A straight leg ends where the direction changes (or the route does)
                    bool legEnds = i == route.Count || (i >= 2 && route[i] - route[i - 1] != route[i - 1] - route[i - 2]);
                    if (!legEnds) continue;
                    int runEnd = i - 1;
                    var along = route[runStart + 1] - route[runStart];
                    var perpendicular = new Vector3Int(-along.y, along.x, 0);

                    // The detour leaves at `a` and rejoins at `b`, keeping a full leg of the run on both sides
                    for (int a = runStart + minSegment; a + minWidth <= runEnd - minSegment; a++)
                        for (int w = minWidth; a + w <= runEnd - minSegment && w <= minWidth * 3; w += Mathf.Max(1, minWidth / 2))
                        {
                            int b = a + w;
                            for (int sign = -1; sign <= 1; sign += 2)
                            {
                                var side = perpendicular * sign;
                                int legs = 0;
                                while (legs < maxDepth && Usable(route[a] + side * (legs + 1), a, b) && Usable(route[b] + side * (legs + 1), a, b))
                                    legs++;
                                // Deepest detour whose far leg is free too
                                for (int depth = legs; depth >= minSegment; depth--)
                                {
                                    bool farLegFree = true;
                                    for (int k = a + 1; k < b && farLegFree; k++)
                                        farLegFree = Usable(route[k] + side * depth, a, b);
                                    if (!farLegFree) continue;
                                    candidates.Add((a, b, side, depth));
                                    break;
                                }
                            }
                        }
                    runStart = runEnd;
                }
                if (candidates.Count == 0) break;

                // Deep detours are likelier (squared weight), so the route reaches into open ground instead of rippling
                double total = 0;
                foreach (var c in candidates) total += (double)c.depth * c.depth;
                double pick = rng.NextDouble() * total;
                var chosen = candidates[candidates.Count - 1];
                foreach (var c in candidates)
                {
                    pick -= (double)c.depth * c.depth;
                    if (pick <= 0) { chosen = c; break; }
                }

                var detour = new List<Vector3Int>();
                for (int h = 1; h <= chosen.depth; h++) detour.Add(route[chosen.a] + chosen.side * h);
                for (int k = chosen.a + 1; k < chosen.b; k++) detour.Add(route[k] + chosen.side * chosen.depth);
                for (int h = chosen.depth; h >= 1; h--) detour.Add(route[chosen.b] + chosen.side * h);
                route.RemoveRange(chosen.a + 1, chosen.b - chosen.a - 1);
                route.InsertRange(chosen.a + 1, detour);
            }
            return route;
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
