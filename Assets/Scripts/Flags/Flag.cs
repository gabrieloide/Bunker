using UnityEngine;

// Claims the ground within Radius: the player's cards can only be played inside some flag's radius.
public class Flag : MonoBehaviour
{
    [Min(0.5f)] [SerializeField] float radius = 6f;

    public float Radius
    {
        get => radius;
        set
        {
            radius = Mathf.Max(0.5f, value);
            FlagTerritory.NotifyChanged();
        }
    }

    public Vector2 Center => transform.position;

    void OnEnable() => FlagTerritory.Register(this);
    void OnDisable() => FlagTerritory.Unregister(this);

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
