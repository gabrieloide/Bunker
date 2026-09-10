using UnityEngine;

public class Trash : MonoBehaviour
{
    [Header("Sounds")]
    [SerializeField] AK.Wwise.Event openTrashSound;
    [SerializeField] AK.Wwise.Event closeTrashSound;
    [Space]
    
    [SerializeField] float widthBox, heightBox;
    [SerializeField] LayerMask CardLayer;
    [HideInInspector] public RaycastHit2D hit2D;
    [SerializeField] Sprite defaultTrash, openTrash;
    [SerializeField] GameObject trashGO;

    public bool IsPointerOver()
    {
        if (Camera.main == null) return false;
        Vector3 worldMouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Bounds b = new Bounds(transform.position, new Vector3(widthBox, heightBox, 10f));
        return b.Contains(new Vector3(worldMouse.x, worldMouse.y, transform.position.z));
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, new Vector2(widthBox, heightBox));
    }

    private void LateUpdate()
    {
        hit2D = Physics2D.BoxCast(transform.position, new Vector2(widthBox, heightBox), 360, Vector2.one, 5, CardLayer);
        bool isHovered = hit2D || (GameManager.instance != null && GameManager.instance.onDrag && IsPointerOver());
        if (isHovered)
        {
            if (trashGO != null) trashGO.GetComponent<SpriteRenderer>().sprite = openTrash;
        }
        else
        {
            if (trashGO != null) trashGO.GetComponent<SpriteRenderer>().sprite = defaultTrash;
        }
    }
}
