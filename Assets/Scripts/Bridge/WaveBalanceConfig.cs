using UnityEngine;

[CreateAssetMenu(fileName = "WaveBalanceConfig", menuName = "Bunker/Wave Balance Config")]
public class WaveBalanceConfig : ScriptableObject
{
    [Header("Spawning")]
    [Tooltip("Seconds of preparation phase to place cards before each wave starts")]
    [Min(0f)] public float startDelay = 25f;
    [Tooltip("Seconds between two spawns inside a wave")]
    [Min(0.05f)] public float spawnInterval = 1f;
    [Tooltip("Enemies in the first wave")]
    [Min(1)] public int initialEnemyAmount = 15;
    [Tooltip("Enemies spawned back to back as one group; spawnInterval is the wait between groups")]
    [Min(1)] public int groupSize = 1;
    [Tooltip("Seconds between the members of one group (0 = all on the same frame, stacked)")]
    [Min(0f)] public float groupGap = 0.2f;
    [Tooltip("Every enemy walks at its own speed times this (2 = twice as fast)")]
    [Min(0.05f)] public float enemySpeedMultiplier = 1f;

    [Header("Wave progression")]
    public int wavesPerTier = 10;
    public int initialUnlockedTypes = 2;
    public int maxUnlockedTypes = 9;
    public int amountIncreasePerTier = 5;
    [Tooltip("Extra enemies added after every wave, on top of the per-tier increase")]
    [Min(0)] public int amountIncreasePerWave = 0;
    [Tooltip("Enemy life grows by this fraction every wave (0.1 = +10% per wave, linear)")]
    [Min(0f)] public float lifeGrowthPerWave = 0.1f;

    [Header("Boss")]
    [Tooltip("Every N waves the last spawn is the boss; 0 disables bosses")]
    [Min(0)] public int bossEveryWaves = 10;
    [Tooltip("Roster index of the boss; keep it >= maxUnlockedTypes so it never spawns as a regular enemy")]
    public int bossTypeIndex = 9;

    [Header("Enemy buff roll weights")]
    public float weightNormal = 60f;
    public float weightAttack = 13f;
    public float weightDefense = 7f;
    public float weightVelocity = 6f;
    public float weightLife = 5f;
    public float weightFireRate = 8f;

    [Header("Enemy buff multipliers")]
    public float lifeMultiplier = 2f;
    public float damageMultiplier = 1.5f;
    public float fireRateMultiplier = 1.7f;
    public float defenseMultiplier = 1.3f;
    public float speedMultiplier = 1.5f;
}
