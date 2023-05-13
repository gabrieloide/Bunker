using UnityEngine;

public class NormalCard : Card
{
    LayerMask NormalCardLM() => LayerMask.GetMask("Decoration", "Path");
    protected override RaycastHit2D DetectObjectsBelow() => Physics2D.BoxCast(transform.position + offset, new Vector2(width, height), 0f, Vector2.down, 0.1f, NormalCardLM());

    protected override void CardBehaviour()
    {
        Vector2 Mouseposition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Instantiate(towerData.CardToInstantiate, Mouseposition, Quaternion.identity);
    }
}