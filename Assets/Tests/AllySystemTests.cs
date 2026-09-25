using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Bunker.Simulation;

namespace Bunker.Tests
{
    [TestFixture]
    public class AllySystemTests
    {
        World world;
        EntityManager em;
        SystemHandle allySystem;
        SystemHandle damageSystem;
        Entity simEntity;
        BlobAssetReference<AllyRosterBlob> roster;

        [SetUp]
        public void SetUp()
        {
            world = new World("AllyTestWorld");
            em = world.EntityManager;
            world.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

            simEntity = em.CreateEntity();
            em.AddComponentData(simEntity, new WaveState());
            em.AddComponentData(simEntity, new GameSession());
            em.AddBuffer<SimEvent>(simEntity);
            var path = em.AddBuffer<PathPoint>(simEntity);
            path.Add(new PathPoint { Value = new float3(10f, 0f, 0f) });
            path.Add(new PathPoint { Value = new float3(0f, 0f, 0f) });

            using var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<AllyRosterBlob>();
            var types = builder.Allocate(ref root.Types, 1);
            types[0] = new AllyTypeDef { Life = 10f, Damage = 5f, AttackInterval = 1f, AttackRange = 1f, MoveSpeed = 1f, HitHalfExtents = 0.5f };
            roster = builder.CreateBlobAssetReference<AllyRosterBlob>(Allocator.Persistent);

            var config = em.CreateEntity();
            em.AddComponentData(config, new AllySpawnConfig { Interval = 0f, Roster = roster });
            em.AddComponentData(config, new AllySpawnState());

            allySystem = world.GetOrCreateSystem<AllySystem>();
            damageSystem = world.GetOrCreateSystem<DamageSystem>();
        }

        [TearDown]
        public void TearDown()
        {
            if (world.IsCreated) world.Dispose();
            if (roster.IsCreated) roster.Dispose();
        }

        Entity CreateAlly(float3 position, int nextIndex)
        {
            var ally = em.CreateEntity();
            em.AddComponent<AllyTag>(ally);
            em.AddComponentData(ally, LocalTransform.FromPosition(position));
            em.AddComponentData(ally, new Health { Value = 10f, Max = 10f });
            em.AddComponentData(ally, new MoveSpeed { Value = 100f });
            em.AddComponentData(ally, new Melee { Damage = 5f, Interval = 1f, Range = 1f });
            em.AddComponentData(ally, new AllyPathFollower { NextIndex = nextIndex });
            em.AddBuffer<DamageRequest>(ally);
            return ally;
        }

        Entity CreateBase(float3 position, float life)
        {
            var enemyBase = em.CreateEntity();
            em.AddComponent<EnemyBaseTag>(enemyBase);
            em.AddComponentData(enemyBase, LocalTransform.FromPosition(position));
            em.AddComponentData(enemyBase, new Health { Value = life, Max = life });
            em.AddComponentData(enemyBase, new HitBox { HalfExtents = new float2(0.5f, 0.5f) });
            em.AddBuffer<DamageRequest>(enemyBase);
            return enemyBase;
        }

        DynamicBuffer<SimEvent> Events => em.GetBuffer<SimEvent>(simEntity);

        [Test]
        public void Ally_WalksPathBackwards()
        {
            var ally = CreateAlly(new float3(0f, 0f, 0f), 1);
            CreateBase(new float3(50f, 0f, 0f), 100f);

            world.SetTime(new Unity.Core.TimeData(0, 0.01f));
            allySystem.Update(world.Unmanaged);
            Assert.AreEqual(0, em.GetComponentData<AllyPathFollower>(ally).NextIndex, "reached last point, heads for the previous one");

            world.SetTime(new Unity.Core.TimeData(0.02, 0.05f));
            allySystem.Update(world.Unmanaged);
            Assert.Greater(em.GetComponentData<LocalTransform>(ally).Position.x, 0f);
        }

        [Test]
        public void Ally_AtEnemyBase_HitsItAndLethalDamageWins()
        {
            var ally = CreateAlly(new float3(1.2f, 0f, 0f), -1);
            var enemyBase = CreateBase(float3.zero, 5f);

            world.SetTime(new Unity.Core.TimeData(0, 0.01f));
            allySystem.Update(world.Unmanaged);
            Assert.AreEqual(1, em.GetBuffer<DamageRequest>(enemyBase).Length);
            Assert.AreEqual(1.2f, em.GetComponentData<LocalTransform>(ally).Position.x, 1e-4f, "stops to attack");

            damageSystem.Update(world.Unmanaged);
            Assert.AreEqual(0f, em.GetComponentData<Health>(enemyBase).Value, 1e-4f);
            Assert.IsTrue(em.GetComponentData<GameSession>(simEntity).IsGameOver);

            bool victory = false;
            foreach (var ev in Events) victory |= ev.Kind == SimEventKind.Victory;
            Assert.IsTrue(victory);
        }

        [Test]
        public void Ally_StopsToHitEnemyInRange()
        {
            var ally = CreateAlly(float3.zero, 1);
            var enemy = em.CreateEntity();
            em.AddComponent<EnemyTag>(enemy);
            em.AddComponentData(enemy, LocalTransform.FromPosition(new float3(0.5f, 0f, 0f)));
            em.AddComponentData(enemy, new Health { Value = 50f, Max = 50f });
            em.AddBuffer<DamageRequest>(enemy);

            world.SetTime(new Unity.Core.TimeData(0, 0.01f));
            allySystem.Update(world.Unmanaged);

            Assert.AreEqual(1, em.GetBuffer<DamageRequest>(enemy).Length);
            Assert.AreEqual(1, em.GetComponentData<AllyPathFollower>(ally).NextIndex, "did not move on");
        }

        [Test]
        public void Ally_LethalDamage_DiesWithEvent()
        {
            var ally = CreateAlly(float3.zero, 1);
            em.GetBuffer<DamageRequest>(ally).Add(new DamageRequest { Damage = 20f });

            damageSystem.Update(world.Unmanaged);

            bool died = false;
            foreach (var ev in Events) died |= ev.Kind == SimEventKind.AllyDied && ev.Source == ally;
            Assert.IsTrue(died);
        }

        [Test]
        public void Victory_AfterGameOver_IsIgnored()
        {
            var session = em.GetComponentData<GameSession>(simEntity);
            session.IsGameOver = true;
            em.SetComponentData(simEntity, session);
            var enemyBase = CreateBase(float3.zero, 5f);
            em.GetBuffer<DamageRequest>(enemyBase).Add(new DamageRequest { Damage = 10f });

            damageSystem.Update(world.Unmanaged);

            foreach (var ev in Events) Assert.AreNotEqual(SimEventKind.Victory, ev.Kind);
        }
    }
}
