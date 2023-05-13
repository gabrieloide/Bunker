using UnityEngine;

public class TowerBullet : MonoBehaviour
{
    [SerializeField]
    private float speed;
    public float Damage, BulletPen;
    private float timeToDestroy;
    public float TimeToDestroy;
    [HideInInspector] public Transform target;
    private Rigidbody2D RB2d;
    private Vector2 dir;

    private void Start()
    {
        timeToDestroy = TimeToDestroy;
        RB2d = gameObject.GetComponent<Rigidbody2D>();
    }
    public void GetData(Transform _target, float _Damage, float _lifeBullet)
    {
        target = _target;
        Damage = _Damage;
        BulletPen = _lifeBullet;
        dir = target.position - transform.position;
    }

    private void Update()
    {
        timeToDestroy -= Time.deltaTime;
        if (timeToDestroy < 0)
        {
            gameObject.SetActive(false);
            timeToDestroy = TimeToDestroy;
            transform.rotation = Quaternion.Euler(Vector3.zero);
            GameObject PoolingPos = GameObject.Find("Pooling");
            transform.position = PoolingPos.transform.position;
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.Damage(Damage, BulletPen, gameObject);
        }
    }
    private void FixedUpdate()
    {
        RB2d.velocity = dir.normalized * speed;
    }
}
