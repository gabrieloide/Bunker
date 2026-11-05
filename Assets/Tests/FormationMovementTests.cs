using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Bunker.Simulation;

namespace Bunker.Tests
{
    // L-shaped movement and per-soldier slots inside the enemy flag circles
    [TestFixture]
    public class FormationMovementTests
    {
        [Test]
        public void StepAxisAligned_CoversHorizontalGapBeforeVertical()
        {
            var pos = BalanceMath.StepAxisAligned(float3.zero, new float3(4f, 3f, 0f), 2f);
            Assert.AreEqual(2f, pos.x, 1e-5f);
            Assert.AreEqual(0f, pos.y, 1e-5f, "no vertical movement while the horizontal gap is open");
        }

        [Test]
        public void StepAxisAligned_LeftoverStepTurnsTheCorner()
        {
            var pos = BalanceMath.StepAxisAligned(float3.zero, new float3(1f, 3f, 0f), 2.5f);
            Assert.AreEqual(1f, pos.x, 1e-5f);
            Assert.AreEqual(1.5f, pos.y, 1e-5f);
        }

        [Test]
        public void StepAxisAligned_ArrivesExactlyWithoutOvershoot()
        {
            var pos = BalanceMath.StepAxisAligned(new float3(0f, 0f, 0f), new float3(-1f, -1f, 0f), 10f);
            Assert.AreEqual(-1f, pos.x, 1e-5f);
            Assert.AreEqual(-1f, pos.y, 1e-5f);
        }

        [Test]
        public void FlagTarget_ShiftsBySlotTimesRadius()
        {
            var point = new PathPoint { Value = new float3(10f, 5f, 0f), Radius = 2f };
            var target = BalanceMath.FlagTarget(point, new float2(0.5f, -1f));
            Assert.AreEqual(11f, target.x, 1e-5f);
            Assert.AreEqual(3f, target.y, 1e-5f);
        }

        [Test]
        public void FlagTarget_ZeroRadiusIsTheExactPoint()
        {
            var point = new PathPoint { Value = new float3(10f, 5f, 0f), Radius = 0f };
            var target = BalanceMath.FlagTarget(point, new float2(0.9f, 0.9f));
            Assert.AreEqual(10f, target.x, 1e-5f);
            Assert.AreEqual(5f, target.y, 1e-5f);
        }

        [Test]
        public void PredictAlongPath_FollowsTheLAndTheSlot()
        {
            var world = new World("PredictTestWorld");
            try
            {
                var em = world.EntityManager;
                var holder = em.CreateEntity();
                var path = em.AddBuffer<PathPoint>(holder);
                path.Add(new PathPoint { Value = new float3(10f, 0f, 0f), Radius = 2f });
                path.Add(new PathPoint { Value = new float3(10f, 10f, 0f), Radius = 2f });

                // Slot (0, -1) puts this soldier 2 below the centre line: it walks to (10, -2), then (10, 8)
                var after3 = BalanceMath.PredictAlongPath(float3.zero, 0, path, 1f, 3f, new float2(0f, -1f));
                Assert.AreEqual(3f, after3.x, 1e-4f, "horizontal gap first");
                Assert.AreEqual(0f, after3.y, 1e-4f);

                var after14 = BalanceMath.PredictAlongPath(float3.zero, 0, path, 1f, 14f, new float2(0f, -1f));
                Assert.AreEqual(10f, after14.x, 1e-4f);
                Assert.AreEqual(0f, after14.y, 1e-4f, "10 across + 2 down reach (10, -2), the remaining 2 climb back to y = 0");
            }
            finally
            {
                world.Dispose();
            }
        }

        [Test]
        public void Enemy_WithSlot_WalksAxisAlignedTowardsItsSpotInTheFlag()
        {
            var world = new World("FormationTestWorld");
            try
            {
                var em = world.EntityManager;
                world.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
                var sim = em.CreateEntity();
                em.AddComponentData(sim, new WaveConfig());
                em.AddBuffer<SimEvent>(sim);
                em.AddComponent<PrimaryPath>(sim);
                var path = em.AddBuffer<PathPoint>(sim);
                path.Add(new PathPoint { Value = new float3(10f, 10f, 0f), Radius = 2f });
                path.Add(new PathPoint { Value = new float3(10f, 0f, 0f), Radius = 0f });

                var enemy = em.CreateEntity();
                em.AddComponent<EnemyTag>(enemy);
                em.AddComponentData(enemy, LocalTransform.FromPosition(new float3(0f, 10f, 0f)));
                em.AddComponentData(enemy, new MoveSpeed { Value = 4f });
                em.AddComponentData(enemy, new PathFollower { NextIndex = 0, Slot = new float2(0f, -1f) });

                var system = world.GetOrCreateSystem<EnemyMovementSystem>();
                world.SetTime(new Unity.Core.TimeData(0.0, 1f));
                system.Update(world.Unmanaged);

                var pos = em.GetComponentData<LocalTransform>(enemy).Position;
                Assert.AreEqual(4f, pos.x, 1e-4f, "horizontal first");
                Assert.AreEqual(10f, pos.y, 1e-4f, "no diagonal drift while the horizontal gap is open");
            }
            finally
            {
                world.Dispose();
            }
        }
    }
}
