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

        // Pseudo-random distribution (as in Dota): the n-th try since the last success succeeds with chance C·n.
        // Same long-run average as a flat `chance`, but no long droughts and fewer streaks.
        public static float PrdChance(float chance, int triesSinceSuccess)
        {
            return math.min(1f, PrdConstant(chance) * (triesSinceSuccess + 1));
        }

        // The C whose PRD averages `chance`, found by bisection (the average grows with C)
        public static float PrdConstant(float chance)
        {
            if (chance <= 0f) return 0f;
            if (chance >= 1f) return 1f;
            float lo = 0f, hi = chance;
            for (int i = 0; i < 32; i++)
            {
                float mid = (lo + hi) * 0.5f;
                if (PrdAverage(mid) > chance) hi = mid;
                else lo = mid;
            }
            return (lo + hi) * 0.5f;
        }

        // Long-run success rate of a PRD with constant c: one over the expected tries per success
        public static float PrdAverage(float c)
        {
            if (c <= 0f) return 0f;
            double noSuccessYet = 1.0, expectedTries = 0.0;
            int maxTries = (int)math.ceil(1f / c);
            for (int n = 1; n <= maxTries; n++)
            {
                double p = math.min(1.0, (double)c * n);
                expectedTries += n * noSuccessYet * p;
                noSuccessYet *= 1.0 - p;
            }
            return (float)(1.0 / expectedTries);
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
