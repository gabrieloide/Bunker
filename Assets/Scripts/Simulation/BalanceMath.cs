using Unity.Entities;
using Unity.Mathematics;

namespace Bunker.Simulation
{
    public static class BalanceMath
    {
        // Armor soaks flat damage per hit; penetration cancels armor point for point.
        // Every hit keeps a chip of its damage so no enemy is ever immune.
        public const float MinDamageFraction = 0.25f;

        public static float EnemyDamageTaken(float damage, float bulletPen, float defense)
        {
            float armor = math.max(0f, defense - bulletPen);
            return math.max(damage * MinDamageFraction, damage - armor);
        }

        // Where a path follower will be after `time` seconds, walking its remaining waypoints at constant speed
        public static float3 PredictAlongPath(float3 position, int nextIndex, DynamicBuffer<PathPoint> path, float speed, float time)
        {
            float remaining = speed * time;
            while (nextIndex < path.Length && remaining > 0f)
            {
                float3 next = path[nextIndex].Value;
                float d = math.distance(position, next);
                if (d >= remaining)
                    return position + (next - position) / d * remaining;
                remaining -= d;
                position = next;
                nextIndex++;
            }
            return position;
        }

        public static bool CircleOverlapsBox(float2 center, float radius, float2 boxCenter, float2 halfExtents)
        {
            float2 closest = math.clamp(center, boxCenter - halfExtents, boxCenter + halfExtents);
            return math.lengthsq(center - closest) <= radius * radius;
        }

        public static float WaveLifeMultiplier(int wave, float growthPerWave)
        {
            return 1f + growthPerWave * math.max(0, wave - 1);
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
