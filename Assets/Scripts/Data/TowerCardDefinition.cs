using UnityEngine;

[CreateAssetMenu(fileName = "New Tower Card", menuName = "Bunker/Cards/Tower")]
public class TowerCardDefinition : CardDefinition
{
    [Header("Tower")]
    [Min(1f)] public float life = 100f;
    [Min(0f)] public float damage = 6f;
    [Tooltip("Armor ignored per hit")]
    [Min(0f)] public float bulletPen = 1f;
    [Tooltip("Bursts per second (the rest between bursts); plain shots per second when Burst Count is 1")]
    [Min(0f)] public float fireRate = 1f;
    [Tooltip("Quick shots per burst (PvZ Threepeater style); 1 = no burst")]
    [Min(1)] public int burstCount = 1;
    [Tooltip("Seconds between the shots inside a burst")]
    [Min(0.02f)] public float burstInterval = 0.12f;
    [Range(3f, 20f)] public float range = 3f;
    [Tooltip("Seconds before the first shot after being placed")]
    [Min(0f)] public float initialCooldown = 1.5f;
    [Tooltip("Copies of this tower allowed on the map at once; 0 = only the catalog's global limit applies")]
    [Min(0)] public int maxOnField = 0;
}
