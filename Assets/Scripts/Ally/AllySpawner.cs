using UnityEngine;

// Scene authoring for allies: where they leave from and every ally type an ally card can send out.
public class AllySpawner : MonoBehaviour
{
    [Tooltip("Ally types the ally cards can send; a card's ally must be listed here")]
    public AllyView[] Allies;
}
