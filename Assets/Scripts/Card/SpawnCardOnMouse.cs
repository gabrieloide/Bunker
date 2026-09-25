using UnityEngine;

public class SpawnCardOnMouse : Card
{
    protected override bool SnapsToGrid => true;
<<<<<<< HEAD
=======
    protected override bool BuiltBySoldier => true;
>>>>>>> 1b0f21870329b922747c317aa3561b86f80f1c88

    protected override void CardBehaviour()
    {
        SpawnPlacement();
    }
}
