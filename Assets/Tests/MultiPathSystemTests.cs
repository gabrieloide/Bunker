using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Bunker.Simulation;

namespace Bunker.Tests
{
    [TestFixture]
    public class MultiPathSystemTests
    {
        World world;
        EntityManager em;
        SystemHandle movementSystem;
        Entity simEntity;
        Entity secondaryPathEntity;

        [SetUp]
        public void SetUp()
        {
            world = new World("MultiPathTestWorld");
            em = world.EntityManager;
            world.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

            simEntity = em.CreateEntity();
            em.AddComponentData(simEntity, new WaveConfig());
            em.AddComponentData(simEntity, new WaveState());
            em.AddComponentData(simEntity, new GameSession());
            em.AddBuffer<SimEvent>(simEntity);
            em.AddComponent<PrimaryPath>(simEntity);

            // Default path (North path): (0, 10) -> (0, 0)
            var defaultPath = em.AddBuffer<PathPoint>(simEntity);
            defaultPath.Add(new PathPoint { Value = new float3(0f, 10f, 0f) });
            defaultPath.Add(new PathPoint { Value = new float3(0f, 0f, 0f) });

            // Secondary path (East path): (10, 0) -> (0, 0)
            secondaryPathEntity = em.CreateEntity();
            em.AddComponent<SimulationTag>(secondaryPathEntity);
            var secPath = em.AddBuffer<PathPoint>(secondaryPathEntity);
            secPath.Add(new PathPoint { Value = new float3(10f, 0f, 0f) });
            secPath.Add(new PathPoint { Value = new float3(0f, 0f, 0f) });

            movementSystem = world.GetOrCreateSystem<EnemyMovementSystem>();
        }

        [TearDown]
        public void TearDown()
        {
            if (world.IsCreated) world.Dispose();
        }

        [Test]
        public void EnemyMovement_WithDifferentPaths_EachEnemyFollowsItsAssignedPath()
        {
            // Enemy 1 on North path (default)
            var enemyNorth = em.CreateEntity();
            em.AddComponent<EnemyTag>(enemyNorth);
            em.AddComponent<SimulationTag>(enemyNorth);
            em.AddComponentData(enemyNorth, LocalTransform.FromPosition(new float3(0f, 10f, 0f)));
            em.AddComponentData(enemyNorth, new MoveSpeed { Value = 5f });
            em.AddComponentData(enemyNorth, new PathFollower { NextIndex = 1, PathEntity = Entity.Null });

            // Enemy 2 on East path (secondary)
            var enemyEast = em.CreateEntity();
            em.AddComponent<EnemyTag>(enemyEast);
            em.AddComponent<SimulationTag>(enemyEast);
            em.AddComponentData(enemyEast, LocalTransform.FromPosition(new float3(10f, 0f, 0f)));
            em.AddComponentData(enemyEast, new MoveSpeed { Value = 5f });
            em.AddComponentData(enemyEast, new PathFollower { NextIndex = 1, PathEntity = secondaryPathEntity });

            // Step 1 second: North enemy moves towards (0, 0) along Y; East enemy moves towards (0, 0) along X
            world.SetTime(new Unity.Core.TimeData(1.0, 1.0f));
            movementSystem.Update(world.Unmanaged);

            var posNorth = em.GetComponentData<LocalTransform>(enemyNorth).Position;
            var posEast = em.GetComponentData<LocalTransform>(enemyEast).Position;

            // North enemy should be at (0, 5, 0)
            Assert.AreEqual(0f, posNorth.x, 0.05f);
            Assert.AreEqual(5f, posNorth.y, 0.05f);

            // East enemy should be at (5, 0, 0)
            Assert.AreEqual(5f, posEast.x, 0.05f);
            Assert.AreEqual(0f, posEast.y, 0.05f);
        }

