using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuffCard : Card
{
    [SerializeField] float width, height;
    [SerializeField] LayerMask turretLayerMask;
    protected override void CardBehaviour()
    {
        RaycastHit2D hitTurret = Physics2D.BoxCast(transform.position, new Vector2(width, height), 0, Vector2.down, 1, turretLayerMask);
        if (hitTurret)
        {
            Debug.Log("Pego a la torre");
        }
    }

    protected override bool ReturnToHand()
    {
        throw new System.NotImplementedException();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, new Vector2(width, height));
    }
}
