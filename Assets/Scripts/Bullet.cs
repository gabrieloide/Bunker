using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed;
    [HideInInspector] public Vector3 target;
    protected string hitName;
    protected float Damage, BulletPen;
    [SerializeField] protected float timeToDestroy = 5;
    private Rigidbody2D RB2d;
    private Vector2 dir;


    private void OnEnable()
    {
        StartCoroutine(DeactivateBullet());
    }
    private void Start()
    {
        RB2d = gameObject.GetComponent<Rigidbody2D>();
    }
    public void GetData(Vector3 _target, float _Damage, float _lifeBullet)
    {
        target = _target;
        Damage = _Damage;
        BulletPen = _lifeBullet;
        dir = target - transform.position;
    }
    private void FixedUpdate()
    {
        RB2d.velocity = dir.normalized * speed;
    }
    IEnumerator DeactivateBullet()
    {
        yield return new WaitForSeconds(timeToDestroy);
        gameObject.SetActive(false);
        transform.rotation = Quaternion.Euler(Vector3.zero);
        GameObject PoolingPos = GameObject.Find("Pooling");
        transform.position = PoolingPos.transform.position;

    }
    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();
        if (damageable != null && collision.CompareTag(hitName))
        {
            damageable.Damage(Damage, BulletPen, gameObject);
        }
    }
}
