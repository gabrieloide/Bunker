using UnityEngine;

public abstract class Card : MonoBehaviour
{
    [SerializeField] protected LayerMask objectLayerMask;
    public TowersData towerData;
    [SerializeField] Sprite defaultCard, backCard;
    private Vector3 scaleChange() => new Vector3(transform.localScale.x / 2f
                                , transform.localScale.y / 2f,
                                    0f);
    protected Deck dc;
    [SerializeField] protected Vector3 offset = new Vector3(0, -0.5f, 0);
    [SerializeField] protected float width = 1.2f;
    [SerializeField] protected float height = 1;
    [SerializeField] GameObject CardFlipAnim;
    protected virtual RaycastHit2D DetectObjectsBelow() => Physics2D.BoxCast(transform.position + offset, new Vector2(width, height), 0f, Vector2.down, 0.1f, objectLayerMask);
    [HideInInspector] public int index() => GetComponent<CardIndex>().HandIndex;
    protected Vector3 MousePosition;
    protected SpriteRenderer spriteRenderer;
    [SerializeField] float radious = 1.36f;


    private void Start()
    {
        dc = FindObjectOfType<Deck>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        showCard();
    }

    float currentTiltAngle = 0f;

    private void OnMouseOver()
    {
        if (Input.GetMouseButtonDown(1) && !GameManager.instance.onDrag)
            UIManager.instance.ShowCardBox(towerData.Name, towerData.Description, transform.position, GameManager.instance.onDrag);
    }
    private void OnMouseEnter()
    {
        if (!GameManager.instance.onDrag)
        {
            Vector3 slotPos = dc.cardSlots[index()].position;
            LeanTween.cancel(gameObject);
            LeanTween.move(gameObject, slotPos + new Vector3(0f, 0.45f, 0f), 0.12f).setEaseOutQuad();
            LeanTween.scale(gameObject, Vector3.one * 1.08f, 0.12f).setEaseOutQuad();
        }
    }
    private void OnMouseExit()
    {
        if (!GameManager.instance.onDrag)
        {
            Vector3 slotPos = dc.cardSlots[index()].position;
            LeanTween.cancel(gameObject);
            LeanTween.move(gameObject, slotPos, 0.12f).setEaseOutQuad();
            LeanTween.scale(gameObject, Vector3.one, 0.12f).setEaseOutQuad();
            if (UIManager.instance.cardInstantiate != null)
                Destroy(UIManager.instance.cardInstantiate);
        }
    }
    private void OnMouseDrag()
    {
        MousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition) + new Vector3(0f, 0.9f, 10f);
        transform.position = MousePosition;

        // Dynamic tilt based on horizontal mouse movement
        float mouseDeltaX = Input.GetAxis("Mouse X");
        float targetAngle = Mathf.Clamp(-mouseDeltaX * 14f, -22f, 22f);
        currentTiltAngle = Mathf.Lerp(currentTiltAngle, targetAngle, Time.deltaTime * 18f);
        transform.rotation = Quaternion.Euler(0f, 0f, currentTiltAngle);
    }
    private void OnMouseDown()
    {
        var uimanager = UIManager.instance;
        currentTiltAngle = 0f;
        LeanTween.cancel(gameObject);

        spriteRenderer.sprite = backCard;
        LeanTween.alpha(gameObject, 0.87f, 0.3f);
        GameManager.instance.onDrag = true;
        uimanager.ShowTowerSlot = true;

        if (uimanager.cardInstantiate)
        {
            Destroy(uimanager.cardInstantiate);
        }

        uimanager.ShowLastCardPosition(dc.cardSlots[index()].position);
        transform.localScale -= scaleChange();
    }
    private void OnMouseUp()
    {
        spriteRenderer.sprite = defaultCard;
        UIManager.instance.ShowTowerSlot = false;
        GameManager.instance.onDrag = false;
        transform.rotation = Quaternion.identity;

        if (!FindObjectOfType<Trash>().hit2D)
        {
            LeanTween.alpha(gameObject, 1f, 0.3f);
            UIManager.instance.TowerSlotAnimation.SetActive(false);
            spawnCard();
        }
        else
        {
            dc.availableCardSlots[index()] = true;
            Destroy(gameObject);
        }
        UIManager.instance.ShowLastCardPosition(dc.cardSlots[index()].position);
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radious);
    }
    protected virtual void spawnCard()
    {
        float d = Vector2.Distance(transform.position, dc.cardSlots[index()].position);
        if (!DetectObjectsBelow() && d > radious)
        {
            // Usar carta con micro-shake de impacto
            CameraShake.MicroShake();
            dc.availableCardSlots[index()] = true;
            GameManager.instance.CurrentCardAmount--;
            GameObject c = Instantiate(CardFlipAnim, transform.position, Quaternion.identity);
            CardBehaviour();
            Destroy(c, 0.46f);
            Destroy(gameObject);
        }
        else
        {
            // Drop cancelado / inválido: rebote elástico hacia el slot
            Vector3 slotPos = dc.cardSlots[index()].position;
            LeanTween.cancel(gameObject);
            LeanTween.move(gameObject, slotPos, 0.25f).setEaseOutBack();
            LeanTween.scale(gameObject, Vector3.one, 0.2f).setEaseOutBack();
            LeanTween.rotateZ(gameObject, 0f, 0.2f);
        }
    }
    protected abstract void CardBehaviour();
    public void showCard() => LeanTween.moveLocalY(gameObject, UIManager.instance.posInCamera, UIManager.instance.TimeMovement).
                              setEase(UIManager.instance.TweenDeckIn);
}
