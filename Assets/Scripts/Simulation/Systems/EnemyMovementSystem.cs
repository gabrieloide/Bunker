using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Bunker.Simulation
{
    // Walks enemies along their path. An enemy stops when an ally touches it (and hits that ally in melee) or when
    // another enemy of the same path is right ahead of it, so soldiers never overlap or walk through each other.
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(WaveSystem))]
    public partial struct EnemyMovementSystem : ISystem
    {
        const float ArriveDistance = 0.01f;
        // Fraction of the attack interval an enemy waits after touching an ally before its first blow
        const float ContactWindUp = 0.5f;

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
            // Once the match is decided nobody fights or hits anymore
            bool gameOver = SystemAPI.TryGetSingleton<GameSession>(out var session) && session.IsGameOver;

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

            var config = SystemAPI.GetSingleton<WaveConfig>();
            float contact = config.ContactDistance;
            float spacing = config.UnitSpacing;

            var events = SystemAPI.GetSingletonBuffer<SimEvent>();
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            float dt = SystemAPI.Time.DeltaTime;

            // Snapshot of the walking enemies, so "who is ahead of me" does not depend on iteration order
            var walkers = new NativeList<Walker>(Allocator.Temp);
            if (spacing > 0f)
            {
                foreach (var (transform, follower, entity) in
                         SystemAPI.Query<RefRO<LocalTransform>, RefRO<PathFollower>>()
                             .WithAll<EnemyTag>()
                             .WithNone<AtPathEnd>()
                             .WithEntityAccess())
                {
                    Entity pathEntity = follower.ValueRO.PathEntity != Entity.Null ? follower.ValueRO.PathEntity : defaultPathEntity;
                    walkers.Add(new Walker
                    {
                        Entity = entity,
                        Position = transform.ValueRO.Position.xy,
                        PathEntity = pathEntity,
                        NextIndex = follower.ValueRO.NextIndex,
                        DistanceToNext = DistanceToNext(transform.ValueRO.Position, follower.ValueRO.NextIndex, follower.ValueRO.Slot, pathEntity, pathLookup)
                    });
                }
            }

            var allies = new NativeList<Entity>(Allocator.Temp);
            var allyPositions = new NativeList<float2>(Allocator.Temp);
            if (contact > 0f && !gameOver)
            {
                foreach (var (transform, entity) in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<AllyTag, Health>().WithEntityAccess())
                {
                    allies.Add(entity);
                    allyPositions.Add(transform.ValueRO.Position.xy);
                }
            }

            var damageLookup = SystemAPI.GetBufferLookup<DamageRequest>();
            var weaponLookup = SystemAPI.GetComponentLookup<Weapon>(true);
            var branchLookup = SystemAPI.GetBufferLookup<PathBranch>(true);
            var forkLookup = SystemAPI.GetComponentLookup<PathFork>();

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

                follower.ValueRW.ContactCooldown = math.max(0f, follower.ValueRO.ContactCooldown - dt);

                if (follower.ValueRO.NextIndex >= path.Length)
                {
                    // A fork: take the next branch in turn, so the group splits evenly between them
                    if (branchLookup.HasBuffer(targetPathEntity) && forkLookup.HasComponent(targetPathEntity) && branchLookup[targetPathEntity].Length > 0)
                    {
                        var branches = branchLookup[targetPathEntity];
                        var fork = forkLookup[targetPathEntity];
                        follower.ValueRW.PathEntity = branches[fork.NextBranch % branches.Length].Path;
                        follower.ValueRW.NextIndex = 0;
                        fork.NextBranch++;
                        forkLookup[targetPathEntity] = fork;
                        continue;
                    }

                    ecb.AddComponent(entity, new AtPathEnd { AttackTimer = 0f });
                    events.Add(new SimEvent { Kind = SimEventKind.EnemyReachedEnd, Source = entity, Position = transform.ValueRO.Position });
                    continue;
                }

                float3 pos = transform.ValueRO.Position;

                // An ally in contact: stand and trade blows instead of walking on
                int touching = -1;
                float touchingDist = contact;
                for (int i = 0; i < allies.Length; i++)
                {
                    float d = math.distance(pos.xy, allyPositions[i]);
                    if (d <= touchingDist)
                    {
                        touchingDist = d;
                        touching = i;
                    }
                }
                if (touching >= 0)
                {
                    Entity ally = allies[touching];
                    if (follower.ValueRO.ContactCooldown <= 0f && weaponLookup.TryGetComponent(entity, out var weapon) && damageLookup.HasBuffer(ally))
                    {
                        follower.ValueRW.ContactCooldown = math.max(0.05f, weapon.FireInterval);
                        damageLookup[ally].Add(new DamageRequest { Damage = weapon.Damage, BulletPen = 0f });
                        events.Add(new SimEvent
                        {
                            Kind = SimEventKind.MeleeHit,
                            Source = entity,
                            Target = ally,
                            Position = new float3(allyPositions[touching], pos.z),
                            Amount = weapon.Damage,
                            IntValue = 0
                        });
                    }
                    continue;
                }

                // Not in contact: the next contact starts with a wind-up, so a cheap ally always holds the enemy for a moment before the first blow
                if (weaponLookup.TryGetComponent(entity, out var idleWeapon))
                    follower.ValueRW.ContactCooldown = ContactWindUp * math.max(0.05f, idleWeapon.FireInterval);

                if (spacing > 0f && IsBlockedByAhead(entity, pos.xy, targetPathEntity, follower.ValueRO.NextIndex,
                        DistanceToNext(pos, follower.ValueRO.NextIndex, follower.ValueRO.Slot, targetPathEntity, pathLookup), spacing, walkers))
                    continue;

                float3 targetPos = BalanceMath.FlagTarget(path[follower.ValueRO.NextIndex], follower.ValueRO.Slot);
                pos = BalanceMath.StepAxisAligned(pos, targetPos, speed.ValueRO.Value * dt);
                transform.ValueRW.Position = pos;

                if (math.distance(pos, targetPos) <= ArriveDistance)
                    follower.ValueRW.NextIndex++;
            }

            walkers.Dispose();
            allies.Dispose();
            allyPositions.Dispose();
        }

        struct Walker
        {
            public Entity Entity;
            public float2 Position;
            public Entity PathEntity;
            public int NextIndex;
            public float DistanceToNext;
        }

        // Length of the L the soldier still has to walk to its own spot in the next flag
        static float DistanceToNext(float3 pos, int nextIndex, float2 slot, Entity pathEntity, BufferLookup<PathPoint> pathLookup)
        {
            if (pathEntity == Entity.Null || !pathLookup.HasBuffer(pathEntity))
                return 0f;
            var path = pathLookup[pathEntity];
            if (nextIndex < 0 || nextIndex >= path.Length)
                return 0f;
            float3 target = BalanceMath.FlagTarget(path[nextIndex], slot);
            return math.abs(target.x - pos.x) + math.abs(target.y - pos.y);
        }

        // Ahead = further along the same path; a tie on both counts is broken by entity index so exactly one of two stacked soldiers waits
        static bool IsBlockedByAhead(Entity self, float2 pos, Entity pathEntity, int nextIndex, float distToNext, float spacing, NativeList<Walker> walkers)
        {
            for (int i = 0; i < walkers.Length; i++)
            {
                var other = walkers[i];
                if (other.Entity == self || other.PathEntity != pathEntity)
                    continue;
                if (math.distance(pos, other.Position) >= spacing)
                    continue;

                // Exact comparisons keep this a strict lexicographic order, so three soldiers can never block each other in a cycle
                bool ahead = other.NextIndex > nextIndex
                    || (other.NextIndex == nextIndex
                        && (other.DistanceToNext < distToNext
                            || (other.DistanceToNext == distToNext && other.Entity.Index < self.Index)));
                if (ahead)
                    return true;
            }
            return false;
        }
    }
}
