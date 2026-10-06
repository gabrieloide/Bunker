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

        EntityQuery pathQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<WaveConfig>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            pathQuery = state.GetEntityQuery(ComponentType.ReadOnly<PathPoint>());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var pathLookup = SystemAPI.GetBufferLookup<PathPoint>(true);
            Entity defaultPathEntity = Entity.Null;
            if (SystemAPI.TryGetSingletonEntity<PrimaryPath>(out var primary))
                defaultPathEntity = primary;
            else if (!pathQuery.IsEmpty)
            {
                var ents = pathQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
                defaultPathEntity = ents[0];
                ents.Dispose();
            }

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
                Entity targetPathEntity = follower.ValueRO.PathEntity != Entity.Null ? follower.ValueRO.PathEntity : defaultPathEntity;
                if (targetPathEntity == Entity.Null || !pathLookup.HasBuffer(targetPathEntity))
                    continue;

                var path = pathLookup[targetPathEntity];
                if (path.Length == 0)
                    continue;

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
