using UnityEngine;

// Shared sounds for things placed by cards. Gameplay numbers come from the CardDefinition.
public class TurretStats : MonoBehaviour
{
    [Header("Sounds")]
    [SerializeField] protected Sfx shoot = Sfx.TowerShot;
    [SerializeField] protected Sfx destroy = Sfx.None;
}
