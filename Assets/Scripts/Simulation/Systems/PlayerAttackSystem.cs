using Unity.Burst;
using Unity.Entities;

namespace Bunker.Simulation
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TowerBuffSystem))]
    public partial struct PlayerAttackSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<WaveConfig>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<WaveConfig>();
            var events = SystemAPI.GetSingletonBuffer<SimEvent>();
            float dt = SystemAPI.Time.DeltaTime;

            foreach (var (atEnd, weapon, entity) in
                     SystemAPI.Query<RefRW<AtPathEnd>, RefRO<Weapon>>().WithAll<EnemyTag>().WithEntityAccess())
            {
                atEnd.ValueRW.AttackTimer -= dt;
                if (atEnd.ValueRO.AttackTimer > 0f)
                    continue;

                atEnd.ValueRW.AttackTimer += config.PlayerDamageInterval;
                events.Add(new SimEvent { Kind = SimEventKind.PlayerHit, Source = entity, Amount = weapon.ValueRO.Damage });
            }
        }
    }
}
