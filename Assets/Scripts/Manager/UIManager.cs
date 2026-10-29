using UnityEngine;
using UnityEngine.UIElements;

public class UIManager : MonoBehaviour
{
    [Header("Sounds")]
    [SerializeField] AK.Wwise.Event ShowBoxText;
    [Space]
    
    public static UIManager instance;
    public Texture2D cursorDefault, cursorTexture;
    [Space]
    [Tooltip("UI Toolkit HUD (UI/HUD.uxml): score, wave, towers and bunker life")]
    [SerializeField] UIDocument hud;
    [Space]
    public bool ShowTowerSlot;
    public GameObject TowerSlotAnimation;
    public Vector3 offset;
    [Space]
    [Header("Life Turret")]
    [Tooltip("Bar over the bunker; found on the TowerPlayer when empty")]
    [SerializeField] WorldLifeBar LifeSlider;
    [Space]
    [Header("Deck")]
    public LeanTweenType TweenDeckOut;
    public float TimeMovement;

    private int lastScore = -1;
    private int lastWave = -1;
    private int lastPrepSeconds = -1;
    private bool wasPrepPhase;
    private float lastLife = -1f;
    private ChangeCardText cardPanel;
    private Label waveText, scoreText, towerCountText;
    private VisualElement lifeFill;

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

        if (hud != null)
        {
            var root = hud.rootVisualElement;
            root.pickingMode = PickingMode.Ignore; // cards and towers below must keep receiving clicks
            scoreText = root.Q<Label>("score");
            waveText = root.Q<Label>("wave");
            towerCountText = root.Q<Label>("towers");
            lifeFill = root.Q("life-fill");
        }

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
        towerCountText.text = max > 0 ? $"TOWERS\n{TowerLimits.Count}/{max}" : $"TOWERS\n{TowerLimits.Count}";
    }

    // Punch the counter so a refused tower drop has visible feedback
    public void FlashTowerLimit()
    {
        if (towerCountText == null) return;
        towerCountText.AddToClassList("hud-stat--flash");
        towerCountText.schedule.Execute(() => towerCountText.RemoveFromClassList("hud-stat--flash")).StartingIn(160);
        CameraShake.MicroShake();
    }

    private void SetScore(int newScore, int delta)
    {
        if (scoreText != null && newScore != lastScore)
        {
            lastScore = newScore;
            scoreText.text = $"SCORE\n{newScore:000000}";
        }
    }

    private void SetWave(int newWave, Bunker.Simulation.EnemyBuffKind buff)
    {
        if (waveText != null && !SimulationBridge.IsPreparationPhase && (newWave != lastWave || wasPrepPhase))
        {
            lastWave = newWave;
            wasPrepPhase = false;
            waveText.text = $"WAVE\n{newWave:000000}";
        }
    }

    private void UpdateWaveDisplay()
    {
        if (waveText == null) return;

        if (SimulationBridge.IsPreparationPhase)
        {
            int secondsLeft = Mathf.CeilToInt(SimEventDispatcher.PhaseTimer);
            if (!wasPrepPhase || secondsLeft != lastPrepSeconds)
            {
                wasPrepPhase = true;
                lastPrepSeconds = secondsLeft;
                waveText.text = $"PREP\n{secondsLeft:00}s";
            }
        }
        else if (wasPrepPhase || SimEventDispatcher.Wave != lastWave)
        {
            wasPrepPhase = false;
            lastWave = SimEventDispatcher.Wave;
            waveText.text = $"WAVE\n{lastWave:000000}";
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
        if (lifeFill != null)
            lifeFill.style.width = Length.Percent(Mathf.Clamp01(normalizedHP) * 100f);

        if (LifeSlider == null)
        {
            var tower = FindFirstObjectByType<TowerPlayer>();
            if (tower != null) LifeSlider = tower.GetComponentInChildren<WorldLifeBar>(true);
        }
        if (LifeSlider != null && !Mathf.Approximately(normalizedHP, lastLife))
        {
            lastLife = normalizedHP;
            LifeSlider.SetValue(normalizedHP, 1f);
        }
    }

    void Update()
    {
        showTowerSlotAnimation();
        ShowDeck();
        UpdateWaveDisplay();
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
        cardPanel.Show(card, _name, _description, card.View);
    }

    public void HideCardBox(Card card = null)
    {
        if (cardPanel != null)
            cardPanel.Hide(card);
    }

    // The panel lives next to GameUIPanel (UI/Game.uxml)
    bool EnsureCardPanel()
    {
        if (cardPanel == null)
            cardPanel = FindAnyObjectByType<ChangeCardText>();
        return cardPanel != null;
    }

    public void ShowDeck()
    {
        if (CardDrop.instance == null) return;

        // Shown while it holds something playable in this phase (towers in preparation, allies during a wave)
        bool playable = CardDrop.instance.HasPlayableCard;
        if (!CardDrop.instance.DeckShown && playable)
        {
            CardDrop.instance.ShowDeck();
        }
        else if (CardDrop.instance.DeckShown && !playable)
        {
            CardDrop.instance.HideDeck();
        }
    }
}
