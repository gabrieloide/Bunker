using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Sounds")]
    [SerializeField] AK.Wwise.Event ShowBoxText;
    [Space]
    
    public static UIManager instance;
    public Texture2D cursorDefault, cursorTexture;
    [Space]
    public TMP_Text waveText, scoreText;
    [Space]
    public bool ShowTowerSlot;
    public GameObject TowerSlotAnimation;
    public Vector3 offset;
    [Space]
    public GameObject CardStats;
    [HideInInspector] public GameObject cardInstantiate;
    public GameObject Canvas2;
    [Space]
    [Header("Last Card")]
    public GameObject LastPosCard;
    public float TimeLastPosCard;
    [Space]
    [Header("Life Turret")]
    [SerializeField] Slider LifeSlider;
    [Space]
    [Header("Deck")]
    public LeanTweenType TweenDeckIn;
    public LeanTweenType TweenDeckOut;
    public GameObject Deck;
    public float posInCamera;
    public float TimeMovement;
    float currentLife()
    {
        float ActualLife = TowerPlayer.instance.life / 100;
        return ActualLife;
    }
    void Awake()
    {
        if (!instance)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    void Update()
    {
        int score = GameManager.instance.ActualScore;
        string scoreString = score.ToString("D6");

        scoreText.text = $"Score: {scoreString}";
        waveText.text = $"Wave: {WaveManager.instance.Wave.ToString("D6")}";
        LifeSlider.value = currentLife();
        showTowerSlotAnimation();
        ShowDeck();
    }
    void showTowerSlotAnimation()
    {
        if (ShowTowerSlot)
        {
            TowerSlotAnimation.SetActive(true);
            TowerSlotAnimation.transform.position = Camera.main.ScreenToWorldPoint(Input.mousePosition) - offset;
        }
        else
            TowerSlotAnimation.SetActive(false);
    }
    public void ShowCardBox(string _name, string _description, Vector3 TC, bool onDrag)
    {
        if (cardInstantiate == null)
        {
            ShowBoxText.Post(gameObject);
            cardInstantiate = Instantiate(CardStats, Canvas2.transform);
            cardInstantiate.transform.position = TC;
            FindObjectOfType<ChangeCardText>().instantiateStats(_name, _description);
        }
    }
    public void ShowLastCardPosition(Vector3 CardPosition)
    {
        bool isNotDragging = !GameManager.instance.onDrag;

        LastPosCard.SetActive(!isNotDragging);

        if (isNotDragging)
        {
            LastPosCard.transform.localScale = Vector2.zero;
        }
        else
        {
            LastPosCard.transform.position = CardPosition;
        }
    }
    public void ShowDeck()
    {
        if (CardDrop.instance.cardsQueue.Count > 0)
        {
            this.Deck.SetActive(true);
        }
    }
}
