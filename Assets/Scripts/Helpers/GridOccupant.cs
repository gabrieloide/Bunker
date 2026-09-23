using UnityEngine;

// Frees its cell when the placed object is destroyed (tower death, mine explosion, scene unload).
public class GridOccupant : MonoBehaviour
{
    [HideInInspector] public Vector3Int Cell;

    void OnDestroy() => PlacementGrid.Release(Cell, gameObject);
}
