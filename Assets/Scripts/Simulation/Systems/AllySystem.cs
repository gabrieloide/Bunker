using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Bunker.Simulation
{
    // Sends allies out of the bunker on a timer. They walk the enemy path backwards, stop to hit any enemy
    // in melee range, and once past the path start they hit the enemy base until it falls.
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnemyMovementSystem))]
    [UpdateBefore(typeof(TargetingSystem))]
    public partial struct AllySystem : ISystem
    {
        const float ArriveDistance = 0.01f;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AllySpawnConfig>();
            state.RequireForUpdate<AllySpawnState>();
            state.RequireForUpdate<SimEvent>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.TryGetSingleton<GameSession>(out var session) && session.IsGameOver)
                return;

            var events = SystemAPI.GetSingletonBuffer<SimEvent>();
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            float dt = SystemAPI.Time.DeltaTime;

            bool hasPath = SystemAPI.TryGetSingletonBuffer<PathPoint>(out var path, true);
            var config = SystemAPI.GetSingleton<AllySpawnConfig>();
            ref var spawn = ref SystemAPI.GetSingletonRW<AllySpawnState>().ValueRW;
            if (config.Interval > 0f && config.Roster.IsCreated && config.Roster.Value.Types.Length > 0)
            {
                spawn.Timer -= dt;
                if (spawn.Timer <= 0f)
                {
                    spawn.Timer += config.Interval;
                    int typeIndex = spawn.NextType % config.Roster.Value.Types.Length;
                    spawn.NextType = typeIndex + 1;
                    SpawnAlly(typeIndex, hasPath ? path.Length - 1 : -1, in config, ecb);
                }
            }

            var enemies = new NativeList<Entity>(Allocator.Temp);
            var enemyPositions = new NativeList<float3>(Allocator.Temp);
            foreach (var (transform, entity) in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<EnemyTag, Health>().WithEntityAccess())
            {
                enemies.Add(entity);
                enemyPositions.Add(transform.ValueRO.Position);
            }

            bool hasBase = SystemAPI.TryGetSingletonEntity<EnemyBaseTag>(out var baseEntity);
            float3 basePos = hasBase ? SystemAPI.GetComponent<LocalTransform>(baseEntity).Position : float3.zero;
            // Reach counts from the base's edge, not its centre
            float baseReach = hasBase && SystemAPI.HasComponent<HitBox>(baseEntity)
                ? math.cmax(SystemAPI.GetComponent<HitBox>(baseEntity).HalfExtents)
                : 0f;

            var damageLookup = SystemAPI.GetBufferLookup<DamageRequest>();
            var defenseLookup = SystemAPI.GetComponentLookup<Defense>(true);

            foreach (var (transform, melee, follower, speed, entity) in
                     SystemAPI.Query<RefRW<LocalTransform>, RefRW<Melee>, RefRW<AllyPathFollower>, RefRO<MoveSpeed>>()
                         .WithAll<AllyTag>()
                         .WithEntityAccess())
            {
                ref var m = ref melee.ValueRW;
                m.Cooldown -= dt;
                float3 pos = transform.ValueRO.Position;

                int best = -1;
                float bestDist = float.MaxValue;
                for (int i = 0; i < enemies.Length; i++)
                {
                    float d = math.distance(pos.xy, enemyPositions[i].xy);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        best = i;
                    }
                }

                Entity target = Entity.Null;
                float3 targetPos = float3.zero;
                if (best >= 0 && bestDist <= m.Range)
                {
                    target = enemies[best];
                    targetPos = enemyPositions[best];
                }
                else if (hasBase && follower.ValueRO.NextIndex < 0 && math.distance(pos.xy, basePos.xy) <= m.Range + baseReach)
                {
                    target = baseEntity;
                    targetPos = basePos;
                }

                if (target != Entity.Null)
                {
                    if (m.Cooldown > 0f || !damageLookup.HasBuffer(target))
                        continue;
                    m.Cooldown = m.Interval;
                    damageLookup[target].Add(new DamageRequest { Damage = m.Damage, BulletPen = m.BulletPen });
                    events.Add(new SimEvent
                    {
                        Kind = SimEventKind.MeleeHit,
                        Source = entity,
                        Target = target,
                        Position = targetPos,
                        Amount = defenseLookup.TryGetComponent(target, out var defense)
                            ? BalanceMath.EnemyDamageTaken(m.Damage, m.BulletPen, defense.Value)
                            : m.Damage,
                        IntValue = target == baseEntity ? 1 : 0
                    });
                    continue;
                }

                float3 waypoint;
                if (hasPath && follower.ValueRO.NextIndex >= 0 && follower.ValueRO.NextIndex < path.Length)
                    waypoint = path[follower.ValueRO.NextIndex].Value;
                else if (hasBase)
                    waypoint = basePos;
                else
                    continue;

                float3 delta = waypoint - pos;
                float dist = math.length(delta);
                float step = speed.ValueRO.Value * dt;
                pos = dist <= step ? waypoint : pos + delta / dist * step;
                transform.ValueRW.Position = pos;

                if (follower.ValueRO.NextIndex >= 0 && math.distance(pos, waypoint) <= ArriveDistance)
                    follower.ValueRW.NextIndex--;
            }

            enemies.Dispose();
            enemyPositions.Dispose();
        }

        static void SpawnAlly(int typeIndex, int lastPathIndex, in AllySpawnConfig config, EntityCommandBuffer ecb)
        {
            ref var def = ref config.Roster.Value.Types[typeIndex];
            var e = ecb.CreateEntity();
            ecb.AddComponent(e, LocalTransform.FromPosition(config.SpawnPosition));
            ecb.AddComponent<AllyTag>(e);
            ecb.AddComponent<SimulationTag>(e);
            ecb.AddComponent(e, new Health { Value = def.Life, Max = def.Life });
            ecb.AddComponent(e, new MoveSpeed { Value = def.MoveSpeed });
            ecb.AddComponent(e, new HitBox { HalfExtents = def.HitHalfExtents, Offset = def.HitOffset });
            ecb.AddComponent(e, new Melee
            {
                Damage = def.Damage,
                BulletPen = def.BulletPen,
                Interval = def.AttackInterval,
                Range = def.AttackRange,
                Cooldown = 0f
            });
            ecb.AddComponent(e, new AllyPathFollower { NextIndex = lastPathIndex });
            ecb.AddBuffer<DamageRequest>(e);
            ecb.AddComponent(e, new NeedsView { Kind = ViewKind.Ally, PrefabId = typeIndex });
        }
    }
}
