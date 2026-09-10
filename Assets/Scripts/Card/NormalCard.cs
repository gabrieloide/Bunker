using UnityEngine;

public class NormalCard : Card
{
    LayerMask NormalCardLM() => LayerMask.GetMask("Decoration", "Path", "Limits");
    protected override RaycastHit2D DetectObjectsBelow() => Physics2D.BoxCast(GetRaycastOrigin() + offset, new Vector2(width, height), 0f, Vector2.down, 0.1f, NormalCardLM());

    protected override void CardBehaviour()
    {
        Vector2 Mouseposition = GetRaycastOrigin();
        SimulationBridge.SpawnFromCard(towerData.CardToInstantiate, Mouseposition);
    }
}
