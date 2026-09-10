using UnityEngine;

// Scene authoring for wave spawning. The wave loop itself runs in Bunker.Simulation.WaveSystem.
public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner instance;
    public float StartEnemySpawner;
    public float EnemyDelay;
    public int EnemyAmount = 15;
    public GameObject[] Enemies;
    public int enemiesAlive;

    private void Awake()
    {
        if (!instance)
            instance = this;
        else
            Destroy(gameObject);
    }
}
