using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace Bunker.Simulation
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(DamageSystem))]
    public partial struct TowerBuffSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<WaveConfig>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (weapon, requests) in
                     SystemAPI.Query<RefRW<Weapon>, DynamicBuffer<TowerBuffRequest>>().WithAll<TowerTag>())
            {
                if (requests.Length == 0)
                    continue;

                ref var w = ref weapon.ValueRW;
                for (int i = 0; i < requests.Length && !w.HasBuff; i++)
                {
                    var request = requests[i];
                    switch (request.Kind)
                    {
                        case TowerBuffKind.Attack:
                            w.Damage = math.floor(w.Damage * request.Multiplier);
                            break;
                        case TowerBuffKind.Speed:
                            w.FireInterval /= math.max(request.Multiplier, 1e-3f);
                            break;
                        case TowerBuffKind.BulletPen:
                            // Flat: pen only matters against armor, and low base pen would scale to nothing
                            w.BulletPen += request.Multiplier;
                            break;
                    }
                    w.HasBuff = true;
                }
                requests.Clear();
            }
        }
    }
}
