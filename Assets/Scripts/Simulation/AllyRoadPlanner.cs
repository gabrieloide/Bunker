using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bunker.Simulation
{
    // Pure grid planning logic for player-built ally roads: orthogonal L-segments connected
    // to the bunker or existing road cells, capped by per-card and per-level budgets, without 2x2 blobs.
    public static class AllyRoadPlanner
    {
        public static bool TryPlanSegment(
            Vector3Int bunkerCell,
            HashSet<Vector3Int> existingRoadCells,
            Vector3Int targetCell,
            int maxCardTiles,
            int remainingLevelTiles,
            Func<Vector3Int, bool> isCellAllowed,
            out List<Vector3Int> plannedCells,
            out Vector3Int anchorCell)
        {
            plannedCells = new List<Vector3Int>();
            anchorCell = bunkerCell;

            int budget = Math.Min(Math.Max(1, maxCardTiles), Math.Max(0, remainingLevelTiles));
            if (budget <= 0)
                return false;

            anchorCell = FindNearestAnchor(bunkerCell, existingRoadCells, targetCell);
            if (targetCell == anchorCell)
                return false;

            bool preferHorizontalFirst = Mathf.Abs(targetCell.x - anchorCell.x) >= Mathf.Abs(targetCell.y - anchorCell.y);

            if (TryBuildLPath(bunkerCell, existingRoadCells, anchorCell, targetCell, preferHorizontalFirst, budget, isCellAllowed, plannedCells))
                return true;

            plannedCells.Clear();
            if (TryBuildLPath(bunkerCell, existingRoadCells, anchorCell, targetCell, !preferHorizontalFirst, budget, isCellAllowed, plannedCells))
                return true;

            plannedCells.Clear();
            return false;
        }

        static Vector3Int FindNearestAnchor(Vector3Int bunkerCell, HashSet<Vector3Int> existingRoadCells, Vector3Int targetCell)
        {
            if (existingRoadCells == null || existingRoadCells.Count == 0)
                return bunkerCell;

            Vector3Int best = bunkerCell;
            int bestDist = Manhattan(bunkerCell, targetCell);

            foreach (var cell in existingRoadCells)
            {
                int d = Manhattan(cell, targetCell);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = cell;
                }
            }
            return best;
        }

        static bool TryBuildLPath(
            Vector3Int bunkerCell,
            HashSet<Vector3Int> existingRoadCells,
            Vector3Int anchor,
            Vector3Int target,
            bool horizontalFirst,
            int maxCount,
            Func<Vector3Int, bool> isCellAllowed,
            List<Vector3Int> output)
        {
            output.Clear();
            var pending = new HashSet<Vector3Int>();
            Vector3Int current = anchor;

            if (horizontalFirst)
            {
                if (!StepAxis(bunkerCell, existingRoadCells, ref current, target.x, true, maxCount, pending, isCellAllowed, output))
                    return output.Count > 0;
                StepAxis(bunkerCell, existingRoadCells, ref current, target.y, false, maxCount, pending, isCellAllowed, output);
            }
            else
            {
                if (!StepAxis(bunkerCell, existingRoadCells, ref current, target.y, false, maxCount, pending, isCellAllowed, output))
                    return output.Count > 0;
                StepAxis(bunkerCell, existingRoadCells, ref current, target.x, true, maxCount, pending, isCellAllowed, output);
            }

            return output.Count > 0;
        }

        static bool StepAxis(
            Vector3Int bunkerCell,
            HashSet<Vector3Int> existingRoadCells,
            ref Vector3Int current,
            int targetCoord,
            bool xAxis,
            int maxCount,
            HashSet<Vector3Int> pending,
            Func<Vector3Int, bool> isCellAllowed,
            List<Vector3Int> output)
        {
            int coord = xAxis ? current.x : current.y;
            int dir = Math.Sign(targetCoord - coord);
            if (dir == 0)
                return true;

            while ((xAxis ? current.x : current.y) != targetCoord && output.Count < maxCount)
            {
                Vector3Int next = current;
                if (xAxis) next.x += dir;
                else next.y += dir;

                if (next == bunkerCell || pending.Contains(next) || (existingRoadCells != null && existingRoadCells.Contains(next)))
                    return false;

                if (CreatesSolidSquare(next, existingRoadCells, pending))
                    return false;

                if (isCellAllowed != null && !isCellAllowed(next))
                    return false;

                output.Add(next);
                pending.Add(next);
                current = next;
            }
            return true;
        }

        // Prevents messy 2x2 road blobs so ally paths stay clean and readable
        static bool CreatesSolidSquare(Vector3Int cell, HashSet<Vector3Int> existingRoadCells, HashSet<Vector3Int> pending)
        {
            for (int ox = -1; ox <= 0; ox++)
            {
                for (int oy = -1; oy <= 0; oy++)
                {
                    int count = 0;
                    for (int dx = 0; dx <= 1; dx++)
                    {
                        for (int dy = 0; dy <= 1; dy++)
                        {
                            var c = new Vector3Int(cell.x + ox + dx, cell.y + oy + dy, cell.z);
                            if (c == cell || pending.Contains(c) || (existingRoadCells != null && existingRoadCells.Contains(c)))
                                count++;
                        }
                    }
                    if (count == 4)
                        return true;
                }
            }
            return false;
        }

        static int Manhattan(Vector3Int a, Vector3Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}
