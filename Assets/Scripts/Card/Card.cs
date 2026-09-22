using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform), typeof(Image), typeof(CanvasGroup))]
public abstract class Card : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] protected LayerMask objectLayerMask;
    public TowersData towerData;
    [SerializeField] Sprite defaultCard, backCard;

    protected Deck dc;
    [SerializeField] protected Vector3 offset = new Vector3(0, -0.5f, 0);
    [SerializeField] protected float width = 1.2f;
    [SerializeField] protected float height = 1;
    [SerializeField] GameObject CardFlipAnim;

    [HideInInspector] public int index() => GetComponent<CardIndex>() != null ? GetComponent<CardIndex>().HandIndex : 0;

    protected Image uiImage;
    protected RectTransform rectTransform;
    protected CanvasGroup canvasGroup;

    [SerializeField] float uiHoverHeight = 16f;
    [SerializeField] float dragThreshold = 20f;

    float currentTiltAngle = 0f;
    bool isDragging = false;
    Transform originalParent;
    Vector2 baseAnchoredPos;
    Vector2 dragStartScreenPos;

    public virtual Vector3 GetRaycastOrigin()
    {
        if (Camera.main == null) return transform.position;
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        worldPos.z = 0f;
        return worldPos;
    }

    protected virtual RaycastHit2D DetectObjectsBelow()
    {
        return Physics2D.BoxCast(GetRaycastOrigin() + offset, new Vector2(width, height), 0f, Vector2.down, 0.1f, objectLayerMask);
    }

    protected virtual void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        uiImage = GetComponent<Image>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (GetComponentInParent<Canvas>() == null)
            Debug.LogError($"[Card] {name} must be instantiated under a Canvas.", this);

        baseAnchoredPos = rectTransform.anchoredPosition;
    }

    protected virtual void Start()
    {
        dc = Deck.instance != null ? Deck.instance : FindAnyObjectByType<Deck>();
        showCard();
    }

    // ------------------------------------------------ UI EventSystem Handlers

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isDragging || (GameManager.instance != null && GameManager.instance.onDrag))
            return;

        ElevateCard();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isDragging || (GameManager.instance != null && GameManager.instance.onDrag))
            return;

        LowerCard();
    }

    void ElevateCard()
    {
        LeanTween.cancel(gameObject);
        TweenAnchoredY(baseAnchoredPos.y + uiHoverHeight, 0.12f).setEaseOutQuad();
        LeanTween.scale(gameObject, Vector3.one * 1.08f, 0.12f).setEaseOutQuad();
        transform.SetAsLastSibling();
    }

    void LowerCard()
    {
        LeanTween.cancel(gameObject);
        TweenAnchoredY(baseAnchoredPos.y, 0.12f).setEaseOutQuad();
        LeanTween.scale(gameObject, Vector3.one, 0.12f).setEaseOutQuad();

        if (originalParent != null)
            transform.SetSiblingIndex(index());

        if (UIManager.instance != null && UIManager.instance.cardInstantiate != null)
            Destroy(UIManager.instance.cardInstantiate);
    }

    LTDescr TweenAnchoredY(float targetY, float time)
    {
        return LeanTween.value(gameObject, y => {
            if (rectTransform != null)
                rectTransform.anchoredPosition = new Vector2(baseAnchoredPos.x, y);
        }, rectTransform.anchoredPosition.y, targetY, time);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (UIManager.instance != null && towerData != null && GameManager.instance != null && !GameManager.instance.onDrag)
            {
                UIManager.instance.ShowCardBox(towerData.Name, towerData.Description, transform.position, GameManager.instance.onDrag);
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        isDragging = true;
        dragStartScreenPos = eventData.position;
        currentTiltAngle = 0f;
        LeanTween.cancel(gameObject);

        if (backCard != null)
            uiImage.sprite = backCard;

        canvasGroup.alpha = 0.85f;

        if (GameManager.instance != null)
            GameManager.instance.onDrag = true;

        if (UIManager.instance != null)
        {
            UIManager.instance.ShowTowerSlot = true;
            if (UIManager.instance.cardInstantiate != null)
                Destroy(UIManager.instance.cardInstantiate);

            if (dc != null && dc.cardSlots != null && index() < dc.cardSlots.Length && dc.cardSlots[index()] != null)
                UIManager.instance.ShowLastCardPosition(dc.cardSlots[index()].position);
        }

        originalParent = transform.parent;
        Canvas rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas != null)
            transform.SetParent(rootCanvas.transform, true);
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out Vector3 worldPoint))
            transform.position = worldPoint;
        else
            transform.position = eventData.position;

        // Dynamic tilt based on horizontal mouse movement
        float deltaX = eventData.delta.x;
        float targetAngle = Mathf.Clamp(-deltaX * 1.5f, -22f, 22f);
        currentTiltAngle = Mathf.Lerp(currentTiltAngle, targetAngle, Time.deltaTime * 18f);
        transform.rotation = Quaternion.Euler(0f, 0f, currentTiltAngle);

        // Update world placement preview indicator
        if (UIManager.instance != null && UIManager.instance.ShowTowerSlot && Camera.main != null)
        {
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(eventData.position);
            worldPos.z = 0f;
            if (UIManager.instance.TowerSlotAnimation != null)
                UIManager.instance.TowerSlotAnimation.transform.position = worldPos - UIManager.instance.offset;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        isDragging = false;

        transform.rotation = Quaternion.identity;

        if (defaultCard != null)
            uiImage.sprite = defaultCard;

        canvasGroup.alpha = 1f;

        if (GameManager.instance != null)
            GameManager.instance.onDrag = false;

        if (UIManager.instance != null)
            UIManager.instance.ShowTowerSlot = false;

        var trash = Trash.Instance != null ? Trash.Instance : FindAnyObjectByType<Trash>();
        bool isTrash = (trash != null && (trash.hit2D || trash.IsPointerOver()));

        if (isTrash)
        {
            if (dc != null && index() < dc.availableCardSlots.Length)
                dc.availableCardSlots[index()] = true;
            if (GameManager.instance != null)
                GameManager.instance.CurrentCardAmount--;
            Destroy(gameObject);
            return;
        }

        spawnCard();
    }

    // ------------------------------------------------ Card Placement & Slot Logic

    protected virtual void spawnCard()
    {
        Vector3 worldDropPos = GetRaycastOrigin();
        // Measured in screen space: anchoredPosition is relative to the root canvas while dragging, not the slot
        float d = Vector2.Distance(Input.mousePosition, dragStartScreenPos);
        bool hasObstacle = DetectObjectsBelow();

        if (!hasObstacle && d > dragThreshold)
        {
            CameraShake.MicroShake();
            if (dc != null && index() < dc.availableCardSlots.Length)
                dc.availableCardSlots[index()] = true;
            if (GameManager.instance != null)
                GameManager.instance.CurrentCardAmount--;

            if (CardFlipAnim != null)
            {
                GameObject c = Instantiate(CardFlipAnim, worldDropPos, Quaternion.identity);
                Destroy(c, 0.46f);
            }

            CardBehaviour();
            Destroy(gameObject);
        }
        else
        {
            ReturnToSlot();
        }
    }

    protected void ReturnToSlot()
    {
        if (originalParent != null)
        {
            transform.SetParent(originalParent, true);
            transform.SetSiblingIndex(index());
        }

        LeanTween.cancel(gameObject);
        LeanTween.value(gameObject, pos => {
            if (rectTransform != null)
                rectTransform.anchoredPosition = pos;
        }, rectTransform.anchoredPosition, baseAnchoredPos, 0.25f).setEaseOutBack();
        LeanTween.scale(gameObject, Vector3.one, 0.2f).setEaseOutBack();
        LeanTween.rotateZ(gameObject, 0f, 0.2f);
    }

    protected abstract void CardBehaviour();

    public void showCard()
    {
        rectTransform.anchoredPosition = new Vector2(baseAnchoredPos.x, baseAnchoredPos.y - 35f);
        TweenAnchoredY(baseAnchoredPos.y, 0.25f).setEaseOutQuad();
    }
}
