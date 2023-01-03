using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneMovement : MonoBehaviour
{
    [SerializeField] float speed;
    [SerializeField] GameObject bullet;
    bool spawned;
    // Update is called once per frame
    private void Start()
    {
        Destroy(gameObject, 15);
        spawned = true;
    }
    void Update()
    {
        transform.position = new Vector2(transform.position.x + speed * Time.deltaTime, transform.position.y);
        float d = Vector2.Distance(transform.position, FindObjectOfType<AirAttack>().target);
        Debug.Log(d);

        if (d < 9)
        {
            if (spawned)
            {
                Instantiate(bullet, transform.position, transform.rotation);
                spawned = false;
            }
        }
    }
}
