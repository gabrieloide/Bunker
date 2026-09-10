using UnityEngine;

public enum BuffType
{
    AttackBuff,
    SpeedBuff,
    BulletPenBuff
}

public class BuffCard : Card
{
    [Header("Sound")]
    [SerializeField] AK.Wwise.Event buffSound;
    [Space]
    [SerializeField] GameObject BuffSprite;
    [SerializeField] BuffType buffType = BuffType.AttackBuff;
    LayerMask NormalCardLM() => LayerMask.GetMask("Turret");
    [Range(1.1f, 3f)][SerializeField] float multiplierStat;

    TurretCard pendingTurret;

    protected override RaycastHit2D DetectObjectsBelow() => Physics2D.BoxCast(transform.position + offset, new Vector2(width, height), 0f, Vector2.down, 0.1f, NormalCardLM());

    protected override void spawnCard()
    {
        var hit = DetectObjectsBelow();
        pendingTurret = hit ? hit.collider.gameObject.GetComponent<TurretCard>() : null;

        if (pendingTurret != null && !pendingTurret.HaveBuff)
        {
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
        pendingTurret.ShowBuffSprite(BuffSprite);
        pendingTurret.HaveBuff = true;
        if (SimulationBridge.Instance != null)
            SimulationBridge.Instance.RequestBuff(pendingTurret.Entity, buffType, multiplierStat);
    }
}
