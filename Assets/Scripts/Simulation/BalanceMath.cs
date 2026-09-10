using Unity.Entities;
using Unity.Mathematics;

namespace Bunker.Simulation
{
    public static class BalanceMath
    {
        public static float EnemyDamageTaken(float damage, float bulletPen, float defense)
        {
            return math.max(0f, damage - (bulletPen - defense));
        }

        public static bool ShouldUnlockTier(int completedWave, int wavesPerTier, int unlockedTypes, int maxUnlockedTypes)
        {
            return completedWave % wavesPerTier == 0 && unlockedTypes < maxUnlockedTypes;
        }

        public static EnemyBuffKind PickWeighted(ref BlobArray<float> weights, ref Random rng)
        {
            float total = 0f;
            for (int i = 0; i < weights.Length; i++)
                total += math.max(0f, weights[i]);

            if (total <= 0f)
                return EnemyBuffKind.None;

            float roll = rng.NextFloat(0f, total);
            float cumulative = 0f;
            for (int i = 0; i < weights.Length; i++)
            {
                cumulative += math.max(0f, weights[i]);
                if (roll < cumulative)
                    return (EnemyBuffKind)i;
            }
            return (EnemyBuffKind)(weights.Length - 1);
        }

        public static EnemyTypeDef ApplyBuff(EnemyTypeDef def, EnemyBuffKind buff, ref EnemyBuffTableBlob table)
        {
            switch (buff)
            {
                case EnemyBuffKind.Life: def.Life *= table.LifeMultiplier; break;
                case EnemyBuffKind.Attack: def.Damage *= table.DamageMultiplier; break;
                case EnemyBuffKind.FireRate: def.FireInterval /= table.FireRateMultiplier; break;
                case EnemyBuffKind.Defense: def.Defense *= table.DefenseMultiplier; break;
                case EnemyBuffKind.Velocity: def.MoveSpeed *= table.SpeedMultiplier; break;
            }
            return def;
        }

        public static bool BoxesOverlap(float3 posA, float2 offsetA, float2 halfA, float3 posB, float2 halfB)
        {
            float2 centerA = posA.xy + offsetA;
            float2 delta = math.abs(centerA - posB.xy);
            float2 limit = halfA + halfB;
            return delta.x <= limit.x && delta.y <= limit.y;
        }
    }
}
