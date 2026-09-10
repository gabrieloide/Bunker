using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Bunker.Simulation;

namespace Bunker.Tests
{
    [TestFixture]
    public class DamageSystemTests
    {
        private World world;
        private EntityManager em;
        private SystemHandle damageSystemHandle;

        [SetUp]
        public void SetUp()
        {
            world = new World("TestWorld");
            em = world.EntityManager;

            // Required simulation singletons
            world.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

            var waveEntity = em.CreateEntity();
            em.AddComponentData(waveEntity, new WaveState { EnemiesAlive = 0 });

            var eventEntity = em.CreateEntity();
            em.AddBuffer<SimEvent>(eventEntity);

            var sessionEntity = em.CreateEntity();
            em.AddComponentData(sessionEntity, new GameSession { Score = 0, IsGameOver = false });

            damageSystemHandle = world.GetOrCreateSystem<DamageSystem>();
        }

        [TearDown]
        public void TearDown()
        {
            if (world.IsCreated)
            {
                world.Dispose();
            }
        }

        [Test]
        public void Bunker_WhenDamageRequested_HealthDecreasesAndEventEmitted()
        {
            // Arrange: create bunker with 100 HP
            Entity bunker = em.CreateEntity();
            em.AddComponentData(bunker, new BunkerTag());
            em.AddComponentData(bunker, new Health { Value = 100f, Max = 100f });
            em.AddComponentData(bunker, LocalTransform.Identity);
            var damageBuffer = em.AddBuffer<DamageRequest>(bunker);

            damageBuffer.Add(new DamageRequest { Damage = 35f, BulletPen = 0f });

            // Act
            damageSystemHandle.Update(world.Unmanaged);

            // Assert
            var updatedHealth = em.GetComponentData<Health>(bunker);
            Assert.AreEqual(65f, updatedHealth.Value, 0.01f);

            var events = em.CreateEntityQuery(typeof(SimEvent)).GetSingletonBuffer<SimEvent>();
            Assert.IsTrue(events.Length > 0);
            Assert.AreEqual(SimEventKind.PlayerHit, events[0].Kind);
            Assert.AreEqual(35f, events[0].Amount, 0.01f);
        }

        [Test]
        public void Bunker_WhenLethalDamage_HealthZeroAndGameOverTriggered()
        {
            // Arrange
            Entity bunker = em.CreateEntity();
            em.AddComponentData(bunker, new BunkerTag());
            em.AddComponentData(bunker, new Health { Value = 20f, Max = 100f });
            em.AddComponentData(bunker, LocalTransform.Identity);
            var damageBuffer = em.AddBuffer<DamageRequest>(bunker);

            damageBuffer.Add(new DamageRequest { Damage = 50f, BulletPen = 0f });

            // Act
            damageSystemHandle.Update(world.Unmanaged);

            // Assert
            var updatedHealth = em.GetComponentData<Health>(bunker);
            Assert.AreEqual(0f, updatedHealth.Value, 0.01f);

            var session = em.CreateEntityQuery(typeof(GameSession)).GetSingleton<GameSession>();
            Assert.IsTrue(session.IsGameOver);

            var events = em.CreateEntityQuery(typeof(SimEvent)).GetSingletonBuffer<SimEvent>();
            bool hasGameOverEvent = false;
            for (int i = 0; i < events.Length; i++)
            {
                if (events[i].Kind == SimEventKind.GameOver)
                    hasGameOverEvent = true;
            }
            Assert.IsTrue(hasGameOverEvent, "Expected GameOver event to be emitted");
        }
    }
}
