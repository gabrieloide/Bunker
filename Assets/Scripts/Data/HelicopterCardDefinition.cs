using UnityEngine;

[CreateAssetMenu(fileName = "New Helicopter Card", menuName = "Bunker/Cards/Helicopter")]
public class HelicopterCardDefinition : CardDefinition
{
    [Header("Helicopter")]
    [Tooltip("Enemies can shoot it down while it hovers")]
    [Min(1f)] public float life = 60f;
    [Min(0f)] public float damage = 5f;
    [Tooltip("Armor ignored per hit")]
    [Min(0f)] public float bulletPen = 2f;
    [Tooltip("Shots per second while hovering")]
    [Min(0f)] public float fireRate = 3f;
    [Tooltip("Shooting range")]
    [Range(1f, 20f)] public float range = 4f;
    [Tooltip("Damage multiplier against the enemy base (0.2 = 20%), so it cannot solo it")]
    [Range(0f, 1f)] public float baseDamageMultiplier = 0.2f;
    [Tooltip("Radius of the zone it patrols around the drop point; it chases enemies inside it")]
    [Min(0.5f)] public float patrolRadius = 3f;
    [Tooltip("Speed while patrolling the zone")]
    [Min(0.1f)] public float patrolSpeed = 2f;
    [Tooltip("Seconds it patrols the zone before flying home")]
    [Min(0.5f)] public float hoverTime = 8f;
    [Tooltip("Flight speed to and from the drop point")]
    [Min(0.1f)] public float flySpeed = 6f;
}
