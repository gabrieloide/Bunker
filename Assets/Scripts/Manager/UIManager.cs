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
    public Text waveText, scoreText;
    [Space]
    public bool ShowTowerSlot;
    public GameObject TowerSlotAnimation;
    public Vector3 offset;
    [Space]
    public GameObject CardStats;
    public LeanTweenType leanTweenType;
    [HideInInspector] public GameObject cardInstantiate;
    public GameObject Canvas2;
    [Space]
    public GameObject DeckSlot;
    public float TimeShowDeckSlot;
    [Space]
    [Header("Last Card")]
    public GameObject LastPosCard;
    public float TimeLastPosCard;
    [Space]
    [Header("Life Turret")]
    [SerializeField] Slider LifeSlider;
    float currentLife()
    {
        float ActualLife = TowerPlayer.instance.life/100;
        return ActualLife;
    }
    public bool returnCard()
    {
        if (FindObjectOfType<Card>() != null)
        {
            return true;
        }
        return false;
    }
    void Awake()
    {
        if (!instance)
        {
            instance = this;
        }
    }
    void Update()
    {
        //scoreText.text = $"Score: {GameManager.instance.ActualScore}";
        //waveText.text = $"Wave: {WaveManager.instance.Wave.ToString()}";
        LifeSlider.value = currentLife();
        showTowerSlotAnimation();
    }
    void showTowerSlotAnimation()
    {
        if (returnCard() && ShowTowerSlot)
        {
            TowerSlotAnimation.SetActive(true);
            TowerSlotAnimation.transform.position = Camera.main.ScreenToWorldPoint(Input.mousePosition) - offset;
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
    public void ShowLastCardPosition(Vector3 CardPos, bool show)
    {
        if (show)
        {
            LastPosCard.SetActive(show);
            Vector3 offset = new Vector3(0, 0.01f,0);
            LastPosCard.transform.position = CardPos - offset;
            LeanTween.scale(LastPosCard, Vector3.one * 15, TimeLastPosCard).setEaseOutBack();
        }
        else
        {
            LastPosCard.SetActive(show);
            LastPosCard.transform.localScale = Vector3.zero;
        }
    }
}
