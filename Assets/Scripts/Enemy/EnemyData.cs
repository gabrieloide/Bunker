using UnityEngine;

// Every gameplay number of an enemy type. The prefab only carries presentation. Edit all enemies at once from Bunker > Balance.
[CreateAssetMenu(fileName = "New Enemy Data", menuName = "Enemy Data")]
public class EnemyData : ScriptableObject
{
    [SerializeField] float _life;
    [SerializeField] float _damage;
    [Tooltip("Seconds between shots")]
    [SerializeField] float _attackInterval = 1.5f;
    [Tooltip("Distance at which it stops and starts shooting")]
    [Range(3, 20)][SerializeField] float _attackRange = 3f;
    [Tooltip("Flat damage soaked per hit; bullet penetration cancels it point for point")]
    [SerializeField] float _defense;
    [SerializeField] float _moveSpeed = 2;
    public int Score;

    public float BaseLife => _life;
    public float BaseDamage => _damage;
    public float BaseAttackInterval => _attackInterval;
    public float BaseAttackRange => _attackRange;
    public float BaseDefense => _defense;
    public float BaseMoveSpeed => _moveSpeed;
}
