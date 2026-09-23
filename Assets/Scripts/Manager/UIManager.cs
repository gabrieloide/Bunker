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
    [Tooltip("Shows towers on the map / the limit; flashes when a drop is refused")]
    public TMP_Text towerCountText;
    [Space]
    public bool ShowTowerSlot;
    public GameObject TowerSlotAnimation;
    public Vector3 offset;
    [Space]
    public GameObject CardStats;
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
    public LeanTweenType TweenDeckOut;
    public GameObject Deck;
    public float TimeMovement;

    private int lastScore = -1;
    private int lastWave = -1;
    private float lastLife = -1f;
    private ChangeCardText cardPanel;

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
        TowerLimits.Changed += UpdateTowerCount;
        UpdateTowerCount();
    }

    private void OnDisable()
    {
        SimEventDispatcher.OnScoreChanged -= SetScore;
        SimEventDispatcher.OnWaveChanged -= SetWave;
        SimEventDispatcher.OnPlayerHit -= OnPlayerHit;
        SimEventDispatcher.OnBunkerHealed -= OnBunkerHealed;
        TowerLimits.Changed -= UpdateTowerCount;
    }

    // OnEnable can run before GameManager.Awake, so the limit is read again once everything exists
    void Start() => UpdateTowerCount();

    void UpdateTowerCount()
    {
        if (towerCountText == null) return;
        int max = GameManager.instance != null && GameManager.instance.Catalog != null ? GameManager.instance.Catalog.maxTowers : 0;
        if (max > 0) towerCountText.SetText("Towers: {0}/{1}", TowerLimits.Count, max);
        else towerCountText.SetText("Towers: {0}", TowerLimits.Count);
    }

    // Punch the counter so a refused tower drop has visible feedback
    public void FlashTowerLimit()
    {
        if (towerCountText == null) return;
        var go = towerCountText.gameObject;
        LeanTween.cancel(go);
        go.transform.localScale = Vector3.one;
        LeanTween.scale(go, Vector3.one * 1.35f, 0.08f).setEaseOutQuad().setLoopPingPong(2);
        CameraShake.MicroShake();
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

        // Position is driven by the dragged Card (snapped to the placement grid when applicable)
        if (TowerSlotAnimation.activeSelf != ShowTowerSlot)
            TowerSlotAnimation.SetActive(ShowTowerSlot);
    }

    // Right click on a hand card: toggles its detail panel (single reusable instance)
    public void ToggleCardBox(Card card, string _name, string _description)
    {
        if (!EnsureCardPanel()) return;

        if (cardPanel.Owner == card)
        {
            cardPanel.Hide(card);
            return;
        }

        ShowBoxText.Post(gameObject);
        cardPanel.Show(card, _name, _description, card.transform as RectTransform);
    }

    public void HideCardBox(Card card = null)
    {
        if (cardPanel != null)
            cardPanel.Hide(card);
    }

    bool EnsureCardPanel()
    {
        if (cardPanel != null) return true;
        if (CardStats == null || Canvas2 == null) return false;

        cardPanel = Instantiate(CardStats, Canvas2.transform).GetComponent<ChangeCardText>();
        return cardPanel != null;
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
