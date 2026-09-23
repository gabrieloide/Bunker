using UnityEngine;

[CreateAssetMenu(fileName = "New Air Strike Card", menuName = "Bunker/Cards/Air Strike")]
public class AirStrikeCardDefinition : CardDefinition
{
    [Header("Air Strike")]
    [Min(0f)] public float damage = 15f;
    [Min(0f)] public float bulletPen = 8f;
    [Min(1)] public int shotCount = 5;
    [Tooltip("Seconds between the plane's shots")]
    [Min(0.02f)] public float shotInterval = 0.37f;
}
