using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Bunker.Simulation;

namespace Bunker.Tests
{
    // The procedural enemy route: shortest 4-way walk around blocked cells, fewest turns among equals
    [TestFixture]
    public class RoutePlannerTests
    {
        static readonly BoundsInt Area = new BoundsInt(-10, -10, 0, 20, 20, 1);

        static List<Vector3Int> Plan(Vector3Int from, Vector3Int to, HashSet<Vector3Int> walls) =>
            RoutePlanner.FindPath(from, to, c => walls.Contains(c), Area);

        [Test]
        public void OpenGround_GoesStraight()
        {
            var path = Plan(new Vector3Int(0, 0, 0), new Vector3Int(5, 0, 0), new HashSet<Vector3Int>());
            Assert.AreEqual(6, path.Count);
            Assert.AreEqual(2, RoutePlanner.Corners(path).Count, "no turn: only both ends");
        }

        [Test]
        public void Walls_AreWalkedAround_NeverThrough_NeverDiagonally()
        {
            // A wall across x = 2 from y = -2 to y = 2
            var walls = new HashSet<Vector3Int>();
            for (int y = -2; y <= 2; y++) walls.Add(new Vector3Int(2, y, 0));

            var path = Plan(new Vector3Int(0, 0, 0), new Vector3Int(4, 0, 0), walls);

            Assert.IsNotEmpty(path);
            for (int i = 0; i < path.Count; i++)
            {
                Assert.IsFalse(walls.Contains(path[i]), $"{path[i]} is a wall");
                if (i > 0)
                    Assert.AreEqual(1, Mathf.Abs(path[i].x - path[i - 1].x) + Mathf.Abs(path[i].y - path[i - 1].y), "one straight step at a time");
            }
            Assert.AreEqual(4 + 2 * 3 + 1, path.Count, "shortest detour: 3 up, across, 3 down");
        }

        [Test]
        public void EquallyShortWalks_PreferFewestTurns()
        {
            var path = Plan(new Vector3Int(0, 0, 0), new Vector3Int(4, 4, 0), new HashSet<Vector3Int>());
            Assert.AreEqual(9, path.Count);
            Assert.AreEqual(3, RoutePlanner.Corners(path).Count, "a single L, not a staircase");
        }

        [Test]
        public void ClosedOff_ReturnsNoRoute()
        {
            var walls = new HashSet<Vector3Int>();
            for (int x = -1; x <= 1; x++)
                for (int y = -1; y <= 1; y++)
                    if (x != 0 || y != 0) walls.Add(new Vector3Int(5 + x, y, 0));

            // The goal itself is walkable, but every way into it is walled
            Assert.IsEmpty(Plan(new Vector3Int(0, 0, 0), new Vector3Int(5, 0, 0), walls));
        }

        [Test]
        public void BlockedEnds_AreStillReached()
        {
            var walls = new HashSet<Vector3Int> { new Vector3Int(0, 0, 0), new Vector3Int(3, 0, 0) };
            var path = Plan(new Vector3Int(0, 0, 0), new Vector3Int(3, 0, 0), walls);
            Assert.AreEqual(4, path.Count, "the spawner and the bunker door may sit on decorated cells");
        }

        static readonly BoundsInt Field = new BoundsInt(0, 0, 0, 40, 24, 1);

        static List<Vector3Int> Wind(HashSet<Vector3Int> walls, RoutePlanner.MeanderSettings settings, out List<Vector3Int> shortest)
        {
            shortest = RoutePlanner.FindPath(new Vector3Int(2, 12, 0), new Vector3Int(37, 12, 0), c => walls.Contains(c), Field);
            return RoutePlanner.Meander(shortest, c => walls.Contains(c), Field, settings);
        }

        static HashSet<Vector3Int> Clumps()
        {
            var walls = new HashSet<Vector3Int>();
            foreach (var (cx, cy) in new[] { (12, 6), (20, 17), (28, 8) })
                for (int x = -1; x <= 1; x++)
                    for (int y = -1; y <= 1; y++)
                        walls.Add(new Vector3Int(cx + x, cy + y, 0));
            return walls;
        }

