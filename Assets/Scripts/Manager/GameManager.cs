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
            DealTowers(catalog.startingTowers);
        if (cardDrop != null && !cardDrop.HasPlayableCard)
            cardDrop.HideDeck();
        StartingHandDealt = true;

        SimEventDispatcher.OnWavePhaseChanged += OnWavePhaseChanged;
    }

    private void OnDestroy()
    {
        SimEventDispatcher.OnWavePhaseChanged -= OnWavePhaseChanged;
    }

    // Clearing a wave earns a flag to win ground back and new towers (kills only drop allies and effect cards)
    void OnWavePhaseChanged(Bunker.Simulation.WavePhase phase)
    {
        if (phase != Bunker.Simulation.WavePhase.WaitingToStart || SimEventDispatcher.IsGameOver || catalog == null)
            return;
        DealFlags(catalog.flagsPerWave);
        DealTowers(catalog.towersPerWave);
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
        if (UIManager.instance == null) return;

        if (Input.GetMouseButtonDown(0))
        {
            Cursor.SetCursor(UIManager.instance.cursorTexture, Vector2.zero, CursorMode.Auto);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            Cursor.SetCursor(UIManager.instance.cursorDefault, Vector2.zero, CursorMode.Auto);
        }
    }
}
