using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

// A card in the hand. Its picture is a UI Toolkit element in the hand of the game UI panel (see HandLayout);
// LeanTween animates this transform's scale and rotation and the hand position, LateUpdate copies them onto the element.
public abstract class Card : MonoBehaviour
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

    protected HandLayout Hand => dc != null ? dc.Hand : null;

    protected VisualElement view;
    public VisualElement View => view;
    // Position in the hand, panel units, y up
    Vector2 handPosition;
    float alpha = 1f;

    [SerializeField] float uiHoverHeight = 16f;
    [SerializeField] float dragThreshold = 20f;
    [Tooltip("Gap between the pointer and the dragged card's bottom edge, in panel units, so the placement preview stays visible")]
    [SerializeField] float dragPointerGap = 4f;
    [Tooltip("Scale applied to the card while being dragged above the hand")]
    [SerializeField] float dragScale = 0.5f;

    float currentTiltAngle = 0f;
    bool isDragging = false;
    bool hovered = false;
    bool isResolving = false;
    int moveTweenId = -1;
    Vector2 baseAnchoredPos;
    Vector2 dragStartScreenPos;
    bool pressed;
    Vector2 pressPosition;
    float lastPointerX;

    // Pointer travel (panel units) before a press becomes a drag, like the EventSystem's drag threshold
    const float BeginDragDistance = 2f;

    public static int PendingResolveCount { get; private set; }

    // Towers are built between waves; allies and air support only act while a wave is on; everything else
    // plays any time. The hand hides cards that can't be played now and the draw pile holds them back.
    public static bool PlayableIn(CardDefinition card, bool preparation) => card switch
    {
        TowerCardDefinition => preparation,
        AllyCardDefinition => !preparation,
        HelicopterCardDefinition => !preparation,
        _ => true
    };

    public bool PlayableNow => PlayableIn(definition, SimulationBridge.IsPreparationPhase);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStaticResolveCount() => PendingResolveCount = 0;

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

    protected Vector3Int FootprintCell() => PlacementGrid.WorldToCell(PointerWorld() + offset);

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

    // Checks whether the tile is blocked (no ground, on an obstacle tile, or occupied) via PlacementGrid without colliders
    protected virtual bool HasObstacleBelow()
    {
        if (UseGrid && PlacementGrid.Available)
            return !PlacementGrid.CanPlaceAt(FootprintCell());
        return (bool)DetectObjectsBelow();
    }

    // Every card has to be played inside a flag's radius (see FlagTerritory)
    protected virtual Vector3 TerritoryPoint => UseGrid ? PlacementGrid.CellCenter(FootprintCell()) : PointerWorld();
    protected virtual bool InTerritory => FlagTerritory.Contains(TerritoryPoint);

    // Captured on drop: placement resolves after the flip animation, when the pointer may have moved
    Vector3 dropOrigin;
    Vector3Int dropCell;

    protected GameObject SpawnPlacement()
    {
        GameObject spawned = SimulationBridge.SpawnFromCard(definition, dropOrigin);
        if (spawned == null) return null;
        if (UseGrid && OccupiesCell)
            PlacementGrid.Occupy(dropCell, spawned);
        return spawned;
    }

    // Whether the placed object keeps its cell (false for things that move away, like the helicopter)
    protected virtual bool OccupiesCell => true;

    // Where the drag preview goes; false hides it (e.g. a buff with no valid target under the pointer).
    // Grid cards mark the footprint cell; free cards mark the pointer.
    protected virtual bool TryGetPreviewPosition(out Vector3 position)
    {
        position = UseGrid ? PlacementGrid.CellCenter(FootprintCell()) : PointerWorld();
        return true;
    }

    // How far the preview can reach above the pointer, so the dragged card never covers it
    protected virtual float PreviewClearance => UseGrid ? Mathf.Max(0f, PlacementGrid.CellSize.y + offset.y) : 0f;

    public virtual bool CanPlaceNow => PlayableNow && InTerritory && !HasObstacleBelow();

    void UpdatePlacementPreview()
    {
        if (!isDragging || UIManager.instance == null || UIManager.instance.TowerSlotAnimation == null)
            return;

        var trash = Trash.Instance != null ? Trash.Instance : FindAnyObjectByType<Trash>();
        bool isTrash = (trash != null && (trash.hit2D || trash.IsPointerOver()));

        if (isTrash)
        {
            UIManager.instance.ShowTowerSlot = false;
            RangeIndicator.Hide();
            HidePlacementFeedback();
            return;
        }

        bool hasPosition = TryGetPreviewPosition(out Vector3 target);
        bool valid = hasPosition && CanPlaceNow;
        bool visible = hasPosition && valid;

        ShowPlacementFeedback(valid);

        UIManager.instance.ShowTowerSlot = visible;
        if (visible)
            UIManager.instance.TowerSlotAnimation.transform.position = target - UIManager.instance.offset;

        // Towers aim from their pivot (footprint origin), not from the footprint cell
        if (visible && PlacedRange > 0f)
            RangeIndicator.Show(GetRaycastOrigin(), PlacedRange);
        else
            RangeIndicator.Hide();
    }

    // Extra feedback while dragging, e.g. the flag's check/cross; valid = dropping here would be accepted
    protected virtual void ShowPlacementFeedback(bool valid)
    {
        if (!valid)
        {
            Vector3 crossPos = TryGetPreviewPosition(out Vector3 target) ? target : PointerWorld();
            PlacementCrossIndicator.Show(crossPos);
        }
        else
        {
            PlacementCrossIndicator.Hide();
        }
    }

    protected virtual void HidePlacementFeedback()
    {
        PlacementCrossIndicator.Hide();
    }

    // Radius drawn around the placement preview: a tower's attack range; 0 draws nothing
    protected virtual float PlacedRange => SnapsToGrid && definition is TowerCardDefinition tower ? tower.range : 0f;

    protected virtual void Awake()
    {
        view = new VisualElement { name = name };
        view.AddToClassList("card");
        view.style.backgroundImage = new StyleBackground(defaultCard);
        view.RegisterCallback<PointerEnterEvent>(_ => OnPointerEnter());
        view.RegisterCallback<PointerLeaveEvent>(_ => OnPointerExit());
        view.RegisterCallback<PointerDownEvent>(OnPointerDown);
        view.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        view.RegisterCallback<PointerUpEvent>(OnPointerUp);
        view.RegisterCallback<PointerCaptureOutEvent>(_ => { pressed = false; if (isDragging) OnEndDrag(); });

        if (GameUIPanel.Instance != null)
            GameUIPanel.Instance.Hand.Add(view);
        else
            Debug.LogError($"[Card] {name} needs a GameUIPanel in the scene.", this);
    }

    void OnEnable() => view.style.display = DisplayStyle.Flex;
    void OnDisable() => view.style.display = DisplayStyle.None;

    void LateUpdate()
    {
        view.style.translate = new Translate(handPosition.x, -handPosition.y);
        view.style.scale = new Scale(transform.localScale);
        view.style.rotate = new Rotate(-transform.eulerAngles.z);
        view.style.opacity = alpha;
    }

    protected virtual void Start()
    {
        dc = Deck.instance != null ? Deck.instance : FindAnyObjectByType<Deck>();
        showCard();
    }

    protected virtual void OnDestroy()
    {
        EndPendingResolve();
        if (Hand != null)
            Hand.Remove(this);
        if (UIManager.instance != null)
            UIManager.instance.HideCardBox(this);
        if (isDragging)
        {
            RangeIndicator.Hide();
            if (Hand != null) Hand.HideGhost();
            HidePlacementFeedback();
        }
        view?.RemoveFromHierarchy();
    }

    void BeginPendingResolve()
    {
        if (isResolving) return;
        isResolving = true;
        PendingResolveCount++;
    }

    void EndPendingResolve()
    {
        if (!isResolving) return;
        isResolving = false;
        PendingResolveCount = Mathf.Max(0, PendingResolveCount - 1);
    }



    public void CancelDrag()
    {
        if (!isDragging) return;
        isDragging = false;

        transform.rotation = Quaternion.identity;

        view.style.backgroundImage = new StyleBackground(defaultCard);
        alpha = 1f;

        if (GameManager.instance != null)
            GameManager.instance.onDrag = false;

        if (UIManager.instance != null)
            UIManager.instance.ShowTowerSlot = false;
        if (Hand != null)
            Hand.HideGhost();
        RangeIndicator.Hide();
        HidePlacementFeedback();

        ReturnToSlot();
    }

    // ------------------------------------------------ Pointer Handlers

    void OnPointerEnter()
    {
        if (isDragging || (GameManager.instance != null && GameManager.instance.onDrag))
            return;

        ElevateCard();
    }

    void OnPointerExit()
    {
        if (isDragging || (GameManager.instance != null && GameManager.instance.onDrag))
            return;

        LowerCard();
    }

    void ElevateCard()
    {
        hovered = true;
        LeanTween.cancel(gameObject);
        TweenAnchored(HandPosition, 0.12f).setEaseOutQuad();
        LeanTween.scale(gameObject, Vector3.one * 1.08f, 0.12f).setEaseOutQuad();
        view.BringToFront();
    }

    void LowerCard()
    {
        hovered = false;
        LeanTween.cancel(gameObject);
        TweenAnchored(HandPosition, 0.12f).setEaseOutQuad();
        LeanTween.scale(gameObject, Vector3.one, 0.12f).setEaseOutQuad();
        RestoreHandOrder();

        if (UIManager.instance != null)
            UIManager.instance.HideCardBox(this);
    }

    // Home spot in the hand, raised while hovered
    Vector2 HandPosition => baseAnchoredPos + (hovered ? Vector2.up * uiHoverHeight : Vector2.zero);

    LTDescr TweenAnchored(Vector2 target, float time)
    {
        if (moveTweenId >= 0)
            LeanTween.cancel(gameObject, moveTweenId);
        var tween = LeanTween.value(gameObject, pos => handPosition = pos, handPosition, target, time);
        moveTweenId = tween.uniqueId;
        return tween;
    }

    // Called by HandLayout whenever the hand changes; a dragged card picks it up when it returns
    public void SetHandHome(Vector2 home, float time)
    {
        baseAnchoredPos = home;
        if (isDragging) return;
        if (time <= 0f)
            handPosition = HandPosition;
        else
            TweenAnchored(HandPosition, time).setEaseOutQuad();
    }

    void RestoreHandOrder()
    {
        int index = Hand != null ? Hand.IndexOf(this) : -1;
        if (index >= 0)
        {
            transform.SetSiblingIndex(index);
            SetDrawOrder(index);
        }
    }

    // Cards are drawn in hand order, after the slot placeholder (the hand element's first child)
    public void SetDrawOrder(int index)
    {
        var hand = view.parent;
        if (hand == null) return;
        view.RemoveFromHierarchy();
        hand.Insert(Mathf.Min(index + 1, hand.childCount), view);
    }

    // The card is spent: frees its place in the hand so the rest close ranks
    protected void LeaveHand()
    {
        if (GameManager.instance != null)
            GameManager.instance.CurrentCardAmount--;
        if (Hand != null)
            Hand.Remove(this);
    }

    void OnPointerDown(PointerDownEvent evt)
    {
        if (evt.button == 1)
        {
            if (UIManager.instance != null && definition != null && GameManager.instance != null && !GameManager.instance.onDrag)
            {
                UIManager.instance.ToggleCardBox(this, definition.displayName, definition.description);
            }
        }
        else if (evt.button == 0)
        {
            pressed = true;
            pressPosition = evt.position;
            view.CapturePointer(evt.pointerId);
        }
    }

    void OnPointerMove(PointerMoveEvent evt)
    {
        if (!pressed) return;
        if (!isDragging && Vector2.Distance(evt.position, pressPosition) > BeginDragDistance)
            OnBeginDrag();
        if (isDragging)
            OnDrag(evt.position);
    }

    void OnPointerUp(PointerUpEvent evt)
    {
        if (evt.button != 0) return;
        pressed = false;
        if (isDragging)
            OnEndDrag();
        if (view.HasPointerCapture(evt.pointerId))
            view.ReleasePointer(evt.pointerId);
    }

    void OnBeginDrag()
    {
        isDragging = true;
        hovered = false;
        dragStartScreenPos = Input.mousePosition;
        lastPointerX = Input.mousePosition.x;
        currentTiltAngle = 0f;
        LeanTween.cancel(gameObject);
        LeanTween.scale(gameObject, Vector3.one * dragScale, 0.08f).setEaseOutQuad();

        if (backCard != null)
            view.style.backgroundImage = new StyleBackground(backCard);

        alpha = 0.85f;

        if (GameManager.instance != null)
            GameManager.instance.onDrag = true;

        if (UIManager.instance != null)
        {
            UIManager.instance.ShowTowerSlot = true;
            UIManager.instance.HideCardBox(this);
        }
        if (Hand != null)
            Hand.ShowGhost(baseAnchoredPos);
        UpdatePlacementPreview();

        view.BringToFront();
    }

    void OnDrag(Vector2 pointer)
    {
        if (!isDragging) return;

        Vector2 local = view.parent.WorldToLocal(pointer);
        handPosition = new Vector2(local.x, -local.y) + DragLift();

        // Dynamic tilt based on horizontal mouse movement (screen pixels)
        float deltaX = Input.mousePosition.x - lastPointerX;
        lastPointerX = Input.mousePosition.x;
        float targetAngle = Mathf.Clamp(-deltaX * 1.5f, -22f, 22f);
        currentTiltAngle = Mathf.Lerp(currentTiltAngle, targetAngle, Time.deltaTime * 18f);
        transform.rotation = Quaternion.Euler(0f, 0f, currentTiltAngle);

        UpdatePlacementPreview();
    }

    // Card floats above the pointer (bottom edge + gap) so the tile under the pointer is not covered
    Vector2 DragLift()
    {
        float units = view.resolvedStyle.height * 0.5f * transform.localScale.y + dragPointerGap;
        // Constant clearance (not per-cell) so the card doesn't jump while the preview snaps
        units += PreviewClearance * GameUIPanel.Instance.UnitsPerWorld;
        return Vector2.up * units;
    }

    void OnEndDrag()
    {
        if (!isDragging) return;
        isDragging = false;

        transform.rotation = Quaternion.identity;

        view.style.backgroundImage = new StyleBackground(defaultCard);
        alpha = 1f;

        if (GameManager.instance != null)
            GameManager.instance.onDrag = false;

        if (UIManager.instance != null)
            UIManager.instance.ShowTowerSlot = false;
        if (Hand != null)
            Hand.HideGhost();
        RangeIndicator.Hide();
        HidePlacementFeedback();

        var trash = Trash.Instance != null ? Trash.Instance : FindAnyObjectByType<Trash>();
        bool isTrash = (trash != null && (trash.hit2D || trash.IsPointerOver()));

        if (isTrash)
        {
            LeaveHand();
            Destroy(gameObject);
            return;
        }

        spawnCard();
    }

    // ------------------------------------------------ Card Placement & Slot Logic

    protected virtual void spawnCard()
    {
        Vector3 worldDropPos = GetRaycastOrigin();
        // Measured in screen space, from where the drag started
        float d = Vector2.Distance(Input.mousePosition, dragStartScreenPos);
        bool hasObstacle = HasObstacleBelow();

        if (!hasObstacle && d > dragThreshold && PlayableNow && InTerritory && AllowPlacement())
        {
            OnPlacementAccepted();
            CameraShake.MicroShake();
            LeaveHand();
            BeginPendingResolve();

            dropOrigin = worldDropPos;
            dropCell = UseGrid ? FootprintCell() : default;
            alpha = 0f;
            view.pickingMode = PickingMode.Ignore;

            if (BuiltBySoldier && BuilderSquad.Instance != null && BuilderSquad.Instance.Available)
            {
                Vector3 site = UseGrid ? PlacementGrid.CellCenter(dropCell) : worldDropPos;
                // The marker holds the cell while the soldier walks there, so nothing else drops on it
                GameObject marker = BuilderSquad.MarkSite(site);
                if (UseGrid)
                    PlacementGrid.Occupy(dropCell, marker);
                BuilderSquad.Instance.Send(site, () =>
                {
                    Destroy(marker);
                    // The card may be gone if the scene is unloading
                    return this != null ? PlayFlipAndResolve() : 0f;
                }, CarriesFlag);
            }
            else
            {
                PlayFlipAndResolve();
            }
        }
        else
        {
            ReturnToSlot();
        }
    }

    // Cards that put an object on the map send a soldier from the bunker to build it first
    protected virtual bool BuiltBySoldier => false;
    // The soldier walks out carrying a flag (the flag bearer) instead of as a plain builder
    protected virtual bool CarriesFlag => false;

    // Plays the flip where the object goes and resolves the card when it ends; returns the flip's length
    float PlayFlipAndResolve()
    {
        float flipDuration = 0f;
        if (CardFlipAnim != null)
        {
            GameObject flip = Instantiate(CardFlipAnim, dropOrigin, Quaternion.identity);
            if (flip.TryGetComponent(out OneShotEffect fx))
                flipDuration = fx.Duration;
            else
                Destroy(flip, FallbackFlipDuration);
            // The flip holds the cell until the real occupant takes it over, so nothing else drops there meanwhile
            if (UseGrid)
                PlacementGrid.Occupy(dropCell, flip);
        }

        StartCoroutine(ResolveAfter(flipDuration));
        return flipDuration;
    }

    // Last say before a drop is accepted (e.g. the tower limit); false sends the card back to the hand
    protected virtual bool AllowPlacement() => true;
    // The drop is accepted; CardBehaviour runs after the flip animation (and the builder's walk)
    protected virtual void OnPlacementAccepted() { }

    const float FallbackFlipDuration = 0.46f;

    // The card is already spent (slot freed, count decremented); it stays alive, hidden, only to run CardBehaviour once the flip ends
    IEnumerator ResolveAfter(float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);
        CardBehaviour();
        EndPendingResolve();
        Destroy(gameObject);
    }

    protected void ReturnToSlot()
    {
        RestoreHandOrder();

        LeanTween.cancel(gameObject);
        TweenAnchored(baseAnchoredPos, 0.25f).setEaseOutBack();
        LeanTween.scale(gameObject, Vector3.one, 0.2f).setEaseOutBack();
        LeanTween.rotateZ(gameObject, 0f, 0.2f);
    }

    protected abstract void CardBehaviour();

    public void showCard()
    {
        handPosition = baseAnchoredPos - new Vector2(0f, 35f);
        TweenAnchored(baseAnchoredPos, 0.25f).setEaseOutQuad();
    }
}
