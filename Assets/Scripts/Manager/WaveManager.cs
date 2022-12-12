using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public GameObject[] Waves;
    EnemySpawner enemySpawner;
    private void Start()
    {
        enemySpawner = FindObjectOfType<EnemySpawner>();
        enemySpawner.inGame = true;
        StartCoroutine(Wave());
    }
    IEnumerator Wave()
    {
        yield return new WaitForSeconds(100f);
        Waves[0].SetActive(false);
        Waves[1].SetActive(true);
        yield return new WaitForSeconds(170f);
        Waves[1].SetActive(false);
        Waves[2].SetActive(true);
    }
}