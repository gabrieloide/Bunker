using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;
    public GameObject Stats;
    [SerializeField] Text Name;

    public Text waveText;
    public Text scoreText;
    public int score;
    public int Wave;
    private void Start()
    {
        if (!instance)
        {
            instance = this;
        }
    }
    void Update()
    {
        scoreText.text = $"Score: {score}";
        waveText.text = $"Wave: {WaveManager.instance.Wave.ToString()}";
    }
    public void showStatsCards(string name, Vector2 newPosition)
    {
        Vector2 offset = new Vector2(0, 0.5f);
        Name.gameObject.transform.position = newPosition+ offset;
        Stats.SetActive(true);
        Name.text = name;
    }
}
