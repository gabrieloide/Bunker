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
