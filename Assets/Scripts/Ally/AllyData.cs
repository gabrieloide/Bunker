using UnityEngine;

// Every gameplay number of an ally type. The prefab (AllyView) only carries presentation.
[CreateAssetMenu(fileName = "New Ally Data", menuName = "Bunker/Ally Data")]
public class AllyData : ScriptableObject
{
    [Min(1f)] public float life = 30f;
    [Tooltip("Damage per melee blow")]
    [Min(0f)] public float damage = 5f;
    [Tooltip("Enemy armor ignored per blow")]
    [Min(0f)] public float bulletPen = 1f;
    [Tooltip("Seconds between blows")]
    [Min(0.05f)] public float attackInterval = 1f;
    [Tooltip("Distance at which it stops to hit an enemy or the enemy base")]
    [Min(0.1f)] public float attackRange = 1.2f;
    [Min(0.1f)] public float moveSpeed = 1.8f;
}
