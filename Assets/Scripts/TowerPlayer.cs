using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TowerPlayer : MonoBehaviour
{
    public static TowerPlayer instance;
    public GameObject loseScreen;
    public float life;
    private void Update()
    {
        if (life <= 0)
        {
            loseScreen.SetActive(true);
        }
    }
    private void Awake()
    {
        if (!instance)
        {
            instance = this;
        }
    }
}