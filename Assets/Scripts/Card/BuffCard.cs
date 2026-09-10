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

    protected override RaycastHit2D DetectObjectsBelow() => Physics2D.BoxCast(GetRaycastOrigin() + offset, new Vector2(width, height), 0f, Vector2.down, 0.1f, NormalCardLM());

    protected override void spawnCard()
    {
        var hit = DetectObjectsBelow();
        pendingTurret = hit ? hit.collider.gameObject.GetComponent<TurretCard>() : null;

        if (pendingTurret != null && !pendingTurret.HaveBuff)
        {
            if (GameManager.instance != null)
                GameManager.instance.CurrentCardAmount--;
            if (dc != null && index() < dc.availableCardSlots.Length)
                dc.availableCardSlots[index()] = true;
            CardBehaviour();
            Destroy(gameObject);
        }
        else
        {
            ReturnToSlot();
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
