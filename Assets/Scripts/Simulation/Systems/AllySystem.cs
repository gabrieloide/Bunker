using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Bunker.Simulation
{
    // Sends allies out of the bunker when an ally card is played (AllySpawnRequest). Allies only spawn on and walk
    // player-built ally roads (AllyRoadTag with at least 2 path points), stopping to hit any enemy in melee range,
    // patrolling unfinished roads, and hitting the enemy base once a road reaches it.
    // Between waves they wait at the bunker.
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
            state.RequireForUpdate<AllySpawnRequest>();
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

            var pathLookup = SystemAPI.GetBufferLookup<PathPoint>(true);
            var parentLookup = SystemAPI.GetBufferLookup<PathParent>(true);
            var mergeLookup = SystemAPI.GetComponentLookup<PathMergeCursor>();
            var allyRoadLookup = SystemAPI.GetComponentLookup<AllyRoadTag>(true);

            // Between waves allies wait at the bunker
            bool preparation = SystemAPI.TryGetSingleton<WaveState>(out var waveState) && waveState.Phase == WavePhase.WaitingToStart;

            var config = SystemAPI.GetSingleton<AllySpawnConfig>();
            var requests = SystemAPI.GetSingletonBuffer<AllySpawnRequest>();
            if (requests.Length > 0)
            {
                int typeCount = config.Roster.IsCreated ? config.Roster.Value.Types.Length : 0;
                for (int i = 0; i < requests.Length; i++)
                {
                    if (requests[i].TypeIndex < 0 || requests[i].TypeIndex >= typeCount)
                        continue;
                    Entity preferred = requests[i].PreferredPath;
                    if (preferred == Entity.Null || !allyRoadLookup.HasComponent(preferred) || !pathLookup.HasBuffer(preferred) || pathLookup[preferred].Length < 2)
                        continue;
                    var slotRng = Random.CreateFromIndex((uint)(SystemAPI.Time.ElapsedTime * 1000.0) + (uint)i + 1u);
                    int lastIndex = pathLookup[preferred].Length - 1;
                    SpawnAlly(requests[i].TypeIndex, preferred, lastIndex, slotRng.NextFloat2Direction() * math.sqrt(slotRng.NextFloat()), in config, ecb);
                }
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

            var bases = new NativeList<Entity>(Allocator.Temp);
            var basePositions = new NativeList<float3>(Allocator.Temp);
            var baseReaches = new NativeList<float>(Allocator.Temp);
            var hitBoxLookup = SystemAPI.GetComponentLookup<HitBox>(true);
            foreach (var (transform, health, entity) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<Health>>().WithAll<EnemyBaseTag>().WithEntityAccess())
            {
                if (health.ValueRO.Value <= 0f) continue;
                bases.Add(entity);
                basePositions.Add(transform.ValueRO.Position);
                // Reach counts from the base's edge, not its centre
                baseReaches.Add(hitBoxLookup.TryGetComponent(entity, out var box) ? math.cmax(box.HalfExtents) : 0f);
            }

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
                    if (follower.ValueRO.PathEntity == Entity.Null || !allyRoadLookup.HasComponent(follower.ValueRO.PathEntity))
                        continue;
                    if (!TryWaypoint(transform.ValueRO.Position, follower.ValueRO, in pathLookup, basePositions, out float3 walkerWaypoint))
                        continue;
                    walkers.Add(new Walker
                    {
                        Entity = entity,
                        Position = transform.ValueRO.Position.xy,
                        PathEntity = follower.ValueRO.PathEntity,
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
                bool hittingBase = false;
                if (best >= 0 && bestDist <= m.Range)
                {
                    target = enemies[best];
                    targetPos = enemyPositions[best];
                }
                else if (bases.Length > 0 && !preparation && follower.ValueRO.NextIndex < 0)
                {
                    int nearestBase = NearestBaseIndex(pos.xy, basePositions);
                    if (nearestBase >= 0 && math.distance(pos.xy, basePositions[nearestBase].xy) <= m.Range + baseReaches[nearestBase])
                    {
                        target = bases[nearestBase];
                        targetPos = basePositions[nearestBase];
                        hittingBase = true;
                    }
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
                        IntValue = hittingBase ? 1 : 0
                    });
                    continue;
                }

                if (preparation)
                    continue;

                if (follower.ValueRO.PathEntity == Entity.Null || !allyRoadLookup.HasComponent(follower.ValueRO.PathEntity))
                    continue;

                if (!TryWaypoint(pos, follower.ValueRO, in pathLookup, basePositions, out float3 waypoint))
                    continue;

                Entity pathEntity = follower.ValueRO.PathEntity;
                if (spacing > 0f && IsBlockedByAhead(entity, pos.xy, pathEntity, follower.ValueRO.NextIndex, math.distance(pos, waypoint), spacing, walkers))
                    continue;

                pos = BalanceMath.StepAxisAligned(pos, waypoint, speed.ValueRO.Value * dt);
                transform.ValueRW.Position = pos;

                if (math.distance(pos, waypoint) > ArriveDistance || follower.ValueRO.NextIndex < 0)
                    continue;

                bool isCustomRoad = pathEntity != Entity.Null && allyRoadLookup.HasComponent(pathEntity) && pathLookup.HasBuffer(pathEntity);
                if (isCustomRoad && follower.ValueRO.Reversing != 0)
                {
                    var customPath = pathLookup[pathEntity];
                    follower.ValueRW.NextIndex++;
                    if (follower.ValueRW.NextIndex >= customPath.Length)
                    {
                        follower.ValueRW.Reversing = 0;
                        follower.ValueRW.NextIndex = customPath.Length > 1 ? customPath.Length - 2 : 0;
                    }
                    continue;
                }

                follower.ValueRW.NextIndex--;
                bool hasParents = parentLookup.HasBuffer(pathEntity) && parentLookup[pathEntity].Length > 0;
                // On an unfinished player-built ally road that does not reach an enemy base yet,
                // turn around and patrol back toward the bunker instead of walking off-road across obstacles.
                if (isCustomRoad && follower.ValueRO.NextIndex < 0 && !hasParents)
                {
                    var customPath = pathLookup[pathEntity];
                    if (customPath.Length > 0 && !IsTipNearAnyBase(customPath[0].Value.xy, basePositions, baseReaches, 3.5f))
                    {
                        follower.ValueRW.Reversing = 1;
                        follower.ValueRW.NextIndex = customPath.Length > 1 ? 1 : 0;
                    }
                    continue;
                }

                // Past the start of a branch: carry on backwards along a route it forked from
                if (follower.ValueRO.NextIndex < 0 && hasParents)
                {
                    var parents = parentLookup[pathEntity];
                    int parentIdx = 0;
                    if (parents.Length > 1 && mergeLookup.HasComponent(pathEntity))
                    {
                        var cursor = mergeLookup[pathEntity];
                        parentIdx = cursor.NextParent % parents.Length;
                        cursor.NextParent++;
                        mergeLookup[pathEntity] = cursor;
                    }
                    Entity parentPath = parents[parentIdx].Path;
                    if (allyRoadLookup.HasComponent(parentPath) && pathLookup.HasBuffer(parentPath) && pathLookup[parentPath].Length > 0)
                    {
                        follower.ValueRW.PathEntity = parentPath;
                        follower.ValueRW.NextIndex = pathLookup[parentPath].Length - 1;
                    }
                }
            }

            enemies.Dispose();
            enemyPositions.Dispose();
            bases.Dispose();
            basePositions.Dispose();
            baseReaches.Dispose();
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

        static bool IsTipNearAnyBase(float2 tip, NativeList<float3> basePositions, NativeList<float> baseReaches, float maxDist)
        {
            for (int i = 0; i < basePositions.Length; i++)
            {
                float reach = maxDist + baseReaches[i];
                if (math.distancesq(tip, basePositions[i].xy) <= reach * reach)
                    return true;
            }
            return false;
        }

        static int NearestBaseIndex(float2 pos, NativeList<float3> basePositions)
        {
            int best = -1;
            float bestDist = float.MaxValue;
            for (int i = 0; i < basePositions.Length; i++)
            {
                float d = math.distancesq(pos, basePositions[i].xy);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = i;
                }
            }
            return best;
        }

        // The point an ally walks to: its own spot at the next route point, or the nearest enemy base once past the route's start
        static bool TryWaypoint(float3 pos, in AllyPathFollower follower, in BufferLookup<PathPoint> pathLookup, NativeList<float3> basePositions, out float3 waypoint)
        {
            if (follower.NextIndex >= 0 && follower.PathEntity != Entity.Null && pathLookup.HasBuffer(follower.PathEntity))
            {
                var path = pathLookup[follower.PathEntity];
                if (follower.NextIndex < path.Length)
                {
                    waypoint = BalanceMath.FlagTarget(path[follower.NextIndex], follower.Slot);
                    return true;
                }
            }
            int nearest = NearestBaseIndex(pos.xy, basePositions);
            if (nearest >= 0)
            {
                waypoint = basePositions[nearest];
                return true;
            }
            waypoint = float3.zero;
            return false;
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

        static void SpawnAlly(int typeIndex, Entity route, int lastIndex, float2 slot, in AllySpawnConfig config, EntityCommandBuffer ecb)
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
            ecb.AddComponent(e, new AllyPathFollower { NextIndex = lastIndex, PathEntity = route, Slot = slot });
            ecb.AddBuffer<DamageRequest>(e);
            ecb.AddComponent(e, new NeedsView { Kind = ViewKind.Ally, PrefabId = typeIndex });
        }
    }
}
