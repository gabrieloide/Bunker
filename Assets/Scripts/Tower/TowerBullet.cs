using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TowerBullet : MonoBehaviour
{
    [SerializeField]
    private float speed;
    public int damage, lifeBullet;
    [SerializeField]
    private float timeToDestroy;
    [HideInInspector] public Transform target;
    private Rigidbody2D RB2d;
    private Vector2 dir;

    private void Start()
    {
        RB2d = gameObject.GetComponent<Rigidbody2D>();
        Destroy(gameObject, timeToDestroy);
    }

    public void GetData(Transform _target, int _damage, int _lifeBullet)
    {
        target = _target;
        damage = _damage;
        lifeBullet = _lifeBullet;
        dir = target.position - transform.position;
    }

    private void Update()
    {
        if (lifeBullet <= 0)
        {
            Destroy(gameObject);
        }
    }
    private void FixedUpdate()
    {
        RB2d.velocity = dir.normalized * speed;
    }
}
