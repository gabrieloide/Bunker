using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;
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
    [HideInInspector]public GameObject cardInstantiate;
    public GameObject Canvas2;
    [Space]
    public GameObject DeckSlot;
    public float MoveX;
    public float time;
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
    public void ShowCardBox(string _name, string _description, Vector3 TC)
    {
        if (cardInstantiate == null)
        {
            cardInstantiate = Instantiate(CardStats, Canvas2.transform);
            cardInstantiate.transform.position = TC;
            FindObjectOfType<ChangeCardText>().instantiateStats(_name, _description);
        }
    }
    public void ShowDeckSlot(LeanTweenType leanTweenType,int cardQueue, 
        RectTransform rectTransform)
    {
        LeanTween.cancel(rectTransform);
        if (cardQueue > 1)
        {
            LeanTween.moveX(rectTransform, 138, time).setEase(leanTweenType);
        }
        else
        {
            LeanTween.moveX(rectTransform, 270, time).setEase(leanTweenType);
        }
    }
}
