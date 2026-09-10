using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Bunker.Simulation
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(WaveSystem))]
    public partial struct EnemyMovementSystem : ISystem
    {
        const float ArriveDistance = 0.01f;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<WaveConfig>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var path = SystemAPI.GetSingletonBuffer<PathPoint>(true);
            if (path.Length == 0)
                return;

            var events = SystemAPI.GetSingletonBuffer<SimEvent>();
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            float dt = SystemAPI.Time.DeltaTime;

            foreach (var (transform, follower, speed, entity) in
                     SystemAPI.Query<RefRW<LocalTransform>, RefRW<PathFollower>, RefRO<MoveSpeed>>()
                         .WithAll<EnemyTag>()
                         .WithNone<AtPathEnd>()
                         .WithEntityAccess())
            {
                if (follower.ValueRO.NextIndex >= path.Length)
                {
                    ecb.AddComponent(entity, new AtPathEnd { AttackTimer = 0f });
                    events.Add(new SimEvent { Kind = SimEventKind.EnemyReachedEnd, Source = entity, Position = transform.ValueRO.Position });
                    continue;
                }

                float3 targetPos = path[follower.ValueRO.NextIndex].Value;
                float3 pos = transform.ValueRO.Position;
                float3 delta = targetPos - pos;
                float dist = math.length(delta);
                float step = speed.ValueRO.Value * dt;

                pos = dist <= step ? targetPos : pos + delta / dist * step;
                transform.ValueRW.Position = pos;

                if (math.distance(pos, targetPos) <= ArriveDistance)
                    follower.ValueRW.NextIndex++;
            }
        }
    }
}
