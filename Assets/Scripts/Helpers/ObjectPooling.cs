using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPooling : MonoBehaviour
{
    public static ObjectPooling instance;
    public List<GameObject> bulletsPooling = new List<GameObject>();
    [SerializeField] GameObject turretBullet;
    [SerializeField] int amountToPool;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private void Start()
    {
        for (int i = 0; i < amountToPool; i++)
        {
            GameObject newBullet= Instantiate(turretBullet, transform.position, transform.rotation, transform);
            newBullet.SetActive(false);
            bulletsPooling.Add(newBullet);
        }
    }
    public GameObject Shoot()
    {
        for (int i = 0; i < bulletsPooling.Count; i++)
        {
            if (!bulletsPooling[i].activeSelf)
            {
                bulletsPooling[i].SetActive(true);
                return bulletsPooling[i];
            }
        }
        GameObject newBullet = new GameObject();
        newBullet.SetActive(true);
        bulletsPooling.Add(newBullet);
        return newBullet;
    }
}
