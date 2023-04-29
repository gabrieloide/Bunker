using UnityEngine;

public class NormalCard : Card
{
    public float width, height;
    public Vector3 offset;
    protected override bool ReturnToHand()
    {
        RaycastHit2D raycastHit2D = Physics2D.BoxCast(transform.position + offset, new Vector2(width, 0.1f), 0f, Vector2.down, 0.1f, objectLayerMask);
        return raycastHit2D;
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position + offset, new Vector2(width, height));
    }
    protected override void CardBehaviour()
    {
        Vector2 Mouseposition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Instantiate(towerData.CardToInstantiate, Mouseposition, Quaternion.identity);
    }
}