        [Test]
        public void Meander_WindsToAboutTheTargetLength()
        {
            var settings = RoutePlanner.MeanderSettings.Default;
            var route = Wind(Clumps(), settings, out var shortest);
            int target = Mathf.RoundToInt((shortest.Count - 1) * settings.lengthFactor);

            Assert.Greater(route.Count, shortest.Count, "it grew");
            Assert.LessOrEqual(route.Count - 1, target, "never past the target");
            Assert.GreaterOrEqual(route.Count - 1, target - 2 * settings.minSegment, "and as close as a last detour allows");
        }

        [Test]
        public void Meander_StaysAWalkableSelfAvoidingRoute()
        {
            var walls = Clumps();
            var route = Wind(walls, RoutePlanner.MeanderSettings.Default, out var shortest);

            Assert.AreEqual(shortest[0], route[0]);
            Assert.AreEqual(shortest[shortest.Count - 1], route[route.Count - 1]);
            var seen = new HashSet<Vector3Int>();
            for (int i = 0; i < route.Count; i++)
            {
                Assert.IsTrue(seen.Add(route[i]), $"{route[i]} walked twice");
                Assert.IsFalse(walls.Contains(route[i]), $"{route[i]} is a wall");
                Assert.IsTrue(Field.Contains(route[i]));
                if (i > 0)
                    Assert.AreEqual(1, Mathf.Abs(route[i].x - route[i - 1].x) + Mathf.Abs(route[i].y - route[i - 1].y), "one straight step at a time");
            }
        }

        [Test]
        public void Meander_KeepsTheGapBetweenLegsThatPassByEachOther()
        {
            var settings = RoutePlanner.MeanderSettings.Default;
            var route = Wind(Clumps(), settings, out _);

            // Cells far apart along the route (not the two sides of one corner) must be more than `gap` cells apart on the map
            for (int i = 0; i < route.Count; i++)
                for (int j = i + 2 * settings.gap + 2; j < route.Count; j++)
                {
                    int distance = Mathf.Max(Mathf.Abs(route[i].x - route[j].x), Mathf.Abs(route[i].y - route[j].y));
                    Assert.Greater(distance, settings.gap, $"cells {i} {route[i]} and {j} {route[j]} leave no room for towers");
                }
        }

        [Test]
        public void Meander_SameSeedSameRoute_OtherSeedOtherRoute()
        {
            var settings = RoutePlanner.MeanderSettings.Default;
            var first = Wind(Clumps(), settings, out _);
            var again = Wind(Clumps(), settings, out _);
            CollectionAssert.AreEqual(first, again);

            bool anyDifferent = false;
            for (int seed = 2; seed <= 6 && !anyDifferent; seed++)
            {
                settings.seed = seed;
                var other = Wind(Clumps(), settings, out _);
                anyDifferent = other.Count != first.Count || !new HashSet<Vector3Int>(other).SetEquals(first);
            }
            Assert.IsTrue(anyDifferent, "another seed gives another variant");
        }

        [Test]
        public void Meander_FactorOne_KeepsTheShortestWalk()
        {
            var settings = RoutePlanner.MeanderSettings.Default;
            settings.lengthFactor = 1f;
            var route = Wind(Clumps(), settings, out var shortest);
            CollectionAssert.AreEqual(shortest, route);
        }

        [Test]
        public void Corners_KeepEndsAndTurnsOnly()
        {
            var path = new List<Vector3Int>
            {
                new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0), new Vector3Int(2, 0, 0),
                new Vector3Int(2, 1, 0), new Vector3Int(2, 2, 0), new Vector3Int(3, 2, 0)
            };
            CollectionAssert.AreEqual(
                new[] { new Vector3Int(0, 0, 0), new Vector3Int(2, 0, 0), new Vector3Int(2, 2, 0), new Vector3Int(3, 2, 0) },
                RoutePlanner.Corners(path));
        }
    }
}
