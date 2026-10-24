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
    [Range(1f, 20f)] public float range = 4f;
    [Tooltip("Seconds it stays over the drop point before flying home")]
    [Min(0.5f)] public float hoverTime = 8f;
    [Tooltip("Flight speed to and from the drop point")]
    [Min(0.1f)] public float flySpeed = 6f;
}
