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

    private Camera mainCamera;
    private int lastScore = -1;
    private int lastWave = -1;
    private float lastLife = -1f;
    private ChangeCardText cachedCardText;

    void Awake()
    {
        if (!instance)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        mainCamera = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
    }

    private void OnEnable()
    {
        if (instance != null && instance != this) return;

        SetScore(SimEventDispatcher.Score, 0);
        SetWave(SimEventDispatcher.Wave, SimEventDispatcher.CurrentBuff);
        SetLifeNormalized(SimEventDispatcher.Health01);

        SimEventDispatcher.OnScoreChanged += SetScore;
        SimEventDispatcher.OnWaveChanged += SetWave;
        SimEventDispatcher.OnPlayerHit += OnPlayerHit;
        SimEventDispatcher.OnBunkerHealed += OnBunkerHealed;
    }

    private void OnDisable()
    {
        SimEventDispatcher.OnScoreChanged -= SetScore;
        SimEventDispatcher.OnWaveChanged -= SetWave;
        SimEventDispatcher.OnPlayerHit -= OnPlayerHit;
        SimEventDispatcher.OnBunkerHealed -= OnBunkerHealed;
    }

    private void SetScore(int newScore, int delta)
    {
        if (scoreText != null && newScore != lastScore)
        {
            lastScore = newScore;
            scoreText.SetText("Score: {0:000000}", newScore);
        }
    }

    private void SetWave(int newWave, Bunker.Simulation.EnemyBuffKind buff)
    {
        if (waveText != null && newWave != lastWave)
        {
            lastWave = newWave;
            waveText.SetText("Wave: {0:000000}", newWave);
        }
    }

    private void OnPlayerHit(float damage, int remainingHP, float normalizedHP)
    {
        SetLifeNormalized(normalizedHP);
    }

    private void OnBunkerHealed(float amount, int remainingHP, float normalizedHP)
    {
        SetLifeNormalized(normalizedHP);
    }

    private void SetLifeNormalized(float normalizedHP)
    {
        if (LifeSlider != null && !Mathf.Approximately(normalizedHP, lastLife))
        {
            lastLife = normalizedHP;
            LifeSlider.value = normalizedHP;
        }
    }

    void Update()
    {
        showTowerSlotAnimation();
        ShowDeck();
    }

    void showTowerSlotAnimation()
    {
        if (TowerSlotAnimation == null) return;

        if (ShowTowerSlot)
        {
            if (!TowerSlotAnimation.activeSelf)
                TowerSlotAnimation.SetActive(true);

            if (mainCamera == null)
                mainCamera = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();

            if (mainCamera != null)
            {
                Vector3 worldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition) - offset;
                worldPos.z = 0f;
                TowerSlotAnimation.transform.position = worldPos;
            }
        }
        else if (TowerSlotAnimation.activeSelf)
        {
            TowerSlotAnimation.SetActive(false);
        }
    }

    public void ShowCardBox(string _name, string _description, Vector3 TC, bool onDrag)
    {
        if (cardInstantiate == null)
        {
            ShowBoxText.Post(gameObject);
            cardInstantiate = Instantiate(CardStats, Canvas2.transform);
            cardInstantiate.transform.position = TC;

            if (cachedCardText == null)
                cachedCardText = ChangeCardText.instance != null ? ChangeCardText.instance : FindAnyObjectByType<ChangeCardText>();

            if (cachedCardText != null)
                cachedCardText.instantiateStats(_name, _description);
        }
    }

    public void ShowLastCardPosition(Vector3 CardPosition)
    {
        bool isNotDragging = GameManager.instance == null || !GameManager.instance.onDrag;

        if (LastPosCard != null)
        {
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
    }

    public void ShowDeck()
    {
        if (this.Deck != null && !this.Deck.activeSelf && CardDrop.instance != null && CardDrop.instance.cardsQueue.Count > 0)
        {
            this.Deck.SetActive(true);
        }
    }
}
