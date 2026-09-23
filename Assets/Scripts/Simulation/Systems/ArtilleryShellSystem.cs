using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Bunker.Simulation
{
    // Mortar shells: straight up from the muzzle, then free fall onto the impact point, then splash damage.
    // The apex jump from origin to impact column happens off-screen (the view also fades out near the apex).
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TargetingSystem))]
    [UpdateBefore(typeof(DamageSystem))]
    public partial struct ArtilleryShellSystem : ISystem
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
            var enemies = new NativeList<Entity>(Allocator.Temp);
            var enemyPositions = new NativeList<float3>(Allocator.Temp);
            var enemyBoxes = new NativeList<HitBox>(Allocator.Temp);
            var enemyDefense = new NativeList<float>(Allocator.Temp);
            foreach (var (transform, box, defense, entity) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<HitBox>, RefRO<Defense>>().WithAll<EnemyTag, Health>().WithEntityAccess())
            {
                enemies.Add(entity);
                enemyPositions.Add(transform.ValueRO.Position);
                enemyBoxes.Add(box.ValueRO);
                enemyDefense.Add(defense.ValueRO.Value);
            }

            var damageLookup = SystemAPI.GetBufferLookup<DamageRequest>();
            var events = SystemAPI.GetSingletonBuffer<SimEvent>();
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            float dt = SystemAPI.Time.DeltaTime;

            foreach (var (transform, shell, entity) in
                     SystemAPI.Query<RefRW<LocalTransform>, RefRW<ArtilleryShell>>().WithEntityAccess())
            {
                ref var s = ref shell.ValueRW;
                s.Elapsed += dt;

                if (s.Elapsed < s.RiseTime)
                {
                    // Thrown straight up: decelerates towards the apex
                    float t = s.Elapsed / s.RiseTime;
                    transform.ValueRW.Position = s.Origin + new float3(0f, s.Height * (1f - (1f - t) * (1f - t)), 0f);
                    continue;
                }

                float fallElapsed = s.Elapsed - s.RiseTime;
                if (fallElapsed < s.FallTime)
                {
                    // Free fall from rest at the apex: accelerates into the ground
                    float u = fallElapsed / s.FallTime;
                    transform.ValueRW.Position = s.Impact + new float3(0f, s.Height * (1f - u * u), 0f);
                    continue;
                }

                transform.ValueRW.Position = s.Impact;
                for (int i = 0; i < enemies.Length; i++)
                {
                    var box = enemyBoxes[i];
                    if (!BalanceMath.CircleOverlapsBox(s.Impact.xy, s.SplashRadius, enemyPositions[i].xy + box.Offset, box.HalfExtents))
                        continue;

                    if (damageLookup.HasBuffer(enemies[i]))
                        damageLookup[enemies[i]].Add(new DamageRequest { Damage = s.Damage, BulletPen = s.BulletPen });

                    events.Add(new SimEvent
                    {
                        Kind = SimEventKind.ProjectileHit,
                        Source = entity,
                        Target = enemies[i],
                        Position = enemyPositions[i],
                        Amount = BalanceMath.EnemyDamageTaken(s.Damage, s.BulletPen, enemyDefense[i]),
                        IntValue = (int)Faction.Enemy
                    });
                }

                events.Add(new SimEvent { Kind = SimEventKind.ShellImpact, Source = entity, Position = s.Impact, Amount = s.SplashRadius, IntValue = s.ViewId });
                ecb.DestroyEntity(entity);
            }

            enemies.Dispose();
            enemyPositions.Dispose();
            enemyBoxes.Dispose();
            enemyDefense.Dispose();
        }
    }
}
