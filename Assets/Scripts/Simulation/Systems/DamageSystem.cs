using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace Bunker.Simulation
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ProjectileSystem))]
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
        }
    }
}