        [Test]
        public void WaveSystem_WithMultipleSpawners_AlternatesSpawnLocationsAndPaths()
        {
            var spawners = em.AddBuffer<SpawnerPoint>(simEntity);
            spawners.Add(new SpawnerPoint { Position = new float3(0f, 20f, 0f), PathEntity = simEntity });
            spawners.Add(new SpawnerPoint { Position = new float3(20f, 0f, 0f), PathEntity = secondaryPathEntity });

            var waveSystem = world.GetOrCreateSystem<WaveSystem>();

            using var builder = new BlobBuilder(Unity.Collections.Allocator.Temp);
            ref var root = ref builder.ConstructRoot<EnemyRosterBlob>();
            var types = builder.Allocate(ref root.Types, 1);
            types[0] = new EnemyTypeDef
            {
                Life = 10f,
                MoveSpeed = 1f,
                Damage = 1f,
                FireInterval = 1f,
                AttackRange = 1f,
                Score = 1,
                HitHalfExtents = new float2(0.5f, 0.5f)
            };
            var roster = builder.CreateBlobAssetReference<EnemyRosterBlob>(Unity.Collections.Allocator.Persistent);

            using var buffBuilder = new BlobBuilder(Unity.Collections.Allocator.Temp);
            ref var buffRoot = ref buffBuilder.ConstructRoot<EnemyBuffTableBlob>();
            var weights = buffBuilder.Allocate(ref buffRoot.Weights, 6);
            for (int i = 0; i < 6; i++) weights[i] = i == 0 ? 100f : 0f;
            buffRoot.LifeMultiplier = 1f;
            buffRoot.DamageMultiplier = 1f;
            buffRoot.FireRateMultiplier = 1f;
            buffRoot.DefenseMultiplier = 1f;
            buffRoot.SpeedMultiplier = 1f;
            var buffTable = buffBuilder.CreateBlobAssetReference<EnemyBuffTableBlob>(Unity.Collections.Allocator.Persistent);

            em.SetComponentData(simEntity, new WaveConfig
            {
                StartDelay = 0f,
                SpawnInterval = 0.5f,
                InitialEnemyAmount = 4,
                WavesPerTier = 10,
                InitialUnlockedTypes = 1,
                MaxUnlockedTypes = 1,
                Roster = roster,
                BuffTable = buffTable
            });

            em.SetComponentData(simEntity, new WaveState
            {
                Wave = 1,
                Phase = WavePhase.Spawning,
                EnemyAmount = 4,
                SpawnedThisWave = 0,
                Timer = 0f,
                UnlockedTypes = 1,
                Rng = new Unity.Mathematics.Random(1234)
            });

            // Update to spawn 2 enemies
            world.SetTime(new Unity.Core.TimeData(0.6, 0.6f));
            waveSystem.Update(world.Unmanaged);
            world.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>().Update(world.Unmanaged);

            var query = em.CreateEntityQuery(ComponentType.ReadOnly<EnemyTag>(), ComponentType.ReadOnly<LocalTransform>(), ComponentType.ReadOnly<PathFollower>());
            var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);

            Assert.AreEqual(2, entities.Length);

            var e0 = entities[0];
            var e1 = entities[1];

            var pos0 = em.GetComponentData<LocalTransform>(e0).Position;
            var pos1 = em.GetComponentData<LocalTransform>(e1).Position;
            var f0 = em.GetComponentData<PathFollower>(e0);
            var f1 = em.GetComponentData<PathFollower>(e1);

            // Spawner 0 (North) vs Spawner 1 (East)
            Assert.IsTrue((math.distance(pos0, new float3(0f, 20f, 0f)) < 0.1f && math.distance(pos1, new float3(20f, 0f, 0f)) < 0.1f) ||
                          (math.distance(pos1, new float3(0f, 20f, 0f)) < 0.1f && math.distance(pos0, new float3(20f, 0f, 0f)) < 0.1f));

            Assert.IsTrue((f0.PathEntity == simEntity && f1.PathEntity == secondaryPathEntity) ||
                          (f1.PathEntity == simEntity && f0.PathEntity == secondaryPathEntity));

            entities.Dispose();
            query.Dispose();
            roster.Dispose();
            buffTable.Dispose();
        }
    }
}
