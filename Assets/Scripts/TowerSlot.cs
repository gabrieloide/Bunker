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