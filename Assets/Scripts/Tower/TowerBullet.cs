using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TowerBullet : MonoBehaviour
{
    [SerializeField]
    private float speed;
    public int damage, lifeBullet;
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

    public void GetData(Transform _target, int _damage, int _lifeBullet)
    {
        target = _target;
        damage = _damage;
        lifeBullet = _lifeBullet;
        dir = target.position - transform.position;
    }

    private void Update()
    {
        timeToDestroy -= Time.deltaTime;
        if (lifeBullet <= 0 || timeToDestroy < 0)
        {
            gameObject.SetActive(false);
            lifeBullet = 1;
            timeToDestroy = TimeToDestroy;
            transform.rotation = Quaternion.Euler(Vector3.zero);
            GameObject PoolingPos = GameObject.Find("Pooling");
            transform.position = PoolingPos.transform.position;
        }
    }
    private void FixedUpdate()
    {
        RB2d.velocity = dir.normalized * speed;
    }
}
