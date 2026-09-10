using Unity.Burst;
using Unity.Entities;

namespace Bunker.Simulation
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ProjectileSystem))]
    public partial struct PlayerAttackSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<WaveConfig>();
            state.RequireForUpdate<BunkerTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingletonEntity<BunkerTag>(out var bunkerEntity))
                return;

            var config = SystemAPI.GetSingleton<WaveConfig>();
            var damageBuffer = SystemAPI.GetBuffer<DamageRequest>(bunkerEntity);
            float dt = SystemAPI.Time.DeltaTime;

            foreach (var (atEnd, weapon, entity) in
                     SystemAPI.Query<RefRW<AtPathEnd>, RefRO<Weapon>>().WithAll<EnemyTag>().WithEntityAccess())
            {
                atEnd.ValueRW.AttackTimer -= dt;
                if (atEnd.ValueRO.AttackTimer > 0f)
                    continue;

                atEnd.ValueRW.AttackTimer += config.PlayerDamageInterval;
                damageBuffer.Add(new DamageRequest { Damage = weapon.ValueRO.Damage, BulletPen = 0f });
            }
        }
    }
}
