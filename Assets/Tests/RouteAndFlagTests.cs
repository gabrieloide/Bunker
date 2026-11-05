using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Bunker.Simulation;

namespace Bunker.Tests
{
    // Allies marching along the player's flags, enemy routes that fork, waves leaving as groups, and the route overlay's data
    [TestFixture]
    public class RouteAndFlagTests
    {
        World world;
        EntityManager em;
        Entity simEntity;
        Entity configEntity;
        BlobAssetReference<AllyRosterBlob> allyRoster;

        [SetUp]
        public void SetUp()
        {
            world = new World("RouteTestWorld");
            em = world.EntityManager;
            world.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

            simEntity = em.CreateEntity();
            em.AddComponentData(simEntity, new WaveConfig());
            em.AddComponentData(simEntity, new WaveState { Phase = WavePhase.Spawning });
            em.AddComponentData(simEntity, new GameSession());
            em.AddBuffer<SimEvent>(simEntity);
            em.AddComponent<PrimaryPath>(simEntity);
            var path = em.AddBuffer<PathPoint>(simEntity);
            path.Add(new PathPoint { Value = new float3(50f, 0f, 0f) });
            path.Add(new PathPoint { Value = new float3(40f, 0f, 0f) });

            using var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<AllyRosterBlob>();
            var types = builder.Allocate(ref root.Types, 1);
            types[0] = new AllyTypeDef { Life = 10f, Damage = 1f, AttackInterval = 1f, AttackRange = 1f, MoveSpeed = 1f };
            allyRoster = builder.CreateBlobAssetReference<AllyRosterBlob>(Allocator.Persistent);

            configEntity = em.CreateEntity();
            em.AddComponentData(configEntity, new AllySpawnConfig { Roster = allyRoster });
            em.AddBuffer<AllySpawnRequest>(configEntity);
        }

        [TearDown]
        public void TearDown()
        {
            if (world.IsCreated) world.Dispose();
            if (allyRoster.IsCreated) allyRoster.Dispose();
        }

        // Flag 0 at the origin, with children 1 and 2 (a fork)
        void AddForkedFlags()
        {
            var nodes = em.AddBuffer<PlayerFlagNode>(configEntity);
            nodes.Add(new PlayerFlagNode { Position = float3.zero, Parent = -1 });
            nodes.Add(new PlayerFlagNode { Position = new float3(0f, 5f, 0f), Parent = 0 });
            nodes.Add(new PlayerFlagNode { Position = new float3(0f, -5f, 0f), Parent = 0 });
        }

        Entity CreateAlly(float3 position, int flagNode)
        {
            var ally = em.CreateEntity();
            em.AddComponent<AllyTag>(ally);
            em.AddComponentData(ally, LocalTransform.FromPosition(position));
            em.AddComponentData(ally, new Health { Value = 10f, Max = 10f });
            em.AddComponentData(ally, new MoveSpeed { Value = 1f });
            em.AddComponentData(ally, new Melee { Damage = 1f, Interval = 1f, Range = 1f });
            em.AddComponentData(ally, new AllyPathFollower { NextIndex = 1, OnFlags = true, FlagNode = flagNode });
            em.AddBuffer<DamageRequest>(ally);
            return ally;
        }

        void StepAllies(float dt)
        {
            world.SetTime(new Unity.Core.TimeData(0.0, dt));
            world.GetOrCreateSystem<AllySystem>().Update(world.Unmanaged);
        }

        void SetPhase(WavePhase phase)
        {
            var state = em.GetComponentData<WaveState>(simEntity);
            state.Phase = phase;
            em.SetComponentData(simEntity, state);
        }

        [Test]
        public void Allies_ArrivingAtAFork_TakeItsBranchesInTurn()
        {
            AddForkedFlags();
            var first = CreateAlly(float3.zero, 0);
            var second = CreateAlly(float3.zero, 0);

            StepAllies(0.01f);

            int a = em.GetComponentData<AllyPathFollower>(first).FlagNode;
            int b = em.GetComponentData<AllyPathFollower>(second).FlagNode;
            Assert.AreNotEqual(a, b, "the group splits between the two flags");
            Assert.That(a, Is.EqualTo(1).Or.EqualTo(2));
            Assert.That(b, Is.EqualTo(1).Or.EqualTo(2));
        }

