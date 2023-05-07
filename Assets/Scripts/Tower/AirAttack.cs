using UnityEngine;

public class AirAttack : MonoBehaviour
{
    public GameObject Plane;
    public TowersData AirAttackData;
    [SerializeField]public float damage;
    public Vector3 offset;
    public Vector2 target;
    int planeAmount;
    private void Awake()
    {
        target = transform.position;
        damage = AirAttackData.damage;
    }
    private void Start()
    {
        Instantiate(Plane, transform.position + offset, transform.rotation);
        Destroy(gameObject, 16);
    }
}
