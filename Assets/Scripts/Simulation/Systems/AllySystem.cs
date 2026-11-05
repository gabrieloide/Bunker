using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Bunker.Simulation
{
    // Sends allies out of the bunker when an ally card is played (AllySpawnRequest). They march along the player's flags, splitting where the flags
    // fork, and wait at the last one until the round is running. Then they walk the closest enemy route backwards (with no flags, the primary route
    // from its end), stop to hit any enemy in melee range, and once past the route's start they hit the enemy base until it falls.
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

            // Between waves soldiers only walk along the player's flags; at the last flag they wait for the round to start
            bool preparation = SystemAPI.TryGetSingleton<WaveState>(out var waveState) && waveState.Phase == WavePhase.WaitingToStart;
            bool hasFlags = SystemAPI.TryGetSingletonBuffer<PlayerFlagNode>(out var flagNodes) && flagNodes.Length > 0;

            var config = SystemAPI.GetSingleton<AllySpawnConfig>();
            var requests = SystemAPI.GetSingletonBuffer<AllySpawnRequest>();
            int typeCount = config.Roster.IsCreated ? config.Roster.Value.Types.Length : 0;
            for (int i = 0; i < requests.Length; i++)
                if (requests[i].TypeIndex >= 0 && requests[i].TypeIndex < typeCount)
                {
                    var slotRng = Random.CreateFromIndex((uint)(SystemAPI.Time.ElapsedTime * 1000.0) + (uint)i + 1u);
                    SpawnAlly(requests[i].TypeIndex, lastPathIndex, hasFlags, slotRng.NextFloat2Direction() * math.sqrt(slotRng.NextFloat()), in config, ecb);
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
            var parentLookup = SystemAPI.GetComponentLookup<PathParent>(true);

            // How far the leading enemy has got on each route, so allies leaving the flags join in front of the enemies, not behind them
            var frontmost = new NativeHashMap<Entity, int>(8, Allocator.Temp);
            foreach (var enemyFollower in SystemAPI.Query<RefRO<PathFollower>>().WithAll<EnemyTag>().WithNone<AtPathEnd>())
            {
                Entity enemyPath = enemyFollower.ValueRO.PathEntity != Entity.Null ? enemyFollower.ValueRO.PathEntity : defaultPathEntity;
                if (!frontmost.TryGetValue(enemyPath, out int front) || enemyFollower.ValueRO.NextIndex > front)
                    frontmost[enemyPath] = enemyFollower.ValueRO.NextIndex;
            }

            // Snapshot of every ally's progress, so "who is ahead of me" does not depend on iteration order
            float spacing = SystemAPI.TryGetSingleton<WaveConfig>(out var waveConfig) ? waveConfig.UnitSpacing : 0f;
            var walkers = new NativeList<Walker>(Allocator.Temp);
            if (spacing > 0f)
            {
                foreach (var (transform, follower, entity) in
                         SystemAPI.Query<RefRO<LocalTransform>, RefRO<AllyPathFollower>>().WithAll<AllyTag>().WithEntityAccess())
                {
                    Progress(follower.ValueRO, defaultPathEntity, hasFlags, out Entity walkerPath, out int walkerIndex);
                    if (!TryWaypoint(follower.ValueRO, walkerPath, flagNodes, hasFlags, in pathLookup, hasBase, basePos, out float3 walkerWaypoint))
                        continue;
                    walkers.Add(new Walker
                    {
                        Entity = entity,
                        Position = transform.ValueRO.Position.xy,
                        PathEntity = walkerPath,
                        NextIndex = walkerIndex,
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
                else if (hasBase && !preparation && !follower.ValueRO.OnFlags && follower.ValueRO.NextIndex < 0 && math.distance(pos.xy, basePos.xy) <= m.Range + baseReach)
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

                bool onFlags = follower.ValueRO.OnFlags && hasFlags;
                if (preparation && !onFlags)
                    continue;

                Progress(follower.ValueRO, defaultPathEntity, hasFlags, out Entity targetPathEntity, out int progressIndex);
                if (!TryWaypoint(follower.ValueRO, targetPathEntity, flagNodes, hasFlags, in pathLookup, hasBase, basePos, out float3 waypoint))
                    continue;

                if (spacing > 0f && IsBlockedByAhead(entity, pos.xy, targetPathEntity, progressIndex, math.distance(pos, waypoint), spacing, walkers))
                    continue;

                pos = BalanceMath.StepAxisAligned(pos, waypoint, speed.ValueRO.Value * dt);
                transform.ValueRW.Position = pos;

                if (math.distance(pos, waypoint) > ArriveDistance)
                    continue;

                if (onFlags)
                {
                    int child = BalanceMath.NextFlagChild(flagNodes, follower.ValueRO.FlagNode);
                    if (child >= 0)
                        follower.ValueRW.FlagNode = child;
                    else if (!preparation)
                    {
                        // No flag further on and the round is running: head for the closest stretch of an enemy route still ahead of the enemies (or the base, if nearer) and on towards the base
                        var pathEntities = pathQuery.ToEntityArray(Allocator.Temp);
                        LeaveFlags(ref follower.ValueRW, pos, pathEntities, in pathLookup, frontmost, hasBase, basePos);
                        pathEntities.Dispose();
                    }
                }
                else if (follower.ValueRO.NextIndex >= 0)
                {
                    follower.ValueRW.NextIndex--;
                    // Past the start of a branch: carry on backwards along the route it forked from
                    if (follower.ValueRO.NextIndex < 0 && parentLookup.TryGetComponent(targetPathEntity, out var parent) && pathLookup.HasBuffer(parent.Path))
                    {
                        follower.ValueRW.PathEntity = parent.Path;
                        follower.ValueRW.NextIndex = pathLookup[parent.Path].Length - 1;
                    }
                }
            }

            enemies.Dispose();
            enemyPositions.Dispose();
            walkers.Dispose();
            frontmost.Dispose();
        }

        struct Walker
        {
            public Entity Entity;
            public float2 Position;
            public Entity PathEntity;
            public int NextIndex;
            public float DistanceToWaypoint;
        }

        // Which "lane" an ally is in and how far along it, for the spacing order: the flag tree is one lane (Entity.Null), where a higher node
        // is further out, so its index is negated to read like the routes, where a lower index is further along
        static void Progress(in AllyPathFollower follower, Entity defaultPath, bool hasFlags, out Entity lane, out int index)
        {
            if (follower.OnFlags && hasFlags)
            {
                lane = Entity.Null;
                index = -follower.FlagNode;
                return;
            }
            lane = follower.PathEntity != Entity.Null ? follower.PathEntity : defaultPath;
            index = follower.NextIndex;
        }

        // The point an ally walks to: its own spot at the next player flag or route point, or the enemy base once past the route's start
        static bool TryWaypoint(in AllyPathFollower follower, Entity pathEntity, DynamicBuffer<PlayerFlagNode> flagNodes, bool hasFlags, in BufferLookup<PathPoint> pathLookup, bool hasBase, float3 basePos, out float3 waypoint)
        {
            if (follower.OnFlags && hasFlags && follower.FlagNode >= 0 && follower.FlagNode < flagNodes.Length)
            {
                var node = flagNodes[follower.FlagNode];
                waypoint = BalanceMath.FlagTarget(new PathPoint { Value = node.Position, Radius = node.Radius }, follower.Slot);
                return true;
            }
            if (follower.NextIndex >= 0 && pathEntity != Entity.Null && pathLookup.HasBuffer(pathEntity))
            {
                var path = pathLookup[pathEntity];
                if (follower.NextIndex < path.Length)
                {
                    waypoint = BalanceMath.FlagTarget(path[follower.NextIndex], follower.Slot);
                    return true;
                }
            }
            waypoint = basePos;
            return hasBase;
        }

        static void LeaveFlags(ref AllyPathFollower follower, float3 pos, NativeArray<Entity> pathEntities, in BufferLookup<PathPoint> pathLookup, NativeHashMap<Entity, int> frontmost, bool hasBase, float3 basePos)
        {
            follower.OnFlags = false;
            follower.NextIndex = -1;
            float best = hasBase ? math.distance(pos.xy, basePos.xy) : float.MaxValue;
            for (int p = 0; p < pathEntities.Length; p++)
            {
                var path = pathLookup[pathEntities[p]];
                frontmost.TryGetValue(pathEntities[p], out int firstAhead); // 0 when no enemy walks this route
                for (int i = firstAhead; i < path.Length; i++)
                {
                    float d = math.distance(pos.xy, path[i].Value.xy);
                    if (d < best)
                    {
                        best = d;
                        follower.NextIndex = i;
                        follower.PathEntity = pathEntities[p];
                    }
                }
            }
        }

        // Ahead = fewer path points left (allies walk the path backwards); a full tie is broken by entity index
        static bool IsBlockedByAhead(Entity self, float2 pos, Entity pathEntity, int nextIndex, float distToWaypoint, float spacing, NativeList<Walker> walkers)
        {
            for (int i = 0; i < walkers.Length; i++)
            {
                var other = walkers[i];
                if (other.Entity == self || other.PathEntity != pathEntity)
                    continue;
                // On the flag tree (lane Entity.Null) the order only means something between allies heading for the same flag, not across branches
                if (pathEntity == Entity.Null && other.NextIndex != nextIndex)
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

        static void SpawnAlly(int typeIndex, int lastPathIndex, bool onFlags, float2 slot, in AllySpawnConfig config, EntityCommandBuffer ecb)
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
            ecb.AddComponent(e, new AllyPathFollower { NextIndex = lastPathIndex, Slot = slot, OnFlags = onFlags, FlagNode = 0 });
            ecb.AddBuffer<DamageRequest>(e);
            ecb.AddComponent(e, new NeedsView { Kind = ViewKind.Ally, PrefabId = typeIndex });
        }
    }
}
