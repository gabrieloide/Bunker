using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BuffType
{
    AttackBuff,
    SpeedBuff,
    BulletPenBuff

}
public class BuffCard : Card
{
    [SerializeField] GameObject BuffSprite;
    [SerializeField] BuffType buffType = BuffType.AttackBuff;
    LayerMask NormalCardLM() => LayerMask.GetMask("Turret");
    [Range(1.1f, 3f)][SerializeField] float multiplierStat;
    protected override RaycastHit2D DetectObjectsBelow() => Physics2D.BoxCast(transform.position + offset, new Vector2(width, height), 0f, Vector2.down, 0.1f, NormalCardLM());
    protected override void spawnCard()
    {
        if (DetectObjectsBelow() && !DetectObjectsBelow().collider.gameObject.GetComponent<TurretCard>().HaveBuff)
        {
            //Usar carta
            GameManager.instance.CurrentCardAmount--;
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
        var Turret = DetectObjectsBelow().collider.gameObject.GetComponent<NormalTurret>();
        Turret.ShowBuffSprite(BuffSprite);
        if (!Turret.HaveBuff)
        {
            Turret.HaveBuff = false;
            switch (buffType)
            {

                case BuffType.AttackBuff:
                    Turret.damage = Mathf.Floor(Turret.damage * multiplierStat);
                    break;

                case BuffType.SpeedBuff:
                    Turret.fireRateCountDown = Mathf.Floor(Turret.fireRateCountDown * multiplierStat);
                    break;

                case BuffType.BulletPenBuff:
                    Turret.bulletPen = Mathf.Floor(Turret.bulletPen * multiplierStat);
                    break;
            }

        }
    }
}
