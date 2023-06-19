using UnityEngine;

public class Trash : MonoBehaviour
{
    [Header("Sounds")]
    [SerializeField] AK.Wwise.Event openTrashSound;
    [SerializeField] AK.Wwise.Event closeTrashSound;
    [Space]
    
    [SerializeField] float widthBox, heightBox;
    [SerializeField] LayerMask CardLayer;
    [HideInInspector]public RaycastHit2D hit2D;
    [SerializeField] Sprite defaultTrash, openTrash;
    [SerializeField] GameObject trashGO;
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, new Vector2(widthBox, heightBox));
    }
    private void LateUpdate()
    {
        hit2D = Physics2D.BoxCast(transform.position, new Vector2(widthBox, heightBox), 360, Vector2.one, 5, CardLayer);
        if (hit2D)
        {
            trashGO.GetComponent<SpriteRenderer>().sprite = openTrash;
        }
        else
        {
            trashGO.GetComponent<SpriteRenderer>().sprite = defaultTrash;
        }
    }
}
