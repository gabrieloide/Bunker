using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class bulletMovement : MonoBehaviour
{
    BoxCollider2D boxCollider2D;
    [SerializeField] float speed;
    bool canMove;
    void Start()
    {
        boxCollider2D = GetComponent<BoxCollider2D>();
        canMove = true;
    }

    // Update is called once per frame
    void Update()
    {
        //Al instanciar 2 veces se van a la misma direccion
        float d = Vector2.Distance(transform.position, FindObjectOfType<AirAttack>().target);
        if (d > 0.1)
        {
            if (canMove)
            {
                transform.position = Vector3.MoveTowards(transform.position, FindObjectOfType<AirAttack>().target, speed * Time.deltaTime);
            }
        }
        else
        {
            canMove = false;
            boxCollider2D.enabled = true;
            Destroy(gameObject, 3);
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            collision.GetComponent<Enemy>().Life -= FindObjectOfType<AirAttack>().damage;
        }
    }
}
