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
            var config = SystemAPI.GetSingleton<WaveConfig>();
            ref var wave = ref SystemAPI.GetSingletonRW<WaveState>().ValueRW;
            var events = SystemAPI.GetSingletonBuffer<SimEvent>();
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            float dt = SystemAPI.Time.DeltaTime;

            switch (wave.Phase)
            {
                case WavePhase.WaitingToStart:
                    wave.Timer -= dt;
                    if (wave.Timer <= 0f)
                    {
                        wave.Phase = WavePhase.Spawning;
                        wave.SpawnedThisWave = 0;
                        wave.Timer = 0f;
                    }
                    break;

                case WavePhase.Spawning:
                    wave.Timer -= dt;
                    while (wave.Timer <= 0f && wave.SpawnedThisWave < wave.EnemyAmount)
                    {
                        SpawnEnemy(ref wave, in config, ecb);
                        wave.SpawnedThisWave++;
                        wave.Timer += config.SpawnInterval;
                    }
                    if (wave.SpawnedThisWave >= wave.EnemyAmount && wave.Timer <= 0f)
                        wave.Phase = WavePhase.WaitingForClear;
                    break;

                case WavePhase.WaitingForClear:
                    if (wave.EnemiesAlive <= 0)
                    {
                        if (BalanceMath.ShouldUnlockTier(wave.Wave, config.WavesPerTier, wave.UnlockedTypes, config.MaxUnlockedTypes))
                        {
                            wave.UnlockedTypes++;
                            wave.EnemyAmount += config.AmountIncreasePerTier;
                        }
                        wave.Buff = BalanceMath.PickWeighted(ref config.BuffTable.Value.Weights, ref wave.Rng);
                        wave.Wave++;
                        wave.Phase = WavePhase.WaitingToStart;
                        wave.Timer = config.StartDelay;
                        events.Add(new SimEvent { Kind = SimEventKind.WaveChanged, IntValue = wave.Wave, Amount = (int)wave.Buff });
                    }
                    break;
            }
        }

        static void SpawnEnemy(ref WaveState wave, in WaveConfig config, EntityCommandBuffer ecb)
        {
            ref var roster = ref config.Roster.Value;
            int typeCount = math.clamp(wave.UnlockedTypes, 1, roster.Types.Length);
            if (roster.Types.Length == 0)
                return;

            int typeIndex = wave.Rng.NextInt(0, typeCount);
            bool bossWave = config.BossEveryWaves > 0 && wave.Wave % config.BossEveryWaves == 0;
            bool lastSpawn = wave.SpawnedThisWave == wave.EnemyAmount - 1;
            if (bossWave && lastSpawn && config.BossTypeIndex >= 0 && config.BossTypeIndex < roster.Types.Length)
                typeIndex = config.BossTypeIndex;

            var def = BalanceMath.ApplyBuff(roster.Types[typeIndex], wave.Buff, ref config.BuffTable.Value);
            def.Life *= BalanceMath.WaveLifeMultiplier(wave.Wave, config.LifeGrowthPerWave);

            var e = ecb.CreateEntity();
            ecb.AddComponent(e, LocalTransform.FromPosition(config.SpawnPosition));
            ecb.AddComponent<EnemyTag>(e);
            ecb.AddComponent<SimulationTag>(e);
            ecb.AddComponent(e, new Health { Value = def.Life, Max = def.Life });
            ecb.AddComponent(e, new Defense { Value = def.Defense });
            ecb.AddComponent(e, new MoveSpeed { Value = def.MoveSpeed });
            ecb.AddComponent(e, new ScoreValue { Value = def.Score });
            ecb.AddComponent(e, new PathFollower { NextIndex = 0 });
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
        }
    }
}
