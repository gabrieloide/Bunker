using UnityEngine;
using UnityEngine.EventSystems;

// Shows a tower's attack range as a dotted ring: driven by the dragged card while placing,
// otherwise by hovering a placed tower.
public class RangeIndicator : MonoBehaviour
{
    static RangeIndicator instance;

    [SerializeField] DottedRing ring;
    [SerializeField] LayerMask turretMask;

    bool cardDriven;

    void Awake()
    {
        instance = this;
        if (ring != null) ring.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public static void Show(Vector3 center, float radius)
    {
        if (instance == null) return;
        instance.cardDriven = true;
        instance.Place(center, radius);
    }

    public static void Hide()
    {
        if (instance == null) return;
        instance.cardDriven = false;
        instance.SetVisible(false);
    }

    void Update()
    {
        if (cardDriven || ring == null) return;
        if (GameManager.instance != null && GameManager.instance.onDrag) return;

        TurretCard hovered = HoveredTurret();
        if (hovered != null)
            Place(hovered.transform.position, hovered.Range);
        else
            SetVisible(false);
    }

    TurretCard HoveredTurret()
    {
        if (Camera.main == null) return null;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return null;

        Vector2 pointer = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Collider2D hit = Physics2D.OverlapPoint(pointer, turretMask);
        return hit != null ? hit.GetComponentInParent<TurretCard>() : null;
    }

    void Place(Vector3 center, float radius)
    {
        if (ring == null) return;
        center.z = 0f;
        ring.transform.position = center;
        ring.SetRadius(radius);
        SetVisible(true);
    }

    void SetVisible(bool visible)
    {
        if (ring != null && ring.gameObject.activeSelf != visible)
            ring.gameObject.SetActive(visible);
    }
}
