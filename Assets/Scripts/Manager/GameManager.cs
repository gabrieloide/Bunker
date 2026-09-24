using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public bool onDrag;
    public int ActualScore;
    [HideInInspector]public int CurrentCardAmount;
    [Tooltip("Starting hand is dealt from here")]
    [SerializeField] CardCatalog catalog;
    public CardCatalog Catalog => catalog;
    private CardDrop cardDrop;

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

        cardDrop = CardDrop.instance != null ? CardDrop.instance : FindAnyObjectByType<CardDrop>();
    }

    private void Start()
    {
        if (UIManager.instance != null && UIManager.instance.cursorDefault != null)
        {
            Cursor.SetCursor(UIManager.instance.cursorDefault, Vector2.zero, CursorMode.Auto);
        }

        var level = LevelSetup.Current;
        var startingHand = level != null && level.startingHand.Count > 0 ? level.startingHand
            : catalog != null ? catalog.startingHand : null;
        if (startingHand != null)
            foreach (var card in startingHand)
                AddCardToHand(card);
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

    public void AddCardToHand(CardDefinition card)
    {
        if (cardDrop == null)
        {
            cardDrop = CardDrop.instance != null ? CardDrop.instance : FindAnyObjectByType<CardDrop>();
        }

        if (cardDrop != null)
        {
            cardDrop.cardsQueue.Enqueue(card);
        }
    }
}