        [Test]
        public void Ally_AtLastFlagDuringPreparation_WaitsThere()
        {
            AddForkedFlags();
            SetPhase(WavePhase.WaitingToStart);
            var ally = CreateAlly(new float3(0f, 5f, 0f), 1);

            StepAllies(1f);

            var follower = em.GetComponentData<AllyPathFollower>(ally);
            Assert.IsTrue(follower.OnFlags, "nowhere further to go: it stays on the flag");
            Assert.AreEqual(5f, em.GetComponentData<LocalTransform>(ally).Position.y, 1e-4f);
        }

        [Test]
        public void Allies_AtAFork_SkipADeadEndFlagWhileAChainContinues()
        {
            AddForkedFlags();
            em.GetBuffer<PlayerFlagNode>(configEntity).Add(new PlayerFlagNode { Position = new float3(0f, 9f, 0f), Parent = 1 });
            var first = CreateAlly(float3.zero, 0);
            var second = CreateAlly(float3.zero, 0);

            StepAllies(0.01f);

            Assert.AreEqual(1, em.GetComponentData<AllyPathFollower>(first).FlagNode, "flag 2 is only tower space");
            Assert.AreEqual(1, em.GetComponentData<AllyPathFollower>(second).FlagNode);
        }

        [Test]
        public void Ally_WaitingOnAFlag_DoesNotBlockAllyHeadingForAnotherBranch()
        {
            AddForkedFlags();
            var state = em.GetComponentData<WaveConfig>(simEntity);
            state.UnitSpacing = 2f;
            em.SetComponentData(simEntity, state);
            SetPhase(WavePhase.WaitingToStart);

            CreateAlly(new float3(0f, 5f, 0f), 1);
            var walker = CreateAlly(new float3(1f, 5f, 0f), 2);

            StepAllies(0.1f);

            Assert.AreNotEqual(1f, em.GetComponentData<LocalTransform>(walker).Position.x, "the flag it waits on is not this ally's way");
        }

        [Test]
        public void Ally_LeavingFlags_JoinsTheRouteAheadOfTheLeadingEnemy()
        {
            AddForkedFlags();
            // The route point nearest the flag is index 1, but the leading enemy has passed it
            var path = em.GetBuffer<PathPoint>(simEntity);
            path.Add(new PathPoint { Value = new float3(80f, 0f, 0f) });

            var enemy = em.CreateEntity();
            em.AddComponent<EnemyTag>(enemy);
            em.AddComponentData(enemy, LocalTransform.FromPosition(new float3(70f, 0f, 0f)));
            em.AddComponentData(enemy, new Health { Value = 10f, Max = 10f });
            em.AddComponentData(enemy, new PathFollower { NextIndex = 2 });
            em.AddBuffer<DamageRequest>(enemy);

            var ally = CreateAlly(new float3(0f, 5f, 0f), 1);
            StepAllies(0.1f);

            var follower = em.GetComponentData<AllyPathFollower>(ally);
            Assert.IsFalse(follower.OnFlags);
            Assert.AreEqual(2, follower.NextIndex);
        }

        [Test]
        public void Tower_DuringPreparation_DoesNotShootTheEnemyBase()
        {
            var enemyBase = em.CreateEntity();
            em.AddComponent<EnemyBaseTag>(enemyBase);
            em.AddComponentData(enemyBase, LocalTransform.FromPosition(new float3(3f, 0f, 0f)));
            em.AddComponentData(enemyBase, new Health { Value = 10f, Max = 10f });
            em.AddComponentData(enemyBase, new HitBox { HalfExtents = new float2(1f, 1f) });

            var tower = em.CreateEntity();
            em.AddComponentData(tower, LocalTransform.FromPosition(float3.zero));
            em.AddComponentData(tower, new Weapon { Range = 5f, FireInterval = 1f, ProjectileSpeed = 1f, TargetFaction = Faction.Enemy });
            em.AddComponentData(tower, new Target());

            var projectiles = em.CreateEntityQuery(typeof(Projectile));
            var targeting = world.GetOrCreateSystem<TargetingSystem>();
            var ecbSystem = world.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            world.SetTime(new Unity.Core.TimeData(0.0, 0.1f));

            SetPhase(WavePhase.WaitingToStart);
            targeting.Update(world.Unmanaged);
            ecbSystem.Update(world.Unmanaged);
            Assert.AreEqual(0, projectiles.CalculateEntityCount(), "no free hits between waves");

            SetPhase(WavePhase.Spawning);
            targeting.Update(world.Unmanaged);
            ecbSystem.Update(world.Unmanaged);
            Assert.AreEqual(1, projectiles.CalculateEntityCount(), "fires once the round runs");
            projectiles.Dispose();
        }

