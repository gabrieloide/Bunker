using UnityEngine;

[CreateAssetMenu(fileName = "New Enemy Data", menuName = "Enemy Data")]
public class EnemyData : ScriptableObject
{
    [SerializeField] float _life;
    [SerializeField] float _damage;
    [SerializeField] float _fireRate;
    [SerializeField] float _defense;
    [SerializeField] float _moveSpeed = 2;
    public int Score;

    public float BaseLife => _life;
    public float BaseDamage => _damage;
    public float BaseFireRate => _fireRate;
    public float BaseDefense => _defense;
    public float BaseMoveSpeed => _moveSpeed;
}
