using UnityEngine;

// A tower buff is just the numbers it changes: 1 = unchanged. Combine as many as you like on one card.
[CreateAssetMenu(fileName = "New Buff Card", menuName = "Bunker/Cards/Tower Buff")]
public class BuffCardDefinition : CardDefinition
{
    [Header("Buff (1 = no change)")]
    [Min(0f)] public float damageMultiplier = 1f;
    [Min(0f)] public float fireRateMultiplier = 1f;
    [Min(0f)] public float rangeMultiplier = 1f;
    [Min(0f)] public float lifeMultiplier = 1f;
    [Tooltip("Flat armor penetration added")]
    [Min(0f)] public float bulletPenBonus = 0f;
}
