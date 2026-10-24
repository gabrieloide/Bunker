using UnityEngine;
using UnityEngine.EventSystems;

// Tear a tower down: during preparation, drag it off the map onto the trash. It leaves ruins for the
// rest of the match (see Ruin), so swapping a tower for a better one is never free.
// Added by SimulationBridge to every tower it spawns.
public class TowerDemolition : MonoBehaviour
{
    static readonly Vector3Int[] Neighbours = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };

    GameObject ghost;

    void Update()
    {
        if (ghost == null)
        {
            if (Input.GetMouseButtonDown(0) && CanStartDrag())
                BeginDrag();
            return;
        }

        ghost.transform.position = PointerWorld();
        if (Input.GetMouseButtonUp(0))
            EndDrag();
    }

    void OnDestroy()
    {
        if (ghost != null) EndDrag(demolish: false);
    }

    bool CanStartDrag()
    {
        if (!SimulationBridge.IsPreparationPhase || !PlacementGrid.Available) return false;
        if (GameManager.instance == null || GameManager.instance.onDrag) return false;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return false;
        // Same pick as tower buffs: the tower's own cell or the one under it (the art stands half a cell up)
        Vector3Int cell = PlacementGrid.WorldToCell(PointerWorld());
        return IsMine(cell) || IsMine(cell + Vector3Int.down);
    }

    bool IsMine(Vector3Int cell) => PlacementGrid.TryGetOccupant(cell, out GameObject occupant) && occupant == gameObject;

    void BeginDrag()
    {
        var body = GetComponentInChildren<SpriteRenderer>();
        ghost = new GameObject("DemolishGhost");
        if (body != null)
        {
            var sr = ghost.AddComponent<SpriteRenderer>();
            sr.sprite = body.sprite;
            sr.color = new Color(1f, 1f, 1f, 0.6f);
            sr.sortingLayerID = body.sortingLayerID;
            sr.sortingOrder = body.sortingOrder + 50;
        }
        ghost.transform.position = PointerWorld();
        // Lets the trash open under the pointer, as with cards
        GameManager.instance.onDrag = true;
    }

    void EndDrag(bool demolish = true)
    {
        Destroy(ghost);
        ghost = null;
        if (GameManager.instance != null)
            GameManager.instance.onDrag = false;

        var trash = Trash.Instance;
        if (demolish && SimulationBridge.IsPreparationPhase && trash != null && trash.IsPointerOver())
            Demolish();
    }

    void Demolish()
    {
        if (!TryGetComponent(out GridOccupant occupant)) return;
        Vector3Int cell = occupant.Cell;
        var body = GetComponentInChildren<SpriteRenderer>();
        Ruin.Leave(cell, transform.position, body, Neighbours);

        var turret = GetComponent<TurretCard>();
        if (SimulationBridge.Instance != null && turret != null)
            SimulationBridge.Instance.DemolishTower(turret.Entity, gameObject);
        else
            Destroy(gameObject);
    }

    static Vector3 PointerWorld()
    {
        if (Camera.main == null) return Vector3.zero;
        Vector3 world = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        world.z = 0f;
        return world;
    }
}
