using UnityEngine;

public class AirAttackCard : Card
{
    protected override void CardBehaviour()
    {
        AirSupportQueue.Enqueue(definition, DropOrigin);
    }
}
