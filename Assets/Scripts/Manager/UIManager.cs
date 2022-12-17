using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;
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
}
