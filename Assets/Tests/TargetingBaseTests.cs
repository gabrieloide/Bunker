using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Bunker.Simulation;

namespace Bunker.Tests
{
    [TestFixture]
    public class TargetingBaseTests
    {
        World world;
        EntityManager em;
        SystemHandle targetingSystem;
        SystemHandle projectileSystem;
        SystemHandle artilleryShellSystem;
        SystemHandle damageSystem;
        Entity simEntity;

        [SetUp]
        public void SetUp()
        {
            world = new World("TargetingBaseTestWorld");
            em = world.EntityManager;
            world.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

            simEntity = em.CreateEntity();
            em.AddComponentData(simEntity, new WaveConfig());
            em.AddComponentData(simEntity, new WaveState());
            em.AddComponentData(simEntity, new GameSession());
            em.AddBuffer<SimEvent>(simEntity);

            targetingSystem = world.GetOrCreateSystem<TargetingSystem>();
            projectileSystem = world.GetOrCreateSystem<ProjectileSystem>();
            artilleryShellSystem = world.GetOrCreateSystem<ArtilleryShellSystem>();
            damageSystem = world.GetOrCreateSystem<DamageSystem>();
        }

        [TearDown]
        public void TearDown()
        {
            if (world.IsCreated) world.Dispose();
        }

        Entity CreateEnemyBase(float3 position, float life)
        {
            var baseEntity = em.CreateEntity();
            em.AddComponent<EnemyBaseTag>(baseEntity);
            em.AddComponentData(baseEntity, LocalTransform.FromPosition(position));
            em.AddComponentData(baseEntity, new Health { Value = life, Max = life });
            em.AddComponentData(baseEntity, new HitBox { HalfExtents = new float2(1f, 1f), Offset = float2.zero });
            em.AddBuffer<DamageRequest>(baseEntity);
            return baseEntity;
        }

        Entity CreateTower(float3 position, float range, float damage)
        {
            var tower = em.CreateEntity();
            em.AddComponent<TowerTag>(tower);
            em.AddComponentData(tower, LocalTransform.FromPosition(position));
            em.AddComponentData(tower, new Health { Value = 100f, Max = 100f });
            em.AddComponentData(tower, new Weapon
            {
                TargetFaction = Faction.Enemy,
                Range = range,
                Damage = damage,
                FireInterval = 1f,
                BurstInterval = 0.1f,
                BurstCount = 1,
                ShotsLeftInBurst = 0,
                ProjectileSpeed = 50f,
                ProjectileLifetime = 2f,
                ProjectileHalfExtents = new float2(0.2f, 0.2f),
                MuzzleOffset = float3.zero,
                Cooldown = 0f
            });
            em.AddComponentData(tower, new Target());
            return tower;
        }

        [Test]
        public void Tower_TargetsEnemyBase_WhenNoEnemiesInRange()
        {
            var enemyBase = CreateEnemyBase(new float3(5f, 0f, 0f), 100f);
            var tower = CreateTower(float3.zero, 5f, 20f);

            targetingSystem.Update(world.Unmanaged);

            var target = em.GetComponentData<Target>(tower);
            Assert.AreEqual(enemyBase, target.Value);
        }

        [Test]
        public void Tower_PrefersEnemiesOverEnemyBase()
        {
            var enemyBase = CreateEnemyBase(new float3(5f, 0f, 0f), 100f);
            var enemy = em.CreateEntity();
            em.AddComponent<EnemyTag>(enemy);
            em.AddComponentData(enemy, LocalTransform.FromPosition(new float3(2f, 0f, 0f)));
            em.AddComponentData(enemy, new Health { Value = 50f, Max = 50f });

            var tower = CreateTower(float3.zero, 5f, 20f);

            targetingSystem.Update(world.Unmanaged);

            var target = em.GetComponentData<Target>(tower);
            Assert.AreEqual(enemy, target.Value);
        }

        [Test]
        public void Tower_IgnoresDeadEnemyBase()
        {
            var enemyBase = CreateEnemyBase(new float3(5f, 0f, 0f), 0f);
            var tower = CreateTower(float3.zero, 5f, 20f);

            targetingSystem.Update(world.Unmanaged);

            var target = em.GetComponentData<Target>(tower);
            Assert.AreEqual(Entity.Null, target.Value);
        }

        [Test]
        public void Tower_IgnoresEnemyBaseOutOfRange()
        {
            // Tower at 0 with range 5, base at (10, 0, 0) with halfExtents (1, 1). Distance 10 > 5 + 1
            var enemyBase = CreateEnemyBase(new float3(10f, 0f, 0f), 100f);
            var tower = CreateTower(float3.zero, 5f, 20f);

            targetingSystem.Update(world.Unmanaged);

            var target = em.GetComponentData<Target>(tower);
            Assert.AreEqual(Entity.Null, target.Value);
        }

