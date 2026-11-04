using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Bunker.Simulation;

namespace Bunker.Tests
{
    // Enemy/ally contact and same-side spacing in EnemyMovementSystem
    [TestFixture]
    public class SoldierContactTests
    {
        World world;
        EntityManager em;
        SystemHandle movementSystem;
        Entity simEntity;

        [SetUp]
        public void SetUp()
        {
            world = new World("SoldierContactTestWorld");
            em = world.EntityManager;
            world.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

            simEntity = em.CreateEntity();
            em.AddComponentData(simEntity, new WaveConfig { ContactDistance = 0.6f, UnitSpacing = 0.5f });
            em.AddComponentData(simEntity, new WaveState());
            em.AddBuffer<SimEvent>(simEntity);
            em.AddComponent<PrimaryPath>(simEntity);

            // Path: (0, 10) -> (0, 0)
            var path = em.AddBuffer<PathPoint>(simEntity);
            path.Add(new PathPoint { Value = new float3(0f, 10f, 0f) });
            path.Add(new PathPoint { Value = new float3(0f, 0f, 0f) });

            movementSystem = world.GetOrCreateSystem<EnemyMovementSystem>();
        }

        [TearDown]
        public void TearDown()
        {
            if (world.IsCreated) world.Dispose();
        }

        Entity CreateEnemy(float3 position, float damage = 3f, float interval = 1f)
        {
            var e = em.CreateEntity();
            em.AddComponent<EnemyTag>(e);
            em.AddComponent<SimulationTag>(e);
            em.AddComponentData(e, LocalTransform.FromPosition(position));
            em.AddComponentData(e, new MoveSpeed { Value = 2f });
            em.AddComponentData(e, new PathFollower { NextIndex = 1 });
            em.AddComponentData(e, new Weapon { Damage = damage, FireInterval = interval });
            em.AddComponentData(e, new Health { Value = 10f, Max = 10f });
            em.AddBuffer<DamageRequest>(e);
            return e;
        }

        Entity CreateAlly(float3 position)
        {
            var a = em.CreateEntity();
            em.AddComponent<AllyTag>(a);
            em.AddComponent<SimulationTag>(a);
            em.AddComponentData(a, LocalTransform.FromPosition(position));
            em.AddComponentData(a, new Health { Value = 10f, Max = 10f });
            em.AddBuffer<DamageRequest>(a);
            return a;
        }

        void Step(float dt)
        {
            world.SetTime(new Unity.Core.TimeData(0.0, dt));
            movementSystem.Update(world.Unmanaged);
            world.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>().Update(world.Unmanaged);
        }

        [Test]
        public void Enemy_TouchingAlly_StopsAndHitsIt()
        {
            var enemy = CreateEnemy(new float3(0f, 5f, 0f));
            var ally = CreateAlly(new float3(0f, 4.6f, 0f));

            Step(0.5f);

            Assert.AreEqual(5f, em.GetComponentData<LocalTransform>(enemy).Position.y, 0.001f, "enemy did not walk on");
            var damage = em.GetBuffer<DamageRequest>(ally);
            Assert.AreEqual(1, damage.Length);
            Assert.AreEqual(3f, damage[0].Damage, 0.001f);
        }

        [Test]
        public void Enemy_HitsAllyOnlyOncePerInterval()
        {
            CreateEnemy(new float3(0f, 5f, 0f), 3f, 1f);
            var ally = CreateAlly(new float3(0f, 4.6f, 0f));

            Step(0.4f);
            Step(0.4f);
            Step(0.4f);
            Assert.AreEqual(1, em.GetBuffer<DamageRequest>(ally).Length, "1.2s in, the 1s cooldown has not run out yet after the first hit at 0.4s");
            Step(0.4f);
            Assert.AreEqual(2, em.GetBuffer<DamageRequest>(ally).Length, "cooldown elapsed");
        }

        [Test]
        public void Enemy_NewContact_StartsWithWindUp()
        {
            var enemy = CreateEnemy(new float3(0f, 5f, 0f), 3f, 1f);
            var ally = CreateAlly(new float3(0f, 1f, 0f));
            Step(0.1f); // walking free, wind-up armed

            em.SetComponentData(ally, LocalTransform.FromPosition(new float3(0f, em.GetComponentData<LocalTransform>(enemy).Position.y - 0.3f, 0f)));
            Step(0.1f);
            Assert.AreEqual(0, em.GetBuffer<DamageRequest>(ally).Length, "no instant first blow");
            Step(0.5f);
            Assert.AreEqual(1, em.GetBuffer<DamageRequest>(ally).Length, "blow lands once the half-interval wind-up is over");
        }

        [Test]
        public void Enemy_FarFromAlly_KeepsWalking()
        {
            var enemy = CreateEnemy(new float3(0f, 5f, 0f));
            CreateAlly(new float3(0f, 2f, 0f));

            Step(0.5f);

            Assert.Less(em.GetComponentData<LocalTransform>(enemy).Position.y, 5f);
        }

        [Test]
        public void Enemy_WithZeroContactDistance_WalksThroughAllies()
        {
            em.SetComponentData(simEntity, new WaveConfig { ContactDistance = 0f, UnitSpacing = 0f });
            var enemy = CreateEnemy(new float3(0f, 5f, 0f));
            var ally = CreateAlly(new float3(0f, 4.9f, 0f));

            Step(0.5f);

            Assert.Less(em.GetComponentData<LocalTransform>(enemy).Position.y, 5f);
            Assert.AreEqual(0, em.GetBuffer<DamageRequest>(ally).Length);
        }

        [Test]
        public void Enemy_RightBehindAnotherEnemy_WaitsForIt()
        {
            var front = CreateEnemy(new float3(0f, 5f, 0f));
            var back = CreateEnemy(new float3(0f, 5.3f, 0f));

            Step(0.25f);

            Assert.Less(em.GetComponentData<LocalTransform>(front).Position.y, 5f, "front enemy moves");
            Assert.AreEqual(5.3f, em.GetComponentData<LocalTransform>(back).Position.y, 0.001f, "rear enemy waits");
        }

        [Test]
        public void Enemies_StackedOnTheSamePoint_OnlyOneWaits()
        {
            var a = CreateEnemy(new float3(0f, 5f, 0f));
            var b = CreateEnemy(new float3(0f, 5f, 0f));

            Step(0.25f);

            bool aMoved = em.GetComponentData<LocalTransform>(a).Position.y < 5f;
            bool bMoved = em.GetComponentData<LocalTransform>(b).Position.y < 5f;
            Assert.IsTrue(aMoved ^ bMoved, "exactly one of two overlapping enemies moves, so they separate instead of deadlocking");
        }

        [Test]
        public void Enemies_OnDifferentPaths_DoNotBlockEachOther()
        {
            var other = em.CreateEntity();
            em.AddComponent<SimulationTag>(other);
            var path = em.AddBuffer<PathPoint>(other);
            path.Add(new PathPoint { Value = new float3(10f, 5f, 0f) });
            path.Add(new PathPoint { Value = new float3(0f, 0f, 0f) });

            var a = CreateEnemy(new float3(0f, 5f, 0f));
            var b = CreateEnemy(new float3(0f, 5.2f, 0f));
            em.SetComponentData(b, new PathFollower { NextIndex = 1, PathEntity = other });

            Step(0.25f);

            Assert.Less(em.GetComponentData<LocalTransform>(a).Position.y, 5f);
            Assert.Less(em.GetComponentData<LocalTransform>(b).Position.y, 5.2f);
        }
    }
}
