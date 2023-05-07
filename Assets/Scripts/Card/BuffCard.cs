using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BuffType
{
    AttackBuff,
    SpeedBuff,
    BulletPenBuff,

}
public class BuffCard : Card
{
    [SerializeField]BuffType buffType = BuffType.AttackBuff;
    LayerMask NormalCardLM() => LayerMask.GetMask("Turret");
    [Range(1.1f, 3f)][SerializeField] float multiplierStat;
    protected override RaycastHit2D DetectObjectsBelow() => Physics2D.BoxCast(transform.position + offset, new Vector2(width, height), 0f, Vector2.down, 0.1f, NormalCardLM());
    protected override void spawnCard()
    {
        if (DetectObjectsBelow())
        {
            //Usar carta
            dc.availableCardSlots[index()] = true;
            CardBehaviour();
            Destroy(gameObject);
        }
        else
        {
            transform.position = dc.cardSlots[index()].position;
            transform.localScale = Vector3.one;
        }
    }
    protected override void CardBehaviour()
    {
        TypeOfBuff();
    }
    void TypeOfBuff()
    {
        var Turret = DetectObjectsBelow().collider.gameObject.GetComponent<TurretCard>();
        switch (buffType)
        {
            case BuffType.AttackBuff:
                Turret.Damage *= multiplierStat; 
                break;

            case BuffType.SpeedBuff:
                Turret.fireRateCountDown *= multiplierStat;
                break;

            case BuffType.BulletPenBuff:
                Turret.BulletPen *= multiplierStat;
                break;
            default:
                break;
        }
    }
}
