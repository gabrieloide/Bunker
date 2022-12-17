using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TowerSlot : MonoBehaviour
{
    public bool slotAvailable;

    private void Start()
    {
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