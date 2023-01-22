using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TowerPlayer : MonoBehaviour
{
    public static TowerPlayer instance;
    public GameObject loseScreen;
    private void Update()
    {
        if (life <= 0)
        {
            loseScreen.SetActive(true);
        }
        if (Input.GetMouseButtonDown(1) && UIManager.instance.asdf != null)
        {
            Destroy(UIManager.instance.asdf);
        }
    }
    public float life;
    private void Awake()
    {
        if (!instance)
        {
            instance = this;
        }
    }
}