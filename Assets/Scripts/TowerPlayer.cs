using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TowerPlayer : MonoBehaviour
{
    public static TowerPlayer instance;
    public float life;
    private void Awake()
    {
        if (!instance)
        {
            instance = this;
        }
    }
}