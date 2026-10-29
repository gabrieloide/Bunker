using UnityEngine;

// Feeds the turret's life into its bar
[RequireComponent(typeof(WorldLifeBar))]
public class ManagerTurretLife : MonoBehaviour
{
    WorldLifeBar lifeBar;
    private TurretCard turretCard;
    private float startLife;

    private void Start()
    {
        lifeBar = GetComponent<WorldLifeBar>();
        turretCard = GetComponentInParent<TurretCard>();
        if (turretCard != null)
            startLife = turretCard.Life;
    }

    void Update()
    {
        // A life buff raises the max as well
        if (turretCard != null)
            lifeBar.SetValue(turretCard.Life, turretCard.MaxLife > 0f ? turretCard.MaxLife : startLife);
    }
}
