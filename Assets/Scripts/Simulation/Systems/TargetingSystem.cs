using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Bunker.Simulation
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnemyMovementSystem))]
    public partial struct TargetingSystem : ISystem
    {
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
            var towers = new NativeList<Entity>(Allocator.Temp);
            var towerPositions = new NativeList<float3>(Allocator.Temp);
            var enemies = new NativeList<Entity>(Allocator.Temp);
            var enemyPositions = new NativeList<float3>(Allocator.Temp);

            // Enemies shoot towers only; allies are fought in melee (EnemyMovementSystem)
            foreach (var (transform, entity) in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<Health, TowerTag>().WithEntityAccess())
            {
                towers.Add(entity);
                towerPositions.Add(transform.ValueRO.Position);
            }
            foreach (var (transform, entity) in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<EnemyTag, Health>().WithEntityAccess())
            {
                enemies.Add(entity);
                enemyPositions.Add(transform.ValueRO.Position);
            }

            var bases = new NativeList<Entity>(Allocator.Temp);
            var basePositions = new NativeList<float3>(Allocator.Temp);
            var baseReaches = new NativeList<float>(Allocator.Temp);
            foreach (var (transform, health, box, entity) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<Health>, RefRO<HitBox>>().WithAll<EnemyBaseTag>().WithEntityAccess())
            {
                if (health.ValueRO.Value > 0f)
                {
                    bases.Add(entity);
                    basePositions.Add(transform.ValueRO.Position + new float3(box.ValueRO.Offset, 0f));
                    baseReaches.Add(math.cmax(box.ValueRO.HalfExtents));
                }
            }

            bool preparation = SystemAPI.TryGetSingleton<WaveState>(out var waveState) && waveState.Phase == WavePhase.WaitingToStart;

            var events = SystemAPI.GetSingletonBuffer<SimEvent>();
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            float dt = SystemAPI.Time.DeltaTime;

            var artilleryLookup = SystemAPI.GetComponentLookup<Artillery>(true);
            var followerLookup = SystemAPI.GetComponentLookup<PathFollower>(true);
            var speedLookup = SystemAPI.GetComponentLookup<MoveSpeed>(true);
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

            foreach (var (transform, weapon, target, entity) in
                     SystemAPI.Query<RefRO<LocalTransform>, RefRW<Weapon>, RefRW<Target>>()
                         .WithNone<AtPathEnd>()
                         .WithEntityAccess())
            {
                ref var w = ref weapon.ValueRW;
                w.Cooldown -= dt;

                bool targetsEnemies = w.TargetFaction == Faction.Enemy;
                var candidates = targetsEnemies ? enemies : towers;
                var candidatePositions = targetsEnemies ? enemyPositions : towerPositions;

                float3 pos = transform.ValueRO.Position;
                int best = -1;
                float bestDist = float.MaxValue;
                for (int i = 0; i < candidates.Length; i++)
                {
                    float d = math.distance(pos.xy, candidatePositions[i].xy);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        best = i;
                    }
                }

                Entity newTarget = best >= 0 && bestDist <= w.Range ? candidates[best] : Entity.Null;
                float3 targetPos = best >= 0 ? candidatePositions[best] : float3.zero;
                bool targetingBase = false;

                // No free hits on the base between waves: a flag chain must not win with no enemy on the map
                if (targetsEnemies && !preparation && newTarget == Entity.Null && bases.Length > 0)
                {
                    float nearestDist = float.MaxValue;
                    for (int i = 0; i < bases.Length; i++)
                    {
                        float d = math.distance(pos.xy, basePositions[i].xy);
                        if (d <= w.Range + baseReaches[i] && d < nearestDist)
                        {
                            nearestDist = d;
                            newTarget = bases[i];
                            targetPos = basePositions[i];
                            targetingBase = true;
                        }
                    }
                }

                target.ValueRW.Value = newTarget;

                if (newTarget == Entity.Null || w.Cooldown > 0f)
                    continue;

                // Bursts fire BurstCount quick shots, then rest for FireInterval
                if (w.ShotsLeftInBurst <= 0)
                    w.ShotsLeftInBurst = math.max(1, w.BurstCount);
                w.ShotsLeftInBurst--;
                w.Cooldown = w.ShotsLeftInBurst > 0 ? w.BurstInterval : w.FireInterval;

                float3 origin = pos + w.MuzzleOffset;

                if (artilleryLookup.TryGetComponent(entity, out var artillery))
                {
                    // Shells take a while to land: aim where the target will be, not where it is
                    float3 impact = targetPos;
                    if (followerLookup.TryGetComponent(newTarget, out var follower) && speedLookup.TryGetComponent(newTarget, out var speed))
                    {
                        Entity targetPathEntity = follower.PathEntity != Entity.Null ? follower.PathEntity : defaultPathEntity;
                        if (targetPathEntity != Entity.Null && pathLookup.HasBuffer(targetPathEntity))
                        {
                            var targetPath = pathLookup[targetPathEntity];
                            if (targetPath.Length > 0)
                                impact = BalanceMath.PredictAlongPath(targetPos, follower.NextIndex, targetPath, speed.Value, artillery.RiseTime + artillery.FallTime, follower.Slot);
                        }
                    }

                    var shell = ecb.CreateEntity();
                    ecb.AddComponent(shell, LocalTransform.FromPosition(origin));
                    ecb.AddComponent<SimulationTag>(shell);
                    ecb.AddComponent(shell, new ArtilleryShell
                    {
                        Origin = origin,
                        Impact = impact,
                        RiseTime = artillery.RiseTime,
                        FallTime = artillery.FallTime,
                        Height = artillery.Height,
                        Damage = w.Damage,
                        BulletPen = w.BulletPen,
                        SplashRadius = artillery.SplashRadius,
                        ViewId = artillery.ViewId
                    });
                    ecb.AddComponent(shell, new NeedsView { Kind = ViewKind.ArtilleryShell, PrefabId = artillery.ViewId });

                    events.Add(new SimEvent { Kind = SimEventKind.Fired, Source = entity, Target = newTarget, Position = impact });
                    continue;
                }
                float3 dir = targetPos - origin;
                float len = math.length(dir);
                dir = len > 1e-5f ? dir / len : new float3(1f, 0f, 0f);

                var projectile = ecb.CreateEntity();
                ecb.AddComponent(projectile, LocalTransform.FromPosition(origin));
                ecb.AddComponent<SimulationTag>(projectile);
                ecb.AddComponent(projectile, new Projectile
                {
                    Damage = w.Damage,
                    BulletPen = w.BulletPen,
                    Lifetime = targetingBase ? math.max(w.ProjectileLifetime, (len / math.max(0.01f, w.ProjectileSpeed)) + 0.15f) : w.ProjectileLifetime,
                    Velocity = dir * w.ProjectileSpeed,
                    HalfExtents = w.ProjectileHalfExtents,
                    TargetFaction = w.TargetFaction,
                    BaseDamageCut = w.BaseDamageCut
                });
                ecb.AddComponent(projectile, new NeedsView
                {
                    Kind = targetsEnemies ? ViewKind.TowerProjectile : ViewKind.EnemyProjectile,
                    PrefabId = 0
                });

                events.Add(new SimEvent { Kind = SimEventKind.Fired, Source = entity, Target = newTarget, Position = targetPos });
            }

            towers.Dispose();
            towerPositions.Dispose();
            enemies.Dispose();
            enemyPositions.Dispose();
            bases.Dispose();
            basePositions.Dispose();
            baseReaches.Dispose();
        }
    }
}