        [Test]
        public void Wave_GoesToWaitingForClearAsSoonAsTheLastGroupIsOut()
        {
            var enemyRoster = CreateEnemyRoster();
            var buffs = CreateBuffTable();
            em.SetComponentData(simEntity, new WaveConfig { SpawnInterval = 8f, GroupSize = 3, Roster = enemyRoster, BuffTable = buffs });
            em.SetComponentData(simEntity, new WaveState { Wave = 1, Phase = WavePhase.Spawning, EnemyAmount = 3, UnlockedTypes = 1, Rng = new Unity.Mathematics.Random(7) });

            world.SetTime(new Unity.Core.TimeData(0.0, 0.1f));
            world.GetOrCreateSystem<WaveSystem>().Update(world.Unmanaged);

            Assert.AreEqual(WavePhase.WaitingForClear, em.GetComponentData<WaveState>(simEntity).Phase);
            enemyRoster.Dispose();
            buffs.Dispose();
        }

        [Test]
        public void Ally_WaitingAtLastFlag_MovesOnWhenANewFlagIsPlanted()
        {
            AddForkedFlags();
            SetPhase(WavePhase.WaitingToStart);
            var ally = CreateAlly(new float3(0f, 5f, 0f), 1);
            StepAllies(0.1f);

            em.GetBuffer<PlayerFlagNode>(configEntity).Add(new PlayerFlagNode { Position = new float3(0f, 9f, 0f), Parent = 1 });
            StepAllies(0.1f);

            Assert.AreEqual(3, em.GetComponentData<AllyPathFollower>(ally).FlagNode);
        }

        [Test]
        public void Ally_AtLastFlagOnceTheRoundRuns_LeavesForTheClosestRoutePoint()
        {
            AddForkedFlags();
            var ally = CreateAlly(new float3(0f, 5f, 0f), 1);

            StepAllies(0.1f);

            var follower = em.GetComponentData<AllyPathFollower>(ally);
            Assert.IsFalse(follower.OnFlags);
            Assert.AreEqual(1, follower.NextIndex, "(40, 0) is closer than (50, 0)");
            Assert.AreEqual(simEntity, follower.PathEntity);
        }

        [Test]
        public void Ally_OffTheFlagsDuringPreparation_DoesNotWalk()
        {
            AddForkedFlags();
            SetPhase(WavePhase.WaitingToStart);
            var ally = CreateAlly(new float3(45f, 0f, 0f), 0);
            var follower = em.GetComponentData<AllyPathFollower>(ally);
            follower.OnFlags = false;
            em.SetComponentData(ally, follower);

            StepAllies(1f);

            Assert.AreEqual(45f, em.GetComponentData<LocalTransform>(ally).Position.x, 1e-4f);
        }

        [Test]
        public void Ally_DuringPreparation_DoesNotHitTheEnemyBase()
        {
            SetPhase(WavePhase.WaitingToStart);
            var enemyBase = em.CreateEntity();
            em.AddComponent<EnemyBaseTag>(enemyBase);
            em.AddComponentData(enemyBase, LocalTransform.FromPosition(new float3(45f, 0f, 0f)));
            em.AddComponentData(enemyBase, new Health { Value = 10f, Max = 10f });
            em.AddBuffer<DamageRequest>(enemyBase);

            var ally = CreateAlly(new float3(45.5f, 0f, 0f), 0);
            var follower = em.GetComponentData<AllyPathFollower>(ally);
            follower.OnFlags = false;
            follower.NextIndex = -1;
            em.SetComponentData(ally, follower);

            StepAllies(0.1f);

            Assert.AreEqual(0, em.GetBuffer<DamageRequest>(enemyBase).Length);
        }

