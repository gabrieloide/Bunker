using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Bunker.Simulation
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial struct WaveSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<WaveConfig>();
            state.RequireForUpdate<WaveState>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.TryGetSingleton<GameSession>(out var session) && session.IsGameOver)
                return;

            var config = SystemAPI.GetSingleton<WaveConfig>();
            ref var wave = ref SystemAPI.GetSingletonRW<WaveState>().ValueRW;
            var events = SystemAPI.GetSingletonBuffer<SimEvent>();
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            float dt = SystemAPI.Time.DeltaTime;
            bool hasSpawners = SystemAPI.TryGetSingletonBuffer<SpawnerPoint>(out var spawners, true);

            switch (wave.Phase)
            {
                case WavePhase.WaitingToStart:
                    if (wave.SpawnedThisWave == 0 && wave.EnemyAmount > 0)
                    {
                        float firstEnemySpeed = SpawnEnemy(ref wave, in config, ecb, spawners, hasSpawners);
                        wave.SpawnedThisWave = 1;
                        if (config.FlagZoneDistanceAlongPath > 0.05f)
                        {
                            wave.Timer = config.FlagZoneDistanceAlongPath / math.max(0.01f, firstEnemySpeed);
                        }
                        else
                        {
                            wave.Timer = config.StartDelay;
                        }
                    }

                    var pathLookup = SystemAPI.GetBufferLookup<PathPoint>(true);
                    Entity defaultPathEntity = Entity.Null;
                    if (SystemAPI.TryGetSingletonEntity<PrimaryPath>(out var primary))
                        defaultPathEntity = primary;

                    float maxDistanceTraveled = -1f;
                    float leadSpeed = 0f;
                    bool foundLeadEnemy = false;

                    foreach (var (transform, follower, speed) in
                             SystemAPI.Query<RefRO<LocalTransform>, RefRO<PathFollower>, RefRO<MoveSpeed>>()
                                 .WithAll<EnemyTag>())
                    {
                        Entity targetPathEntity = follower.ValueRO.PathEntity != Entity.Null ? follower.ValueRO.PathEntity : defaultPathEntity;
                        if (targetPathEntity == Entity.Null || !pathLookup.HasBuffer(targetPathEntity))
                            continue;

                        var path = pathLookup[targetPathEntity];
                        float3 spawnerPos = GetSpawnerPosition(targetPathEntity, spawners, hasSpawners, config.SpawnPosition);
                        float dist = CalculateDistanceTraveled(spawnerPos, path, follower.ValueRO.NextIndex, transform.ValueRO.Position);
                        if (dist > maxDistanceTraveled)
                        {
                            maxDistanceTraveled = dist;
                            leadSpeed = speed.ValueRO.Value;
                            foundLeadEnemy = true;
                        }
                    }

                    if (config.FlagZoneDistanceAlongPath > 0.05f)
                    {
                        if (foundLeadEnemy)
                        {
                            float distanceRemaining = config.FlagZoneDistanceAlongPath - maxDistanceTraveled;
                            if (distanceRemaining <= 0.05f || (wave.SpawnedThisWave >= 1 && wave.EnemiesAlive <= 0))
                            {
                                StartSpawningPhase(ref wave, in config, events);
                            }
                            else
                            {
                                wave.Timer = distanceRemaining / math.max(0.01f, leadSpeed);
                            }
                        }
                        else if (wave.SpawnedThisWave >= 1 && wave.EnemiesAlive <= 0)
                        {
                            StartSpawningPhase(ref wave, in config, events);
                        }
                    }
                    else
                    {
                        wave.Timer -= dt;
                        if (wave.Timer <= 0f || (wave.SpawnedThisWave >= 1 && wave.EnemiesAlive <= 0))
                        {
                            StartSpawningPhase(ref wave, in config, events);
                        }
                    }
                    break;

                case WavePhase.Spawning:
                    wave.Timer -= dt;
                    while (wave.Timer <= 0f && wave.SpawnedThisWave < wave.EnemyAmount)
                    {
                        SpawnEnemy(ref wave, in config, ecb, spawners, hasSpawners);
                        wave.SpawnedThisWave++;
                        bool groupDone = config.GroupSize <= 1 || wave.SpawnedThisWave % config.GroupSize == 0;
                        wave.Timer += groupDone ? config.SpawnInterval : config.GroupGap;
                    }
                    if (wave.SpawnedThisWave >= wave.EnemyAmount && wave.Timer <= 0f)
                    {
                        wave.Phase = WavePhase.WaitingForClear;
                        events.Add(new SimEvent { Kind = SimEventKind.WavePhaseChanged, IntValue = (int)wave.Phase, Amount = wave.Wave });
                    }
                    break;

                case WavePhase.WaitingForClear:
                    if (wave.EnemiesAlive <= 0)
                    {
                        if (BalanceMath.ShouldUnlockTier(wave.Wave, config.WavesPerTier, wave.UnlockedTypes, config.MaxUnlockedTypes))
                        {
                            wave.UnlockedTypes++;
                            wave.EnemyAmount += config.AmountIncreasePerTier;
                        }
                        wave.EnemyAmount += config.AmountIncreasePerWave;
                        wave.Buff = BalanceMath.PickWeighted(ref config.BuffTable.Value.Weights, ref wave.Rng);
                        wave.Wave++;
                        wave.Phase = WavePhase.WaitingToStart;
                        wave.Timer = config.StartDelay;
                        wave.SpawnedThisWave = 0;
                        events.Add(new SimEvent { Kind = SimEventKind.WaveChanged, IntValue = wave.Wave, Amount = (int)wave.Buff });
                        events.Add(new SimEvent { Kind = SimEventKind.WavePhaseChanged, IntValue = (int)wave.Phase, Amount = wave.Wave });
                    }
                    break;
            }
        }

        static void StartSpawningPhase(ref WaveState wave, in WaveConfig config, DynamicBuffer<SimEvent> events)
        {
            wave.Phase = WavePhase.Spawning;
            wave.Timer = config.SpawnInterval;
            events.Add(new SimEvent { Kind = SimEventKind.WavePhaseChanged, IntValue = (int)wave.Phase, Amount = wave.Wave });
        }

        static float3 GetSpawnerPosition(Entity targetPathEntity, DynamicBuffer<SpawnerPoint> spawners, bool hasSpawners, float3 fallback)
        {
            if (hasSpawners && spawners.Length > 0)
            {
                for (int i = 0; i < spawners.Length; i++)
                {
                    if (spawners[i].PathEntity == targetPathEntity)
                        return spawners[i].Position;
                }
                return spawners[0].Position;
            }
            return fallback;
        }

        static float CalculateDistanceTraveled(float3 spawnPos, DynamicBuffer<PathPoint> path, int nextIndex, float3 enemyPos)
        {
            if (path.Length == 0)
                return math.distance(spawnPos, enemyPos);

            if (nextIndex <= 0)
                return math.distance(spawnPos, enemyPos);

            float distance = math.distance(spawnPos, path[0].Value);
            int upTo = math.min(nextIndex, path.Length);
            for (int i = 1; i < upTo; i++)
            {
                distance += math.distance(path[i - 1].Value, path[i].Value);
            }

            int prevIndex = nextIndex - 1;
            if (prevIndex < path.Length)
            {
                distance += math.distance(path[prevIndex].Value, enemyPos);
            }

            return distance;
        }

        static float SpawnEnemy(ref WaveState wave, in WaveConfig config, EntityCommandBuffer ecb, DynamicBuffer<SpawnerPoint> spawners, bool hasSpawners)
        {
            ref var roster = ref config.Roster.Value;
            int typeCount = math.clamp(wave.UnlockedTypes, 1, roster.Types.Length);
            if (roster.Types.Length == 0)
                return 1f;

            int typeIndex = wave.Rng.NextInt(0, typeCount);
            bool bossWave = config.BossEveryWaves > 0 && wave.Wave % config.BossEveryWaves == 0;
            bool lastSpawn = wave.SpawnedThisWave == wave.EnemyAmount - 1;
            if (bossWave && lastSpawn && config.BossTypeIndex >= 0 && config.BossTypeIndex < roster.Types.Length)
                typeIndex = config.BossTypeIndex;

            var def = BalanceMath.ApplyBuff(roster.Types[typeIndex], wave.Buff, ref config.BuffTable.Value);
            def.Life *= BalanceMath.WaveLifeMultiplier(wave.Wave, config.LifeGrowthPerWave);
            if (config.EnemySpeedMultiplier > 0f) // a default-initialised config keeps the base speed
                def.MoveSpeed *= config.EnemySpeedMultiplier;

            float3 spawnPos = config.SpawnPosition;
            Entity pathEntity = Entity.Null;
            if (hasSpawners && spawners.Length > 0)
            {
                int spawnerIndex = wave.SpawnedThisWave % spawners.Length;
                spawnPos = spawners[spawnerIndex].Position;
                pathEntity = spawners[spawnerIndex].PathEntity;
            }

            var e = ecb.CreateEntity();
            ecb.AddComponent(e, LocalTransform.FromPosition(spawnPos));
            ecb.AddComponent<EnemyTag>(e);
            ecb.AddComponent<SimulationTag>(e);
            ecb.AddComponent(e, new Health { Value = def.Life, Max = def.Life });
            ecb.AddComponent(e, new Defense { Value = def.Defense });
            ecb.AddComponent(e, new MoveSpeed { Value = def.MoveSpeed });
            ecb.AddComponent(e, new ScoreValue { Value = def.Score });
            ecb.AddComponent(e, new PathFollower { NextIndex = 0, PathEntity = pathEntity });
            ecb.AddComponent(e, new HitBox { HalfExtents = def.HitHalfExtents, Offset = def.HitOffset });
            ecb.AddComponent(e, new Weapon
            {
                Damage = def.Damage,
                BulletPen = 0f,
                FireInterval = def.FireInterval,
                Range = def.AttackRange,
                Cooldown = def.FireInterval,
                ProjectileSpeed = config.EnemyProjectileSpeed,
                ProjectileLifetime = config.EnemyProjectileLifetime,
                ProjectileHalfExtents = config.EnemyProjectileHalfExtents,
                MuzzleOffset = float3.zero,
                TargetFaction = Faction.Tower,
                HasBuff = false
            });
            ecb.AddComponent(e, new Target { Value = Entity.Null });
            ecb.AddBuffer<DamageRequest>(e);
            ecb.AddComponent(e, new NeedsView { Kind = ViewKind.Enemy, PrefabId = typeIndex });

            wave.EnemiesAlive++;
            return def.MoveSpeed;
        }
    }
}
