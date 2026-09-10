using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Bunker.Simulation
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PlayerAttackSystem))]
    public partial struct DamageSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<WaveState>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            ref var wave = ref SystemAPI.GetSingletonRW<WaveState>().ValueRW;
            var events = SystemAPI.GetSingletonBuffer<SimEvent>();
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            bool hasSession = SystemAPI.HasSingleton<GameSession>();

            foreach (var (health, requests, defense, score, transform, entity) in
                     SystemAPI.Query<RefRW<Health>, DynamicBuffer<DamageRequest>, RefRO<Defense>, RefRO<ScoreValue>, RefRO<LocalTransform>>()
                         .WithAll<EnemyTag>()
                         .WithEntityAccess())
            {
                if (requests.Length == 0)
                    continue;

                float total = 0f;
                for (int i = 0; i < requests.Length; i++)
                    total += BalanceMath.EnemyDamageTaken(requests[i].Damage, requests[i].BulletPen, defense.ValueRO.Value);
                requests.Clear();

                health.ValueRW.Value -= total;
                if (health.ValueRO.Value > 0f)
                    continue;

                wave.EnemiesAlive--;

                if (hasSession)
                {
                    ref var session = ref SystemAPI.GetSingletonRW<GameSession>().ValueRW;
                    session.Score += score.ValueRO.Value;
                    events.Add(new SimEvent
                    {
                        Kind = SimEventKind.ScoreChanged,
                        Source = entity,
                        IntValue = session.Score,
                        Amount = score.ValueRO.Value
                    });
                }

                events.Add(new SimEvent
                {
                    Kind = SimEventKind.EnemyDied,
                    Source = entity,
                    Position = transform.ValueRO.Position,
                    IntValue = score.ValueRO.Value
                });
                ecb.DestroyEntity(entity);
            }

            foreach (var (health, requests, transform, entity) in
                     SystemAPI.Query<RefRW<Health>, DynamicBuffer<DamageRequest>, RefRO<LocalTransform>>()
                         .WithAll<TowerTag>()
                         .WithEntityAccess())
            {
                if (requests.Length == 0)
                    continue;

                float total = 0f;
                for (int i = 0; i < requests.Length; i++)
                    total += requests[i].Damage;
                requests.Clear();

                health.ValueRW.Value -= total;
                if (health.ValueRO.Value > 0f)
                    continue;

                events.Add(new SimEvent { Kind = SimEventKind.TowerDied, Source = entity, Position = transform.ValueRO.Position });
                ecb.DestroyEntity(entity);
            }

            foreach (var (health, requests, transform, entity) in
                     SystemAPI.Query<RefRW<Health>, DynamicBuffer<DamageRequest>, RefRO<LocalTransform>>()
                         .WithAll<BunkerTag>()
                         .WithEntityAccess())
            {
                if (requests.Length == 0)
                    continue;

                float total = 0f;
                for (int i = 0; i < requests.Length; i++)
                    total += requests[i].Damage;
                requests.Clear();

                float newHealth = math.clamp(health.ValueRO.Value - total, 0f, health.ValueRO.Max);
                health.ValueRW.Value = newHealth;

                events.Add(new SimEvent
                {
                    Kind = SimEventKind.PlayerHit,
                    Source = entity,
                    Position = transform.ValueRO.Position,
                    Amount = total,
                    IntValue = (int)math.round(newHealth)
                });

                if (newHealth <= 0f)
                {
                    if (hasSession)
                    {
                        ref var session = ref SystemAPI.GetSingletonRW<GameSession>().ValueRW;
                        session.IsGameOver = true;
                    }
                    events.Add(new SimEvent
                    {
                        Kind = SimEventKind.GameOver,
                        Source = entity,
                        Position = transform.ValueRO.Position
                    });
                }
            }
        }
    }
}
