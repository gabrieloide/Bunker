using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public abstract class Card : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler,
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

    protected SpriteRenderer spriteRenderer;
    protected Image uiImage;
    protected RectTransform rectTransform;
    protected CanvasGroup canvasGroup;
    public bool isUI { get; protected set; } = false;

    [SerializeField] float radious = 1.36f;
    [SerializeField] float uiHoverHeight = 16f;

    float currentTiltAngle = 0f;
    bool isDragging = false;
    bool isHovered = false;
    Transform originalParent;
    Vector2 baseAnchoredPos;

    public virtual Vector3 GetRaycastOrigin()
    {
        if (isUI && Camera.main != null)
        {
            Vector3 screenPos = Input.mousePosition;
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
            worldPos.z = 0f;
            return worldPos;
        }
        return transform.position;
    }

    protected virtual RaycastHit2D DetectObjectsBelow()
    {
        return Physics2D.BoxCast(GetRaycastOrigin() + offset, new Vector2(width, height), 0f, Vector2.down, 0.1f, objectLayerMask);
    }

    protected virtual void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null && GetComponentInParent<Canvas>() != null)
        {
            isUI = true;

            // Remove 2D collider to prevent interference with UI GraphicRaycaster
            var col = GetComponent<Collider2D>();
            if (col != null) Destroy(col);

            // Convert SpriteRenderer to UI Image if needed
            uiImage = GetComponent<Image>();
            if (uiImage == null)
            {
                var sr = GetComponent<SpriteRenderer>();
                Sprite initialSprite = defaultCard;
                if (sr != null)
                {
                    if (initialSprite == null) initialSprite = sr.sprite;
                    Destroy(sr);
                }
                gameObject.AddComponent<CanvasRenderer>();
                uiImage = gameObject.AddComponent<Image>();
                uiImage.sprite = initialSprite != null ? initialSprite : defaultCard;
                uiImage.preserveAspect = true;
                uiImage.raycastTarget = true;
            }

            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            baseAnchoredPos = rectTransform.anchoredPosition;
        }
        else
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    protected virtual void Start()
    {
        dc = FindObjectOfType<Deck>();
        showCard();
    }

    // ------------------------------------------------ UI EventSystem Handlers (Rock solid, 0 jitter)

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isDragging || (GameManager.instance != null && GameManager.instance.onDrag))
            return;

        isHovered = true;
        ElevateCard();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isDragging || (GameManager.instance != null && GameManager.instance.onDrag))
            return;

        isHovered = false;
        LowerCard();
    }

    void ElevateCard()
    {
        LeanTween.cancel(gameObject);
        if (isUI && rectTransform != null)
        {
            LeanTween.value(gameObject, y => {
                if (rectTransform != null)
                    rectTransform.anchoredPosition = new Vector2(baseAnchoredPos.x, y);
            }, rectTransform.anchoredPosition.y, baseAnchoredPos.y + uiHoverHeight, 0.12f).setEaseOutQuad();
        }
        else if (dc != null && dc.cardSlots != null && index() < dc.cardSlots.Length && dc.cardSlots[index()] != null)
        {
            Vector3 slotPos = dc.cardSlots[index()].position;
            LeanTween.move(gameObject, slotPos + new Vector3(0f, 0.45f, 0f), 0.12f).setEaseOutQuad();
        }
        LeanTween.scale(gameObject, Vector3.one * 1.08f, 0.12f).setEaseOutQuad();
        transform.SetAsLastSibling();
    }

    void LowerCard()
    {
        LeanTween.cancel(gameObject);
        if (isUI && rectTransform != null)
        {
            LeanTween.value(gameObject, y => {
                if (rectTransform != null)
                    rectTransform.anchoredPosition = new Vector2(baseAnchoredPos.x, y);
            }, rectTransform.anchoredPosition.y, baseAnchoredPos.y, 0.12f).setEaseOutQuad();
        }
        else if (dc != null && dc.cardSlots != null && index() < dc.cardSlots.Length && dc.cardSlots[index()] != null)
        {
            Vector3 slotPos = dc.cardSlots[index()].position;
            LeanTween.move(gameObject, slotPos, 0.12f).setEaseOutQuad();
        }
        LeanTween.scale(gameObject, Vector3.one, 0.12f).setEaseOutQuad();

        if (originalParent != null)
            transform.SetSiblingIndex(index());

        if (UIManager.instance != null && UIManager.instance.cardInstantiate != null)
            Destroy(UIManager.instance.cardInstantiate);
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

    public void OnPointerUp(PointerEventData eventData)
    {
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        isDragging = true;
        currentTiltAngle = 0f;
        LeanTween.cancel(gameObject);

        if (isUI && uiImage != null && backCard != null)
            uiImage.sprite = backCard;
        else if (spriteRenderer != null && backCard != null)
            spriteRenderer.sprite = backCard;

        if (canvasGroup != null)
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

        if (isUI && rectTransform != null)
        {
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out Vector3 worldPoint))
            {
                transform.position = worldPoint;
            }
            else
            {
                transform.position = eventData.position;
            }
        }
        else if (Camera.main != null)
        {
            transform.position = Camera.main.ScreenToWorldPoint(Input.mousePosition) + new Vector3(0f, 0.9f, 10f);
        }

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

        if (isUI && uiImage != null && defaultCard != null)
            uiImage.sprite = defaultCard;
        else if (spriteRenderer != null && defaultCard != null)
            spriteRenderer.sprite = defaultCard;

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        if (GameManager.instance != null)
            GameManager.instance.onDrag = false;

        if (UIManager.instance != null)
            UIManager.instance.ShowTowerSlot = false;

        var trash = FindObjectOfType<Trash>();
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

    // ------------------------------------------------ Legacy Mouse Fallback for non-UI mode
    private void OnMouseEnter() { if (!isUI) OnPointerEnter(null); }
    private void OnMouseExit() { if (!isUI) OnPointerExit(null); }
    private void OnMouseDown() { if (!isUI) { var pe = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }; OnBeginDrag(pe); } }
    private void OnMouseDrag() { if (!isUI) { var pe = new PointerEventData(EventSystem.current) { delta = new Vector2(Input.GetAxis("Mouse X") * 10f, 0f) }; OnDrag(pe); } }
    private void OnMouseUp() { if (!isUI) { var pe = new PointerEventData(EventSystem.current); OnEndDrag(pe); } }
    private void OnMouseOver() { if (!isUI && Input.GetMouseButtonDown(1) && !GameManager.instance.onDrag) UIManager.instance.ShowCardBox(towerData.Name, towerData.Description, transform.position, GameManager.instance.onDrag); }

    // ------------------------------------------------ Card Placement & Slot Logic

    protected virtual void spawnCard()
    {
        Vector3 worldDropPos = GetRaycastOrigin();
        float d = 0f;
        if (dc != null && dc.cardSlots != null && index() < dc.cardSlots.Length && dc.cardSlots[index()] != null)
        {
            if (isUI && rectTransform != null)
                d = Vector2.Distance(rectTransform.anchoredPosition, baseAnchoredPos);
            else
                d = Vector2.Distance(transform.position, dc.cardSlots[index()].position);
        }

        float dragThreshold = isUI ? 20f : radious;
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
            transform.SetParent(originalParent, false);
            transform.SetSiblingIndex(index());
        }

        LeanTween.cancel(gameObject);
        if (isUI && rectTransform != null)
        {
            LeanTween.value(gameObject, pos => {
                if (rectTransform != null)
                    rectTransform.anchoredPosition = pos;
            }, rectTransform.anchoredPosition, baseAnchoredPos, 0.25f).setEaseOutBack();
        }
        else if (dc != null && dc.cardSlots != null && index() < dc.cardSlots.Length && dc.cardSlots[index()] != null)
        {
            Vector3 slotPos = dc.cardSlots[index()].position;
            LeanTween.move(gameObject, slotPos, 0.25f).setEaseOutBack();
        }
        LeanTween.scale(gameObject, Vector3.one, 0.2f).setEaseOutBack();
        LeanTween.rotateZ(gameObject, 0f, 0.2f);
    }

    protected abstract void CardBehaviour();

    public void showCard()
    {
        if (isUI && rectTransform != null)
        {
            rectTransform.anchoredPosition = new Vector2(baseAnchoredPos.x, baseAnchoredPos.y - 35f);
            LeanTween.value(gameObject, y => {
                if (rectTransform != null)
                    rectTransform.anchoredPosition = new Vector2(baseAnchoredPos.x, y);
            }, baseAnchoredPos.y - 35f, baseAnchoredPos.y, 0.25f).setEaseOutQuad();
        }
        else
        {
            LeanTween.moveLocalY(gameObject, UIManager.instance != null ? UIManager.instance.posInCamera : -5.5f, UIManager.instance != null ? UIManager.instance.TimeMovement : 0.3f)
                .setEase(UIManager.instance != null ? UIManager.instance.TweenDeckIn : LeanTweenType.easeOutQuad);
        }
    }
}
