using UnityEngine;

[CreateAssetMenu(fileName = "New Ally Data", menuName = "Ally Data")]
public class AllyData : ScriptableObject
{
    public GameObject CardType;

    public float Life;
    public float Damage;
    public float MoveSpeed;
}
