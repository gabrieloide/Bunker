using System.Collections;
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
    [Tooltip("Name, description, stats and drop weight of this card")]
    public CardDefinition definition;
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
    [Tooltip("Gap between the pointer and the dragged card's bottom edge, in canvas units, so the placement preview stays visible")]
    [SerializeField] float dragPointerGap = 4f;

    float currentTiltAngle = 0f;
    bool isDragging = false;
    Transform originalParent;
    Vector2 baseAnchoredPos;
    Vector2 dragStartScreenPos;

    // Cards that place an object on the map snap its footprint (origin + offset) to a grid cell
    protected virtual bool SnapsToGrid => false;
    bool UseGrid => SnapsToGrid && PlacementGrid.Available;

    protected Vector3 PointerWorld()
    {
        if (Camera.main == null) return transform.position;
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        worldPos.z = 0f;
        return worldPos;
    }

    Vector3Int FootprintCell() => PlacementGrid.WorldToCell(PointerWorld() + offset);

    public virtual Vector3 GetRaycastOrigin()
    {
        if (!UseGrid) return PointerWorld();
        return PlacementGrid.CellCenter(FootprintCell()) - offset;
    }

    // Snapped footprint is shrunk below one cell so it never touches neighbouring cells
    protected RaycastHit2D CastFootprint(LayerMask mask)
    {
        Vector2 size = UseGrid ? PlacementGrid.CellSize * 0.9f : new Vector2(width, height);
        float distance = UseGrid ? 0f : 0.1f;
        return Physics2D.BoxCast(GetRaycastOrigin() + offset, size, 0f, Vector2.down, distance, mask);
    }

    protected virtual RaycastHit2D DetectObjectsBelow() => CastFootprint(objectLayerMask);

    // Captured on drop: placement resolves after the flip animation, when the pointer may have moved
    Vector3 dropOrigin;
    Vector3Int dropCell;

    protected GameObject SpawnPlacement()
    {
        GameObject spawned = SimulationBridge.SpawnFromCard(definition, dropOrigin);
        if (spawned == null) return null;
        if (UseGrid)
            PlacementGrid.Occupy(dropCell, spawned);
        return spawned;
    }

    // Where the drag preview goes; false hides it (e.g. a buff with no valid target under the pointer).
    // Grid cards mark the footprint cell; free cards mark the pointer.
    protected virtual bool TryGetPreviewPosition(out Vector3 position)
    {
        position = UseGrid ? PlacementGrid.CellCenter(FootprintCell()) : PointerWorld();
        return true;
    }

    // How far the preview can reach above the pointer, so the dragged card never covers it
    protected virtual float PreviewClearance => UseGrid ? Mathf.Max(0f, PlacementGrid.CellSize.y + offset.y) : 0f;

    void UpdatePlacementPreview()
    {
        if (!isDragging || UIManager.instance == null || UIManager.instance.TowerSlotAnimation == null)
            return;

        bool visible = TryGetPreviewPosition(out Vector3 target);
        UIManager.instance.ShowTowerSlot = visible;
        if (visible)
            UIManager.instance.TowerSlotAnimation.transform.position = target - UIManager.instance.offset;

        // Towers aim from their pivot (footprint origin), not from the footprint cell
        if (visible && PlacedRange > 0f)
            RangeIndicator.Show(GetRaycastOrigin(), PlacedRange);
        else
            RangeIndicator.Hide();
    }

    // Attack range of the tower this card places; 0 when it places nothing that shoots
    float PlacedRange => SnapsToGrid && definition is TowerCardDefinition tower ? tower.range : 0f;

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

    protected virtual void OnDestroy()
    {
        if (UIManager.instance != null)
            UIManager.instance.HideCardBox(this);
        if (isDragging)
            RangeIndicator.Hide();
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

        if (UIManager.instance != null)
            UIManager.instance.HideCardBox(this);
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
            if (UIManager.instance != null && definition != null && GameManager.instance != null && !GameManager.instance.onDrag)
            {
                UIManager.instance.ToggleCardBox(this, definition.displayName, definition.description);
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
            UIManager.instance.HideCardBox(this);

            if (dc != null && dc.cardSlots != null && index() < dc.cardSlots.Length && dc.cardSlots[index()] != null)
                UIManager.instance.ShowLastCardPosition(dc.cardSlots[index()].position);
        }
        UpdatePlacementPreview();

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
            transform.position = worldPoint + DragLift();
        else
            transform.position = eventData.position;

        // Dynamic tilt based on horizontal mouse movement
        float deltaX = eventData.delta.x;
        float targetAngle = Mathf.Clamp(-deltaX * 1.5f, -22f, 22f);
        currentTiltAngle = Mathf.Lerp(currentTiltAngle, targetAngle, Time.deltaTime * 18f);
        transform.rotation = Quaternion.Euler(0f, 0f, currentTiltAngle);

        UpdatePlacementPreview();
    }

    // Card floats above the pointer (bottom edge + gap) so the tile under the pointer is not covered
    Vector3 DragLift()
    {
        Transform canvasSpace = transform.parent != null ? transform.parent : transform;
        float canvasUnits = rectTransform.rect.height * rectTransform.pivot.y * transform.localScale.y + dragPointerGap;
        Vector3 lift = canvasSpace.TransformVector(Vector3.up * canvasUnits);
        // Constant clearance (not per-cell) so the card doesn't jump while the preview snaps
        lift.y += PreviewClearance;
        return lift;
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
        {
            UIManager.instance.ShowTowerSlot = false;
            // onDrag is already false: hides the slot placeholder shown in OnBeginDrag
            UIManager.instance.ShowLastCardPosition(transform.position);
        }
        RangeIndicator.Hide();

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
        bool hasObstacle = DetectObjectsBelow() || (UseGrid && PlacementGrid.IsOccupied(FootprintCell()));

        if (!hasObstacle && d > dragThreshold && AllowPlacement())
        {
            OnPlacementAccepted();
            CameraShake.MicroShake();
            if (dc != null && index() < dc.availableCardSlots.Length)
                dc.availableCardSlots[index()] = true;
            if (GameManager.instance != null)
                GameManager.instance.CurrentCardAmount--;

            dropOrigin = worldDropPos;
            dropCell = UseGrid ? FootprintCell() : default;

            float flipDuration = 0f;
            if (CardFlipAnim != null)
            {
                GameObject flip = Instantiate(CardFlipAnim, worldDropPos, Quaternion.identity);
                if (flip.TryGetComponent(out OneShotEffect fx))
                    flipDuration = fx.Duration;
                else
                    Destroy(flip, FallbackFlipDuration);
                // The flip holds the cell until the real occupant takes it over, so nothing else drops there meanwhile
                if (UseGrid)
                    PlacementGrid.Occupy(dropCell, flip);
            }

            StartCoroutine(ResolveAfter(flipDuration));
        }
        else
        {
            ReturnToSlot();
        }
    }

    // Last say before a drop is accepted (e.g. the tower limit); false sends the card back to the hand
    protected virtual bool AllowPlacement() => true;
    // The drop is accepted; CardBehaviour runs after the flip animation
    protected virtual void OnPlacementAccepted() { }

    const float FallbackFlipDuration = 0.46f;

    // The card is already spent (slot freed, count decremented); it stays alive, hidden, only to run CardBehaviour once the flip ends
    IEnumerator ResolveAfter(float delay)
    {
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        if (delay > 0f)
            yield return new WaitForSeconds(delay);
        CardBehaviour();
        Destroy(gameObject);
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
