using UnityEngine;

public class NormalCard : Card
{
    LayerMask NormalCardLM() => LayerMask.GetMask("Decoration", "Path", "Limits");
    protected override bool SnapsToGrid => true;
    protected override RaycastHit2D DetectObjectsBelow() => CastFootprint(NormalCardLM());

    protected override void CardBehaviour()
    {
        SpawnPlacement();
    }
}
