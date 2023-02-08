using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;
    public GameObject Stats;
    [SerializeField] Text Name;
    public Texture2D cursorDefault, cursorTexture;
    [Space]
    public Text waveText;
    public Text scoreText;
    [HideInInspector]public int score;
    [HideInInspector]public int Wave;
    [Space]
    public bool ShowTowerSlot;
    public GameObject TowerSlotAnimation;
    Vector3 mousePosition;
    public Vector3 offset;
    [Space]
    public GameObject CardStats;
    public LeanTweenType leanTweenType;
    [HideInInspector]public GameObject c;
    public GameObject Canvas2;
    public bool returnCard()
    {
        if (FindObjectOfType<Card>() != null)
        {
            return true;
        }
        return false;
    }
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
        if (returnCard() && ShowTowerSlot)
        {
            //Agregar offset al towerslot
            mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            TowerSlotAnimation.SetActive(true);
            TowerSlotAnimation.transform.position = mousePosition - offset;
        }
         else
            TowerSlotAnimation.SetActive(false);
    }
    public void showStatsCards(string name, Vector3 newPosition)
    {
        LeanTween.cancel(Stats);
        LeanTween.moveY(Stats.GetComponent<RectTransform>(), 1, 0.5f).setEase(leanTweenType);
        Vector3 offset = new Vector3(0, 0.5f,0);
        //No hace la animacion de nuevo porque se queda en la ultima posicion
        Name.gameObject.transform.position = newPosition + offset;
        Stats.SetActive(true);
        Name.text = name;
        Stats.transform.position -= new Vector3(0, 1, 0);
    }
    public void ShowCardBox(string _name, string _description, Vector3 TC)
    {
        if (c == null)
        {
            c = Instantiate(CardStats, Canvas2.transform);
            c.transform.position = TC;
            FindObjectOfType<ChangeCardText>().instantiateStats(_name, _description);
        }
    }
}
