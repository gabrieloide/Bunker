using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Bunker.Simulation
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TargetingSystem))]
    [UpdateBefore(typeof(DamageSystem))]
    public partial struct ProjectileSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<WaveConfig>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var towers = new NativeList<Entity>(Allocator.Temp);
            var towerPositions = new NativeList<float3>(Allocator.Temp);
            var towerBoxes = new NativeList<HitBox>(Allocator.Temp);
            var enemies = new NativeList<Entity>(Allocator.Temp);
            var enemyPositions = new NativeList<float3>(Allocator.Temp);
            var enemyBoxes = new NativeList<HitBox>(Allocator.Temp);

            foreach (var (transform, box, entity) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<HitBox>>().WithAll<Health>().WithAny<TowerTag, AllyTag>().WithEntityAccess())
            {
                towers.Add(entity);
                towerPositions.Add(transform.ValueRO.Position);
                towerBoxes.Add(box.ValueRO);
            }
            foreach (var (transform, box, entity) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<HitBox>>().WithAll<EnemyTag, Health>().WithEntityAccess())
            {
                enemies.Add(entity);
                enemyPositions.Add(transform.ValueRO.Position);
                enemyBoxes.Add(box.ValueRO);
            }
            foreach (var (transform, box, health, entity) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<HitBox>, RefRO<Health>>().WithAll<EnemyBaseTag>().WithEntityAccess())
            {
                if (health.ValueRO.Value > 0f)
                {
                    enemies.Add(entity);
                    enemyPositions.Add(transform.ValueRO.Position);
                    enemyBoxes.Add(box.ValueRO);
                }
            }

            var damageLookup = SystemAPI.GetBufferLookup<DamageRequest>();
            var defenseLookup = SystemAPI.GetComponentLookup<Defense>(true);
            var baseLookup = SystemAPI.GetComponentLookup<EnemyBaseTag>(true);
            var events = SystemAPI.GetSingletonBuffer<SimEvent>();
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            float dt = SystemAPI.Time.DeltaTime;

            foreach (var (transform, projectile, entity) in
                     SystemAPI.Query<RefRW<LocalTransform>, RefRW<Projectile>>().WithEntityAccess())
            {
                ref var p = ref projectile.ValueRW;
                p.Lifetime -= dt;
                float3 pos = transform.ValueRO.Position + p.Velocity * dt;
                transform.ValueRW.Position = pos;

                if (p.Lifetime <= 0f)
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                bool targetsEnemies = p.TargetFaction == Faction.Enemy;
                var candidates = targetsEnemies ? enemies : towers;
                var candidatePositions = targetsEnemies ? enemyPositions : towerPositions;
                var candidateBoxes = targetsEnemies ? enemyBoxes : towerBoxes;

                for (int i = 0; i < candidates.Length; i++)
                {
                    var box = candidateBoxes[i];
                    if (!BalanceMath.BoxesOverlap(candidatePositions[i], box.Offset, box.HalfExtents, pos, p.HalfExtents))
                        continue;

                    float damage = baseLookup.HasComponent(candidates[i]) ? p.Damage * (1f - p.BaseDamageCut) : p.Damage;
                    if (damageLookup.HasBuffer(candidates[i]))
                        damageLookup[candidates[i]].Add(new DamageRequest { Damage = damage, BulletPen = p.BulletPen });

                    events.Add(new SimEvent
                    {
                        Kind = SimEventKind.ProjectileHit,
                        Source = entity,
                        Target = candidates[i],
                        Position = pos,
                        // What the target actually loses, so the floating number matches the health bar
                        Amount = defenseLookup.TryGetComponent(candidates[i], out var defense)
                            ? BalanceMath.EnemyDamageTaken(damage, p.BulletPen, defense.Value)
                            : damage,
                        IntValue = (int)p.TargetFaction
                    });
                    ecb.DestroyEntity(entity);
                    break;
                }
            }

            towers.Dispose();
            towerPositions.Dispose();
            towerBoxes.Dispose();
            enemies.Dispose();
            enemyPositions.Dispose();
            enemyBoxes.Dispose();
        }
    }
}