        [Test]
        public void Projectile_HitsEnemyBase_DealsDamage()
        {
            var enemyBase = CreateEnemyBase(new float3(2f, 0f, 0f), 100f);

            var projectile = em.CreateEntity();
            em.AddComponent<SimulationTag>(projectile);
            em.AddComponentData(projectile, LocalTransform.FromPosition(new float3(1.8f, 0f, 0f)));
            em.AddComponentData(projectile, new Projectile
            {
                Damage = 25f,
                BulletPen = 0f,
                Lifetime = 1f,
                Velocity = new float3(10f, 0f, 0f),
                HalfExtents = new float2(0.2f, 0.2f),
                TargetFaction = Faction.Enemy
            });

            // Update ProjectileSystem to detect hit and enqueue DamageRequest
            projectileSystem.Update(world.Unmanaged);
            world.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>().Update(world.Unmanaged);
            damageSystem.Update(world.Unmanaged);

            var health = em.GetComponentData<Health>(enemyBase);
            Assert.AreEqual(75f, health.Value);
        }

        [Test]
        public void ArtilleryShell_HitsEnemyBase_DealsDamage()
        {
            var enemyBase = CreateEnemyBase(new float3(5f, 0f, 0f), 100f);

            var shell = em.CreateEntity();
            em.AddComponent<SimulationTag>(shell);
            em.AddComponentData(shell, LocalTransform.FromPosition(new float3(5f, 0f, 0f)));
            em.AddComponentData(shell, new ArtilleryShell
            {
                Origin = float3.zero,
                Impact = new float3(5f, 0f, 0f),
                RiseTime = 0.5f,
                FallTime = 0.5f,
                Elapsed = 1.0f, // Already landed
                Height = 10f,
                Damage = 40f,
                BulletPen = 0f,
                SplashRadius = 2f,
                ViewId = 0
            });

            artilleryShellSystem.Update(world.Unmanaged);
            world.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>().Update(world.Unmanaged);
            damageSystem.Update(world.Unmanaged);

            var health = em.GetComponentData<Health>(enemyBase);
            Assert.AreEqual(60f, health.Value);
        }

        [Test]
        public void Tower_LethalDamageOnEnemyBase_EmitsSingleVictoryEvent()
        {
            var enemyBase = CreateEnemyBase(new float3(2f, 0f, 0f), 20f);

            var projectile = em.CreateEntity();
            em.AddComponent<SimulationTag>(projectile);
            em.AddComponentData(projectile, LocalTransform.FromPosition(new float3(1.8f, 0f, 0f)));
            em.AddComponentData(projectile, new Projectile
            {
                Damage = 50f,
                BulletPen = 0f,
                Lifetime = 1f,
                Velocity = new float3(10f, 0f, 0f),
                HalfExtents = new float2(0.2f, 0.2f),
                TargetFaction = Faction.Enemy
            });

            // Impact frame
            projectileSystem.Update(world.Unmanaged);
            world.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>().Update(world.Unmanaged);
            damageSystem.Update(world.Unmanaged);

            var events = em.GetBuffer<SimEvent>(simEntity);
            int victoryCount = 0;
            for (int i = 0; i < events.Length; i++)
            {
                if (events[i].Kind == SimEventKind.Victory)
                    victoryCount++;
            }
            Assert.AreEqual(1, victoryCount);

            // Subsequent frame with another projectile: verify no duplicate victory event is emitted
            var projectile2 = em.CreateEntity();
            em.AddComponent<SimulationTag>(projectile2);
            em.AddComponentData(projectile2, LocalTransform.FromPosition(new float3(1.8f, 0f, 0f)));
            em.AddComponentData(projectile2, new Projectile
            {
                Damage = 50f,
                BulletPen = 0f,
                Lifetime = 1f,
                Velocity = new float3(10f, 0f, 0f),
                HalfExtents = new float2(0.2f, 0.2f),
                TargetFaction = Faction.Enemy
            });

            projectileSystem.Update(world.Unmanaged);
            world.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>().Update(world.Unmanaged);
            damageSystem.Update(world.Unmanaged);

            events = em.GetBuffer<SimEvent>(simEntity);
            victoryCount = 0;
            for (int i = 0; i < events.Length; i++)
            {
                if (events[i].Kind == SimEventKind.Victory)
                    victoryCount++;
            }
            Assert.AreEqual(1, victoryCount);
        }
    }
}
