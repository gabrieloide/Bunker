using UnityEngine;

// Scene authoring for wave spawning: spawn point and roster order (unlock order; the boss index points here).
// Timings and amounts live in WaveBalanceConfig; the wave loop runs in Bunker.Simulation.WaveSystem.
public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner instance;
    public GameObject[] Enemies;

    private void Awake()
    {
        if (!instance)
            instance = this;
        else
            Destroy(gameObject);
    }
}
