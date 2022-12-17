using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TowerSlot : MonoBehaviour
{
    public bool slotAvailable;
    Animator animator;
    private void Start()
    {
        slotAvailable = true;
        animator = GetComponent<Animator>();
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("MainCamera"))
        {
            animator.SetBool("animationOn", true);
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.GetComponent<Card>() != null && !collision.GetComponent<Card>().onDrag && slotAvailable)
        {
            Instantiate(collision.GetComponent<Card>().tower, transform.position, transform.rotation);
            Destroy(collision.gameObject);
            slotAvailable = false;
        }
        if (collision.gameObject.CompareTag("MainCamera"))
        {
            animator.SetBool("animationOn", false);
        }
        slotAvailable = true;
    }

    public void SpawnTower(GameObject towerObject)
    {
        Instantiate(towerObject, transform.position, transform.rotation);
        slotAvailable = false;
    }

    public void TowerDelete()
    {
        slotAvailable = true;
    }
}