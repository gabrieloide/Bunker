using Unity.Entities;
using Unity.Mathematics;

namespace Bunker.Simulation
{
    public enum Faction : byte { Tower = 0, Enemy = 1 }

    // Same order as the game-side BuffEnemyType enum so values can be cast directly.
    public enum EnemyBuffKind : byte { None = 0, Attack = 1, Defense = 2, Velocity = 3, Life = 4, FireRate = 5 }

    public enum ViewKind : byte { Enemy = 0, TowerProjectile = 1, EnemyProjectile = 2, ArtilleryShell = 3, Ally = 4 }

    public enum WavePhase : byte { WaitingToStart = 0, Spawning = 1, WaitingForClear = 2 }

    public enum SimEventKind : byte
    {
        Fired,
        ProjectileHit,
        EnemyDied,
        TowerDied,
        EnemyReachedEnd,
        PlayerHit,
        WaveChanged,
        ScoreChanged,
        GameOver,
        ShellImpact,
        AllyDied,
        // Melee blow (ally on an enemy or on the enemy base); Amount = what the target actually loses
        MeleeHit,
        // Amount = damage taken, IntValue = remaining life rounded
        EnemyBaseHit,
        Victory
    }

    public struct SimulationTag : IComponentData { }
    public struct TowerTag : IComponentData { }
    public struct EnemyTag : IComponentData { }
    public struct BunkerTag : IComponentData { }
    // Player units that walk the enemy path backwards to the enemy base
    public struct AllyTag : IComponentData { }
    // Destroying it wins the match
    public struct EnemyBaseTag : IComponentData { }

    public struct GameSession : IComponentData
    {
        public int Score;
        public bool IsGameOver;
    }

    public struct Health : IComponentData
    {
        public float Value;
        public float Max;
    }

    public struct Defense : IComponentData { public float Value; }
    public struct MoveSpeed : IComponentData { public float Value; }
    public struct ScoreValue : IComponentData { public int Value; }

    public struct PathFollower : IComponentData { public int NextIndex; }
    public struct AtPathEnd : IComponentData { public float AttackTimer; }
    public struct PathPoint : IBufferElementData { public float3 Value; }

    public struct HitBox : IComponentData
    {
        public float2 HalfExtents;
        public float2 Offset;
    }

    public struct Weapon : IComponentData
    {
        public float Damage;
        public float BulletPen;
        // Rest after a full burst; with BurstCount <= 1 it is simply the time between shots
        public float FireInterval;
        public int BurstCount;
        public float BurstInterval;
        public int ShotsLeftInBurst;
        public float Range;
        public float Cooldown;
        public float ProjectileSpeed;
        public float ProjectileLifetime;
        public float2 ProjectileHalfExtents;
        public float3 MuzzleOffset;
        public Faction TargetFaction;
        public bool HasBuff;
    }

    public struct Target : IComponentData { public Entity Value; }

    // Hits whatever is within Range once per Interval, no projectile
    public struct Melee : IComponentData
    {
        public float Damage;
        public float BulletPen;
        public float Interval;
        public float Range;
        public float Cooldown;
    }

    // Walks the PathPoint buffer from the end towards the start; below 0 it heads for the enemy base
    public struct AllyPathFollower : IComponentData { public int NextIndex; }

    public struct Projectile : IComponentData
    {
        public float Damage;
        public float BulletPen;
        public float Lifetime;
        public float3 Velocity;
        public float2 HalfExtents;
        public Faction TargetFaction;
    }

    // Tower lobs shells (straight up, then free fall onto the predicted impact) instead of firing straight projectiles
    public struct Artillery : IComponentData
    {
        public float RiseTime;
        public float FallTime;
        public float Height;
        public float SplashRadius;
        public int ViewId;
    }

    public struct ArtilleryShell : IComponentData
    {
        public float3 Origin;
        public float3 Impact;
        public float Elapsed;
        public float RiseTime;
        public float FallTime;
        public float Height;
        public float Damage;
        public float BulletPen;
        public float SplashRadius;
        public int ViewId;
    }

    public struct DamageRequest : IBufferElementData
    {
        public float Damage;
        public float BulletPen;
    }

    public struct NeedsView : IComponentData
    {
        public ViewKind Kind;
        public int PrefabId;
    }

    public struct SimEvent : IBufferElementData
    {
        public SimEventKind Kind;
        public Entity Source;
        public Entity Target;
        public float3 Position;
        public float Amount;
        public int IntValue;
    }

    public struct EnemyTypeDef
    {
        public float Life;
        public float Damage;
        public float FireInterval;
        public float Defense;
        public float MoveSpeed;
        public float AttackRange;
        public float2 HitHalfExtents;
        public float2 HitOffset;
        public int Score;
    }

    public struct EnemyRosterBlob
    {
        public BlobArray<EnemyTypeDef> Types;
    }

    public struct EnemyBuffTableBlob
    {
        public BlobArray<float> Weights;
        public float LifeMultiplier;
        public float DamageMultiplier;
        public float FireRateMultiplier;
        public float DefenseMultiplier;
        public float SpeedMultiplier;
    }

    public struct AllyTypeDef
    {
        public float Life;
        public float Damage;
        public float BulletPen;
        public float AttackInterval;
        public float AttackRange;
        public float MoveSpeed;
        public float2 HitHalfExtents;
        public float2 HitOffset;
    }

    public struct AllyRosterBlob
    {
        public BlobArray<AllyTypeDef> Types;
    }

    public struct AllySpawnConfig : IComponentData
    {
        // Seconds between two allies; <= 0 disables them
        public float Interval;
        public float3 SpawnPosition;
        public BlobAssetReference<AllyRosterBlob> Roster;
    }

    public struct AllySpawnState : IComponentData
    {
        public float Timer;
        public int NextType;
    }

    public struct WaveConfig : IComponentData
    {
        public float StartDelay;
        public float SpawnInterval;
        public int InitialEnemyAmount;
        public int AmountIncreasePerTier;
        public int WavesPerTier;
        public int InitialUnlockedTypes;
        public int MaxUnlockedTypes;
        public float LifeGrowthPerWave;
        public int BossEveryWaves;
        public int BossTypeIndex;
        public float PlayerDamageInterval;
        public float EnemyProjectileSpeed;
        public float EnemyProjectileLifetime;
        public float2 EnemyProjectileHalfExtents;
        public float3 SpawnPosition;
        public BlobAssetReference<EnemyRosterBlob> Roster;
        public BlobAssetReference<EnemyBuffTableBlob> BuffTable;
    }

    public struct WaveState : IComponentData
    {
        public int Wave;
        public int EnemiesAlive;
        public int SpawnedThisWave;
        public int EnemyAmount;
        public int UnlockedTypes;
        public float Timer;
        public WavePhase Phase;
        public EnemyBuffKind Buff;
        public Random Rng;
    }
}
