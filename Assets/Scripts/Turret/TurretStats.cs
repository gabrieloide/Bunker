using UnityEngine;

// Shared sounds for things placed by cards. Gameplay numbers come from the CardDefinition.
public class TurretStats : MonoBehaviour
{
    [Header("Sounds")]
    [SerializeField] protected AK.Wwise.Event shoot;
    [SerializeField] protected AK.Wwise.Event destroy;

    // LEGACY: moved to CardDefinition, dropped after migration
    [HideInInspector] public float damage;
    [HideInInspector] public float bulletPen;
}
