using UnityEngine;

[CreateAssetMenu(fileName = "WaveBalanceConfig", menuName = "Bunker/Wave Balance Config")]
public class WaveBalanceConfig : ScriptableObject
{
    [Header("Wave progression")]
    public int wavesPerTier = 10;
    public int initialUnlockedTypes = 2;
    public int maxUnlockedTypes = 9;
    public int amountIncreasePerTier = 5;

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
