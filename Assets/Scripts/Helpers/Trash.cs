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

    public static Trash Instance { get; private set; }

    private SpriteRenderer trashRenderer;
    private Camera mainCamera;
    private bool wasOpen;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(gameObject);

        if (trashGO != null)
            trashRenderer = trashGO.GetComponent<SpriteRenderer>();

        mainCamera = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
    }

    public bool IsPointerOver()
    {
        if (mainCamera == null)
            mainCamera = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();

        if (mainCamera == null) return false;

        Vector3 worldMouse = mainCamera.ScreenToWorldPoint(Input.mousePosition);
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
        bool dragging = GameManager.instance != null && GameManager.instance.onDrag;

        if (dragging)
        {
            hit2D = Physics2D.BoxCast(transform.position, new Vector2(widthBox, heightBox), 0f, Vector2.zero, 0f, CardLayer);
        }
        else
        {
            hit2D = default;
        }

        bool isOpen = dragging && (hit2D || IsPointerOver());

        if (isOpen != wasOpen)
        {
            wasOpen = isOpen;

            if (trashRenderer != null)
                trashRenderer.sprite = isOpen ? openTrash : defaultTrash;

            if (isOpen)
            {
                openTrashSound.Post(gameObject);
            }
            else
            {
                closeTrashSound.Post(gameObject);
            }
        }
    }
}