        [Test]
        public void Ally_PastTheStartOfABranch_ContinuesOnTheRouteItForkedFrom()
        {
            var branch = em.CreateEntity();
            var points = em.AddBuffer<PathPoint>(branch);
            points.Add(new PathPoint { Value = new float3(30f, 0f, 0f) });
            em.AddComponentData(branch, new PathParent { Path = simEntity });

            var ally = CreateAlly(new float3(30f, 0f, 0f), 0);
            em.SetComponentData(ally, new AllyPathFollower { NextIndex = 0, PathEntity = branch, OnFlags = false });

            StepAllies(0.1f);

            var follower = em.GetComponentData<AllyPathFollower>(ally);
            Assert.AreEqual(simEntity, follower.PathEntity);
            Assert.AreEqual(1, follower.NextIndex, "the last point of the parent route");
        }

        [Test]
        public void Enemies_AtAFork_TakeItsBranchesInTurn()
        {
            var left = em.CreateEntity();
            em.AddBuffer<PathPoint>(left).Add(new PathPoint { Value = new float3(-5f, 0f, 0f) });
            var right = em.CreateEntity();
            em.AddBuffer<PathPoint>(right).Add(new PathPoint { Value = new float3(5f, 0f, 0f) });

            var fork = em.CreateEntity();
            em.AddBuffer<PathPoint>(fork).Add(new PathPoint { Value = float3.zero });
            em.AddComponentData(fork, new PathFork());
            var branches = em.AddBuffer<PathBranch>(fork);
            branches.Add(new PathBranch { Path = left });
            branches.Add(new PathBranch { Path = right });

            var first = CreateEnemyPastEnd(fork);
            var second = CreateEnemyPastEnd(fork);

            world.SetTime(new Unity.Core.TimeData(0.0, 0.01f));
            world.GetOrCreateSystem<EnemyMovementSystem>().Update(world.Unmanaged);

            var a = em.GetComponentData<PathFollower>(first);
            var b = em.GetComponentData<PathFollower>(second);
            Assert.AreNotEqual(a.PathEntity, b.PathEntity, "the group splits");
            Assert.AreEqual(0, a.NextIndex);
            Assert.AreEqual(0, b.NextIndex);
            world.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>().Update(world.Unmanaged);
            Assert.IsFalse(em.HasComponent<AtPathEnd>(first), "a fork is not the end of the road");
        }

        Entity CreateEnemyPastEnd(Entity path)
        {
            var enemy = em.CreateEntity();
            em.AddComponent<EnemyTag>(enemy);
            em.AddComponentData(enemy, LocalTransform.FromPosition(float3.zero));
            em.AddComponentData(enemy, new MoveSpeed { Value = 1f });
            em.AddComponentData(enemy, new PathFollower { NextIndex = 1, PathEntity = path });
            return enemy;
        }

        [Test]
        public void Wave_GroupLeavesFromOneSpawner_SpreadOverTheFirstFlag()
        {
            var routeA = em.CreateEntity();
            em.AddBuffer<PathPoint>(routeA).Add(new PathPoint { Value = new float3(0f, 20f, 0f), Radius = 2f });
            var routeB = em.CreateEntity();
            em.AddBuffer<PathPoint>(routeB).Add(new PathPoint { Value = new float3(20f, 0f, 0f), Radius = 2f });

            var spawners = em.AddBuffer<SpawnerPoint>(simEntity);
            spawners.Add(new SpawnerPoint { Position = new float3(0f, 20f, 0f), PathEntity = routeA });
            spawners.Add(new SpawnerPoint { Position = new float3(20f, 0f, 0f), PathEntity = routeB });

            var enemyRoster = CreateEnemyRoster();
            var buffs = CreateBuffTable();
            em.SetComponentData(simEntity, new WaveConfig { SpawnInterval = 5f, GroupSize = 3, Roster = enemyRoster, BuffTable = buffs });
            em.SetComponentData(simEntity, new WaveState { Wave = 1, Phase = WavePhase.Spawning, EnemyAmount = 6, UnlockedTypes = 1, Rng = new Unity.Mathematics.Random(7) });

            world.SetTime(new Unity.Core.TimeData(0.0, 0.1f));
            world.GetOrCreateSystem<WaveSystem>().Update(world.Unmanaged);
            world.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>().Update(world.Unmanaged);

            var query = em.CreateEntityQuery(typeof(EnemyTag), typeof(PathFollower), typeof(LocalTransform));
            var followers = query.ToComponentDataArray<PathFollower>(Allocator.Temp);
            var transforms = query.ToComponentDataArray<LocalTransform>(Allocator.Temp);

            Assert.AreEqual(3, followers.Length);
            var positions = new HashSet<float2>();
            for (int i = 0; i < followers.Length; i++)
            {
                Assert.AreEqual(routeA, followers[i].PathEntity, "the whole group takes the same route");
                Assert.LessOrEqual(math.distance(transforms[i].Position.xy, new float2(0f, 20f)), 2f + 1e-3f);
                positions.Add(transforms[i].Position.xy);
            }
            Assert.AreEqual(3, positions.Count, "nobody starts stacked on another");

            followers.Dispose();
            transforms.Dispose();
            query.Dispose();
            enemyRoster.Dispose();
            buffs.Dispose();
        }

