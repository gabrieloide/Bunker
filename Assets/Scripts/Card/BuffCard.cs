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
    [Tooltip("One-shot burst played on the tower (OneShotEffect prefab)")]
    [SerializeField] GameObject buffEffect;
    [SerializeField] Color buffEffectTint = Color.white;
    LayerMask NormalCardLM() => LayerMask.GetMask("Turret");

    // LEGACY: moved to BuffCardDefinition, dropped after migration
    [HideInInspector][SerializeField] BuffType buffType = BuffType.AttackBuff;
    [HideInInspector][SerializeField] float multiplierStat;
    public BuffType LegacyBuffType => buffType;
    public float LegacyAmount => multiplierStat;

    TurretCard pendingTurret;

    protected override RaycastHit2D DetectObjectsBelow() => CastFootprint(NormalCardLM());

    // Towers are 1x2 with their occupied cell at the base, so the pointer may be over the base cell or the one above it
    protected override float PreviewClearance => PlacementGrid.Available ? PlacementGrid.CellSize.y : 0f;

    // Buffs don't snap: the preview jumps onto the targeted tower's cell and hides when nothing valid is under the pointer
    protected override bool TryGetPreviewPosition(out Vector3 position)
    {
        TurretCard target = FindTarget(out position);
        return target != null && !target.HaveBuff;
    }

    TurretCard FindTarget(out Vector3 previewPosition)
    {
        previewPosition = default;

        if (!PlacementGrid.Available)
        {
            var hit = DetectObjectsBelow();
            TurretCard turret = hit ? hit.collider.GetComponent<TurretCard>() : null;
            if (turret != null) previewPosition = turret.transform.position;
            return turret;
        }

        Vector3Int cell = PlacementGrid.WorldToCell(PointerWorld());
        if (TryTurretAt(cell, out TurretCard found) || TryTurretAt(cell + Vector3Int.down, out found))
        {
            previewPosition = PlacementGrid.CellCenter(found.GetComponent<GridOccupant>().Cell);
            return found;
        }
        return null;
    }

    static bool TryTurretAt(Vector3Int cell, out TurretCard turret)
    {
        turret = PlacementGrid.TryGetOccupant(cell, out GameObject occupant) ? occupant.GetComponent<TurretCard>() : null;
        return turret != null;
    }

    protected override void spawnCard()
    {
        pendingTurret = FindTarget(out _);

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
        pendingTurret.ApplyBuff(BuffSprite, buffEffect, buffEffectTint);
        pendingTurret.HaveBuff = true;
        // Posted on the tower: the card is destroyed this frame and would cut the sound
        if (buffSound != null && buffSound.IsValid())
            buffSound.Post(pendingTurret.gameObject);
        if (SimulationBridge.Instance != null && definition is BuffCardDefinition buff)
            SimulationBridge.Instance.RequestBuff(pendingTurret.Entity, buff.buffType, buff.amount);
    }
}
