using UnityEngine;

[CreateAssetMenu(fileName = "WaveBalanceConfig", menuName = "Bunker/Wave Balance Config")]
public class WaveBalanceConfig : ScriptableObject
{
    [Header("Spawning")]
    [Tooltip("Seconds before the first enemy of the run")]
    [Min(0f)] public float startDelay = 3f;
    [Tooltip("Seconds between two spawns inside a wave")]
    [Min(0.05f)] public float spawnInterval = 1f;
    [Tooltip("Enemies in the first wave")]
    [Min(1)] public int initialEnemyAmount = 15;

    [Header("Wave progression")]
    public int wavesPerTier = 10;
    public int initialUnlockedTypes = 2;
    public int maxUnlockedTypes = 9;
    public int amountIncreasePerTier = 5;
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
