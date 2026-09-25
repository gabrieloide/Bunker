using UnityEngine;
using UnityEngine.EventSystems;

// Claims the ground within Radius: the player's cards can only be played inside some flag's radius.
// The radius is drawn as a dotted ring while a card is being dragged or the pointer is over the flag.
public class Flag : MonoBehaviour, ICardConfigurable
{
    [Min(0.5f)] [SerializeField] float radius = 6f;
    [SerializeField] DottedRing ring;
    [Tooltip("Sprite whose bounds count as hovering the flag")]
    [SerializeField] SpriteRenderer body;

    public float Radius
    {
        get => radius;
        set
        {
            radius = Mathf.Max(0.5f, value);
            if (ring != null) ring.SetRadius(radius);
            FlagTerritory.NotifyChanged();
        }
    }

    public Vector2 Center => transform.position;

    void Awake()
    {
        if (ring != null)
        {
            ring.SetRadius(radius);
            ring.gameObject.SetActive(false);
        }
    }

    void OnEnable() => FlagTerritory.Register(this);
    void OnDisable() => FlagTerritory.Unregister(this);

    public void Configure(CardDefinition definition)
    {
        if (definition is FlagCardDefinition flag)
            Radius = flag.radius;
    }

    void Update()
    {
        if (ring == null) return;
        bool show = (GameManager.instance != null && GameManager.instance.onDrag) || Hovered();
        if (ring.gameObject.activeSelf != show)
            ring.gameObject.SetActive(show);
    }

    bool Hovered()
    {
        if (body == null || Camera.main == null) return false;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return false;
        Vector3 pointer = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        pointer.z = body.bounds.center.z;
        return body.bounds.Contains(pointer);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
