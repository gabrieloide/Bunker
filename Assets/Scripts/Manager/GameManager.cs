using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public bool onDrag;
    public int ActualScore;
    [HideInInspector]public int CurrentCardAmount;
    public bool StartingHandDealt { get; private set; }
    [Tooltip("Towers for the starting hand and the wave-clear reward are dealt from here")]
    [SerializeField] CardCatalog catalog;
    public CardCatalog Catalog => catalog;
    private CardDrop cardDrop;
    private CardBag towerBag;
    private CardBag flagBag;
    private CardBag roadBag;
    private int wavesCleared;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        TowerLimits.ResetStatics();
        PlacementGrid.Clear();
        Card.ResetPendingResolveCount();

        CurrentCardAmount = 0;
        StartingHandDealt = false;
        cardDrop = CardDrop.instance != null ? CardDrop.instance : FindAnyObjectByType<CardDrop>();
    }

    private void Start()
    {
        if (UIManager.instance != null && UIManager.instance.cursorDefault != null)
        {
            Cursor.SetCursor(UIManager.instance.cursorDefault, Vector2.zero, CursorMode.Auto);
        }

        if (catalog != null)
        {
            DealFlags(catalog.startingFlags);
            DealRoads(catalog.startingRoads);
            DealTowers(catalog.startingTowers);
        }
        if (cardDrop != null && !cardDrop.HasPlayableCard)
            cardDrop.HideDeck();
        StartingHandDealt = true;

        SimEventDispatcher.OnWavePhaseChanged += OnWavePhaseChanged;
        SoundBank.PlayMusic(LevelSetup.Current != null ? LevelSetup.Current.music : null);
    }

    private void OnDestroy()
    {
        SimEventDispatcher.OnWavePhaseChanged -= OnWavePhaseChanged;
        SoundBank.StopMusic();
    }

    // Clearing a wave earns a flag to win ground back and new towers (kills only drop allies and effect cards)
    Bunker.Simulation.WavePhase lastPhase = Bunker.Simulation.WavePhase.WaitingToStart;

    void OnWavePhaseChanged(Bunker.Simulation.WavePhase phase)
    {
        if (phase == Bunker.Simulation.WavePhase.Spawning)
            SoundBank.Play(Sfx.WaveStart);
        // Back to preparation after a wave was running: the wave is beaten
        bool cleared = phase == Bunker.Simulation.WavePhase.WaitingToStart && lastPhase != Bunker.Simulation.WavePhase.WaitingToStart && !SimEventDispatcher.IsGameOver;
        if (cleared)
        {
            SoundBank.Play(Sfx.WaveCleared);
            wavesCleared++;
        }
        lastPhase = phase;
        if (phase != Bunker.Simulation.WavePhase.WaitingToStart || SimEventDispatcher.IsGameOver || catalog == null)
            return;
        DealFlags(catalog.flagsPerWave);
        if (catalog.wavesPerRoad > 0 && wavesCleared > 0 && wavesCleared % catalog.wavesPerRoad == 0)
            DealRoads(catalog.roadsPerInterval);
        DealTowers(catalog.towersPerWave);
    }

    void DealRoads(int count)
    {
        if (count <= 0) return;
        if (cardDrop == null)
            cardDrop = CardDrop.instance != null ? CardDrop.instance : FindAnyObjectByType<CardDrop>();
        if (cardDrop == null) return;

        roadBag ??= new CardBag(catalog, CardCatalog.IsRoad);
        for (int i = 0; i < count; i++)
            cardDrop.Deal(roadBag.Draw());
    }

    void DealFlags(int count)
    {
        if (cardDrop == null)
            cardDrop = CardDrop.instance != null ? CardDrop.instance : FindAnyObjectByType<CardDrop>();
        if (cardDrop == null) return;

        flagBag ??= new CardBag(catalog, CardCatalog.IsFlag);
        for (int i = 0; i < count; i++)
            cardDrop.Deal(flagBag.Draw());
    }

    // Random towers from a weighted bag, all different when the pool allows it:
    // into the hand while it has room, the rest to the draw pile
    void DealTowers(int count)
    {
        if (cardDrop == null)
            cardDrop = CardDrop.instance != null ? CardDrop.instance : FindAnyObjectByType<CardDrop>();
        if (cardDrop == null) return;

        towerBag ??= new CardBag(catalog, CardCatalog.IsTower);
        var dealt = new System.Collections.Generic.List<CardDefinition>(count);
        for (int i = 0; i < count; i++)
        {
            CardDefinition tower = towerBag.Draw(dealt);
            if (tower == null) break;
            dealt.Add(tower);
            cardDrop.Deal(tower);
        }
    }

    private void Update()
    {
        if (UIManager.instance == null || Application.isMobilePlatform || PointerInput.IsTouch) return;

        if (PointerInput.WasPressedThisFrame)
        {
            Cursor.SetCursor(UIManager.instance.cursorTexture, Vector2.zero, CursorMode.Auto);
        }
        else if (PointerInput.WasReleasedThisFrame)
        {
            Cursor.SetCursor(UIManager.instance.cursorDefault, Vector2.zero, CursorMode.Auto);
        }
    }
}
