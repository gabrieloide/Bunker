using System.Collections.Generic;
using Bunker.Simulation;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Bunker.Tests
{
    [TestFixture]
    public class AllyRoadTests
    {
        [Test]
        public void PlanSegment_RespectsPerCardAndLevelTileLimits()
        {
            var built = new HashSet<Vector3Int>();
            int levelMax = 6;

            bool firstValid = AllyRoadPlanner.TryPlanSegment(
                Vector3Int.zero, built, new Vector3Int(8, 0, 0),
                maxCardTiles: 5, remainingLevelTiles: levelMax - built.Count,
                _ => true, out var first, out _);
            Assert.IsTrue(firstValid);
            Assert.AreEqual(5, first.Count);
            built.UnionWith(first);

            bool secondValid = AllyRoadPlanner.TryPlanSegment(
                Vector3Int.zero, built, new Vector3Int(8, 0, 0),
                maxCardTiles: 5, remainingLevelTiles: levelMax - built.Count,
                _ => true, out var second, out _);
            Assert.IsTrue(secondValid);
            Assert.AreEqual(1, second.Count);
            built.UnionWith(second);

            bool thirdValid = AllyRoadPlanner.TryPlanSegment(
                Vector3Int.zero, built, new Vector3Int(10, 0, 0),
                maxCardTiles: 5, remainingLevelTiles: levelMax - built.Count,
                _ => true, out _, out _);
            Assert.IsFalse(thirdValid);
        }

        [Test]
        public void PlanSegment_FallsBackToAlternateLCornerWhenPreferredIsBlocked()
        {
            var built = new HashSet<Vector3Int>();

            bool valid = AllyRoadPlanner.TryPlanSegment(
                Vector3Int.zero, built, new Vector3Int(2, 2, 0),
                maxCardTiles: 5, remainingLevelTiles: 12,
                cell => cell != new Vector3Int(1, 0, 0),
                out var cells, out _);

            Assert.IsTrue(valid);
            Assert.AreEqual(4, cells.Count);
            Assert.AreEqual(new Vector3Int(0, 1, 0), cells[0]);
            Assert.AreEqual(new Vector3Int(2, 2, 0), cells[cells.Count - 1]);
        }

        [Test]
        public void PlanSegment_RejectsSolidTwoByTwoBlob()
        {
            var built = new HashSet<Vector3Int>
            {
                new Vector3Int(1, 0, 0),
                new Vector3Int(1, 1, 0),
                new Vector3Int(0, 1, 0)
            };

            bool valid = AllyRoadPlanner.TryPlanSegment(
                Vector3Int.zero, built, new Vector3Int(2, 1, 0),
                maxCardTiles: 5, remainingLevelTiles: 12,
                _ => true, out var cells, out _);

            Assert.IsTrue(valid);
            Assert.IsFalse(cells.Contains(new Vector3Int(2, 0, 0)) && cells.Contains(new Vector3Int(2, 1, 0)));
        }

        [Test]
        public void AllySystem_UsesPreferredAllyRoad_AndSkipsItForDefaultRoundRobin()
        {
            using var world = new World("AllyRoadSimTest");
            var em = world.EntityManager;
            world.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

            var simEntity = em.CreateEntity();
            em.AddComponentData(simEntity, new WaveConfig());
            em.AddComponentData(simEntity, new WaveState { Phase = WavePhase.Spawning });
            em.AddComponentData(simEntity, new GameSession());
            em.AddBuffer<SimEvent>(simEntity);
            em.AddComponent<PrimaryPath>(simEntity);
            var enemyPath = em.AddBuffer<PathPoint>(simEntity);
            enemyPath.Add(new PathPoint { Value = new float3(50f, 0f, 0f) });
            enemyPath.Add(new PathPoint { Value = new float3(10f, 0f, 0f) });

            var customRoad = em.CreateEntity();
            em.AddComponent<AllyRoadTag>(customRoad);
            var customPath = em.AddBuffer<PathPoint>(customRoad);
            customPath.Add(new PathPoint { Value = new float3(0f, 10f, 0f) });
            customPath.Add(new PathPoint { Value = new float3(0f, 0f, 0f) });

            using var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<AllyRosterBlob>();
            var types = builder.Allocate(ref root.Types, 1);
            types[0] = new AllyTypeDef { Life = 10f, Damage = 1f, AttackInterval = 1f, AttackRange = 1f, MoveSpeed = 1f };
            using var roster = builder.CreateBlobAssetReference<AllyRosterBlob>(Allocator.Persistent);

            var configEntity = em.CreateEntity();
            em.AddComponentData(configEntity, new AllySpawnConfig { Roster = roster });
            var requests = em.AddBuffer<AllySpawnRequest>(configEntity);
            requests.Add(new AllySpawnRequest { TypeIndex = 0, PreferredPath = Entity.Null });
            requests.Add(new AllySpawnRequest { TypeIndex = 0, PreferredPath = customRoad });

            world.SetTime(new Unity.Core.TimeData(0.0, 0f));
            world.GetOrCreateSystem<AllySystem>().Update(world.Unmanaged);
            world.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>().Update(world.Unmanaged);

            var query = em.CreateEntityQuery(typeof(AllyTag), typeof(AllyPathFollower));
            using var followers = query.ToComponentDataArray<AllyPathFollower>(Allocator.Temp);
            Assert.AreEqual(2, followers.Length);
            Assert.AreEqual(simEntity, followers[0].PathEntity);
            Assert.AreEqual(customRoad, followers[1].PathEntity);
        }

        [Test]
        public void PlanSegment_BranchesFromNearestExistingRoadCell()
        {
            var built = new HashSet<Vector3Int>
            {
                new Vector3Int(1, 0, 0),
                new Vector3Int(2, 0, 0),
                new Vector3Int(3, 0, 0)
            };

            bool valid = AllyRoadPlanner.TryPlanSegment(
                Vector3Int.zero, built, new Vector3Int(3, 3, 0),
                maxCardTiles: 5, remainingLevelTiles: 12,
                _ => true, out var cells, out var anchor);

            Assert.IsTrue(valid);
            Assert.AreEqual(new Vector3Int(3, 0, 0), anchor);
            Assert.AreEqual(3, cells.Count);
            Assert.AreEqual(new Vector3Int(3, 3, 0), cells[cells.Count - 1]);
        }

        [Test]
        public void PlanSegment_ReturnsFalse_WhenBlockedOrZeroBudget()
        {
            var built = new HashSet<Vector3Int>();

            bool zeroBudget = AllyRoadPlanner.TryPlanSegment(
                Vector3Int.zero, built, new Vector3Int(3, 0, 0),
                maxCardTiles: 5, remainingLevelTiles: 0,
                _ => true, out var emptyA, out _);
            Assert.IsFalse(zeroBudget);
            Assert.AreEqual(0, emptyA.Count);

            bool blocked = AllyRoadPlanner.TryPlanSegment(
                Vector3Int.zero, built, new Vector3Int(3, 0, 0),
                maxCardTiles: 5, remainingLevelTiles: 10,
                _ => false, out var emptyB, out _);
            Assert.IsFalse(blocked);
            Assert.AreEqual(0, emptyB.Count);
        }

        [Test]
        public void AllySystem_AllyOnCustomRoad_WalksToTipAndHitsEnemyBase()
        {
            using var world = new World("AllyRoadBaseAttackTest");
            var em = world.EntityManager;
            world.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

            var simEntity = em.CreateEntity();
            em.AddComponentData(simEntity, new WaveConfig());
            em.AddComponentData(simEntity, new WaveState { Phase = WavePhase.Spawning });
            em.AddComponentData(simEntity, new GameSession());
            em.AddBuffer<SimEvent>(simEntity);

            var customRoad = em.CreateEntity();
            em.AddComponent<AllyRoadTag>(customRoad);
            var customPath = em.AddBuffer<PathPoint>(customRoad);
            customPath.Add(new PathPoint { Value = new float3(0f, 5f, 0f) });
            customPath.Add(new PathPoint { Value = new float3(0f, 0f, 0f) });

            var enemyBase = em.CreateEntity();
            em.AddComponent<EnemyBaseTag>(enemyBase);
            em.AddComponentData(enemyBase, LocalTransform.FromPosition(new float3(0f, 8f, 0f)));
            em.AddComponentData(enemyBase, new Health { Value = 50f, Max = 50f });
            em.AddComponentData(enemyBase, new HitBox { HalfExtents = new float2(1f, 1f) });
            em.AddBuffer<DamageRequest>(enemyBase);

            using var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<AllyRosterBlob>();
            var types = builder.Allocate(ref root.Types, 1);
            types[0] = new AllyTypeDef { Life = 20f, Damage = 5f, AttackInterval = 0.5f, AttackRange = 1.5f, MoveSpeed = 10f };
            using var roster = builder.CreateBlobAssetReference<AllyRosterBlob>(Allocator.Persistent);

            var configEntity = em.CreateEntity();
            em.AddComponentData(configEntity, new AllySpawnConfig { SpawnPosition = float3.zero, Roster = roster });
            var requests = em.AddBuffer<AllySpawnRequest>(configEntity);
            requests.Add(new AllySpawnRequest { TypeIndex = 0, PreferredPath = customRoad });

            var allySystem = world.GetOrCreateSystem<AllySystem>();
            var ecbSystem = world.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

            // Step simulation so ally spawns, walks (0,0)->(0,5)->base at (0,8), and deals damage
            for (int i = 0; i < 20; i++)
            {
                world.SetTime(new Unity.Core.TimeData(i * 0.1, 0.1f));
                allySystem.Update(world.Unmanaged);
                ecbSystem.Update(world.Unmanaged);
            }

            var damageBuffer = em.GetBuffer<DamageRequest>(enemyBase);
            Assert.Greater(damageBuffer.Length, 0);
            Assert.AreEqual(5f, damageBuffer[0].Damage);
        }

        [Test]
        public void AllySystem_AllyOnUnfinishedRoad_ReversesAndPatrolsInsteadOfCrossingMap()
        {
            using var world = new World("AllyRoadPatrolTest");
            var em = world.EntityManager;
            world.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

            var simEntity = em.CreateEntity();
            em.AddComponentData(simEntity, new WaveConfig());
            em.AddComponentData(simEntity, new WaveState { Phase = WavePhase.Spawning });
            em.AddComponentData(simEntity, new GameSession());
            em.AddBuffer<SimEvent>(simEntity);

            // Unfinished road: only goes from (0,0) to (0,4), while enemy base is far away at (0,30)
            var customRoad = em.CreateEntity();
            em.AddComponent<AllyRoadTag>(customRoad);
            var customPath = em.AddBuffer<PathPoint>(customRoad);
            customPath.Add(new PathPoint { Value = new float3(0f, 4f, 0f) });
            customPath.Add(new PathPoint { Value = new float3(0f, 0f, 0f) });

            var enemyBase = em.CreateEntity();
            em.AddComponent<EnemyBaseTag>(enemyBase);
            em.AddComponentData(enemyBase, LocalTransform.FromPosition(new float3(0f, 30f, 0f)));
            em.AddComponentData(enemyBase, new Health { Value = 50f, Max = 50f });
            em.AddComponentData(enemyBase, new HitBox { HalfExtents = new float2(1f, 1f) });
            em.AddBuffer<DamageRequest>(enemyBase);

            using var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<AllyRosterBlob>();
            var types = builder.Allocate(ref root.Types, 1);
            types[0] = new AllyTypeDef { Life = 20f, Damage = 5f, AttackInterval = 0.5f, AttackRange = 1.5f, MoveSpeed = 10f };
            using var roster = builder.CreateBlobAssetReference<AllyRosterBlob>(Allocator.Persistent);

            var configEntity = em.CreateEntity();
            em.AddComponentData(configEntity, new AllySpawnConfig { SpawnPosition = float3.zero, Roster = roster });
            var requests = em.AddBuffer<AllySpawnRequest>(configEntity);
            requests.Add(new AllySpawnRequest { TypeIndex = 0, PreferredPath = customRoad });

            var allySystem = world.GetOrCreateSystem<AllySystem>();
            var ecbSystem = world.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

            for (int i = 0; i < 15; i++)
            {
                world.SetTime(new Unity.Core.TimeData(i * 0.1, 0.1f));
                allySystem.Update(world.Unmanaged);
                ecbSystem.Update(world.Unmanaged);
            }

            var query = em.CreateEntityQuery(typeof(AllyTag), typeof(LocalTransform), typeof(AllyPathFollower));
            using var transforms = query.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            using var followers = query.ToComponentDataArray<AllyPathFollower>(Allocator.Temp);
            Assert.AreEqual(1, transforms.Length);
            Assert.LessOrEqual(transforms[0].Position.y, 4.1f);
            Assert.AreEqual(1, followers[0].Reversing);
            Assert.AreEqual(0, em.GetBuffer<DamageRequest>(enemyBase).Length);
        }
    }
}
