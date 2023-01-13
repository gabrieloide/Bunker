using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;
    public GameObject Stats;
    [SerializeField] Text Name;
    public Texture2D cursorDefault;
    public Texture2D cursorTexture;
    public Vector2 cursorHotspot;
    [Space]
    public Text waveText;
    public Text scoreText;
    [HideInInspector]public int score;
    [HideInInspector]public int Wave;
    [Space]
    public GameObject TowerSlotAnimation;
    Vector3 mousePosition;
    public Vector3 offset;
    private void Start()
    {
        if (!instance)
        {
            instance = this;
        }
    }
    void Update()
    {
        mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        scoreText.text = $"Score: {score}";
        waveText.text = $"Wave: {WaveManager.instance.Wave.ToString()}";
        showTowerSlotAnimation();
    }

    void showTowerSlotAnimation()
    {
        if (FindObjectOfType<Card>() != null)
        {
            if (FindObjectOfType<Card>().onDrag)
            {
                //Agregar offset al towerslot
                mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                TowerSlotAnimation.SetActive(true);
                TowerSlotAnimation.transform.position = mousePosition - offset;
            }
            else
            {
                TowerSlotAnimation.SetActive(false);
            }
        }
    }
    public void showStatsCards(string name, Vector2 newPosition)
    {
        Vector2 offset = new Vector2(0, 0.5f);
        Name.gameObject.transform.position = newPosition+ offset;
        Stats.SetActive(true);
        Name.text = name;
    }
}
