using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Bunker.Simulation
{
    // Sends allies out of the bunker when an ally card is played (AllySpawnRequest). They walk the enemy path backwards, stop to hit any enemy
    // in melee range, and once past the path start they hit the enemy base until it falls.
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnemyMovementSystem))]
    [UpdateBefore(typeof(TargetingSystem))]
    public partial struct AllySystem : ISystem
    {
        const float ArriveDistance = 0.01f;

        EntityQuery pathQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AllySpawnConfig>();
            state.RequireForUpdate<AllySpawnRequest>();
            state.RequireForUpdate<SimEvent>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            pathQuery = state.GetEntityQuery(ComponentType.ReadOnly<PathPoint>());
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

            int lastPathIndex = -1;
            if (defaultPathEntity != Entity.Null && pathLookup.HasBuffer(defaultPathEntity))
                lastPathIndex = pathLookup[defaultPathEntity].Length - 1;

            var config = SystemAPI.GetSingleton<AllySpawnConfig>();
            var requests = SystemAPI.GetSingletonBuffer<AllySpawnRequest>();
            int typeCount = config.Roster.IsCreated ? config.Roster.Value.Types.Length : 0;
            for (int i = 0; i < requests.Length; i++)
                if (requests[i].TypeIndex >= 0 && requests[i].TypeIndex < typeCount)
                {
                    var slotRng = Random.CreateFromIndex((uint)(SystemAPI.Time.ElapsedTime * 1000.0) + (uint)i + 1u);
                    SpawnAlly(requests[i].TypeIndex, lastPathIndex, slotRng.NextFloat2Direction() * math.sqrt(slotRng.NextFloat()), in config, ecb);
                }
            requests.Clear();

            var enemies = new NativeList<Entity>(Allocator.Temp);
            var enemyPositions = new NativeList<float3>(Allocator.Temp);
            // Enemies already hitting the bunker fight nobody else: allies spawning on top of them would get free hits with no retaliation
            foreach (var (transform, entity) in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<EnemyTag, Health>().WithNone<AtPathEnd>().WithEntityAccess())
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

            // Snapshot of every ally's progress, so "who is ahead of me" does not depend on iteration order
            float spacing = SystemAPI.TryGetSingleton<WaveConfig>(out var waveConfig) ? waveConfig.UnitSpacing : 0f;
            var walkers = new NativeList<Walker>(Allocator.Temp);
            if (spacing > 0f)
            {
                foreach (var (transform, follower, entity) in
                         SystemAPI.Query<RefRO<LocalTransform>, RefRO<AllyPathFollower>>().WithAll<AllyTag>().WithEntityAccess())
                {
                    Entity walkerPath = follower.ValueRO.PathEntity != Entity.Null ? follower.ValueRO.PathEntity : defaultPathEntity;
                    if (!TryWaypoint(follower.ValueRO.NextIndex, follower.ValueRO.Slot, walkerPath, in pathLookup, hasBase, basePos, out float3 walkerWaypoint))
                        continue;
                    walkers.Add(new Walker
                    {
                        Entity = entity,
                        Position = transform.ValueRO.Position.xy,
                        PathEntity = walkerPath,
                        NextIndex = follower.ValueRO.NextIndex,
                        DistanceToWaypoint = math.distance(transform.ValueRO.Position, walkerWaypoint)
                    });
                }
            }

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

                Entity targetPathEntity = follower.ValueRO.PathEntity != Entity.Null ? follower.ValueRO.PathEntity : defaultPathEntity;
                if (!TryWaypoint(follower.ValueRO.NextIndex, follower.ValueRO.Slot, targetPathEntity, in pathLookup, hasBase, basePos, out float3 waypoint))
                    continue;

                if (spacing > 0f && IsBlockedByAhead(entity, pos.xy, targetPathEntity, follower.ValueRO.NextIndex, math.distance(pos, waypoint), spacing, walkers))
                    continue;

                pos = BalanceMath.StepAxisAligned(pos, waypoint, speed.ValueRO.Value * dt);
                transform.ValueRW.Position = pos;

                if (follower.ValueRO.NextIndex >= 0 && math.distance(pos, waypoint) <= ArriveDistance)
                    follower.ValueRW.NextIndex--;
            }

            enemies.Dispose();
            enemyPositions.Dispose();
            walkers.Dispose();
        }

        struct Walker
        {
            public Entity Entity;
            public float2 Position;
            public Entity PathEntity;
            public int NextIndex;
            public float DistanceToWaypoint;
        }

        // The point an ally walks to: the path point it is heading for, or the enemy base once past the path start
        static bool TryWaypoint(int nextIndex, float2 slot, Entity pathEntity, in BufferLookup<PathPoint> pathLookup, bool hasBase, float3 basePos, out float3 waypoint)
        {
            if (nextIndex >= 0 && pathEntity != Entity.Null && pathLookup.HasBuffer(pathEntity))
            {
                var path = pathLookup[pathEntity];
                if (nextIndex < path.Length)
                {
                    waypoint = BalanceMath.FlagTarget(path[nextIndex], slot);
                    return true;
                }
            }
            waypoint = basePos;
            return hasBase;
        }

        // Ahead = fewer path points left (allies walk the path backwards); a full tie is broken by entity index
        static bool IsBlockedByAhead(Entity self, float2 pos, Entity pathEntity, int nextIndex, float distToWaypoint, float spacing, NativeList<Walker> walkers)
        {
            for (int i = 0; i < walkers.Length; i++)
            {
                var other = walkers[i];
                if (other.Entity == self || other.PathEntity != pathEntity)
                    continue;
                if (math.distance(pos, other.Position) >= spacing)
                    continue;

                bool ahead = other.NextIndex < nextIndex
                    || (other.NextIndex == nextIndex
                        && (other.DistanceToWaypoint < distToWaypoint
                            || (other.DistanceToWaypoint == distToWaypoint && other.Entity.Index < self.Index)));
                if (ahead)
                    return true;
            }
            return false;
        }

        static void SpawnAlly(int typeIndex, int lastPathIndex, float2 slot, in AllySpawnConfig config, EntityCommandBuffer ecb)
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
            ecb.AddComponent(e, new AllyPathFollower { NextIndex = lastPathIndex, Slot = slot });
            ecb.AddBuffer<DamageRequest>(e);
            ecb.AddComponent(e, new NeedsView { Kind = ViewKind.Ally, PrefabId = typeIndex });
        }
    }
}