        static BlobAssetReference<EnemyRosterBlob> CreateEnemyRoster()
        {
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<EnemyRosterBlob>();
            var types = builder.Allocate(ref root.Types, 1);
            types[0] = new EnemyTypeDef { Life = 10f, MoveSpeed = 1f };
            return builder.CreateBlobAssetReference<EnemyRosterBlob>(Allocator.Persistent);
        }

        static BlobAssetReference<EnemyBuffTableBlob> CreateBuffTable()
        {
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<EnemyBuffTableBlob>();
            var weights = builder.Allocate(ref root.Weights, 6);
            weights[0] = 1f;
            root.LifeMultiplier = 1f;
            root.DamageMultiplier = 1f;
            root.FireRateMultiplier = 1f;
            root.DefenseMultiplier = 1f;
            root.SpeedMultiplier = 1f;
            return builder.CreateBlobAssetReference<EnemyBuffTableBlob>(Allocator.Persistent);
        }

        [Test]
        public void RouteGeometry_CornerOnlyForPointsOutOfLine()
        {
            Assert.IsFalse(RouteGeometry.TryGetCorner(Vector3.zero, new Vector3(5f, 0f, 0f), false, out _));

            Assert.IsTrue(RouteGeometry.TryGetCorner(Vector3.zero, new Vector3(5f, 3f, 0f), false, out Vector3 horizontalFirst));
            Assert.AreEqual(new Vector3(5f, 0f, 0f), horizontalFirst);

            Assert.IsTrue(RouteGeometry.TryGetCorner(Vector3.zero, new Vector3(5f, 3f, 0f), true, out Vector3 verticalFirst));
            Assert.AreEqual(new Vector3(0f, 3f, 0f), verticalFirst);
        }

        [Test]
        public void RouteGeometry_StripCarriesDistanceWalkedAlongCorners()
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();

            RouteGeometry.AppendStrip(new[] { Vector3.zero, new Vector3(4f, 0f, 0f), new Vector3(4f, 3f, 0f) }, 0.5f, vertices, uvs, triangles);

            Assert.AreEqual(8, vertices.Count);
            Assert.AreEqual(12, triangles.Count);
            Assert.AreEqual(7f, uvs[7].x, 1e-4f, "4 + 3 walked at the end of the second leg");
            Assert.AreEqual(3.5f, vertices[4].x, 1e-4f, "the second leg is offset sideways");
        }

        [Test]
        public void RouteOverlayMaterials_LoadWithTheirShaders()
        {
            var line = Resources.Load<Material>("RouteLine");
            Assert.IsNotNull(line);
            Assert.AreEqual("Bunker/RouteLine", line.shader.name);
            Assert.IsFalse(UnityEditor.ShaderUtil.ShaderHasError(line.shader));

            var ring = Resources.Load<Material>("EnemyFlagRing");
            Assert.IsNotNull(ring);
            Assert.AreEqual("Bunker/DottedRing", ring.shader.name);
        }
    }
}
