using UnityEngine;

// What a demolished tower leaves for the rest of the match: its cell stays taken and so do the free
// neighbouring cells (never the enemy path), and the ruins keep one slot of the tower limit.
// Art comes from the CardCatalog; without it the tower's own sprite lies there greyed out.
public class Ruin : MonoBehaviour
{
    // Only the main ruin (the tower's cell) holds a tower-limit slot
    bool holdsSlot;

    void OnEnable()
    {
        if (holdsSlot) TowerLimits.AddRuins(1);
    }

    void OnDisable()
    {
        if (holdsSlot) TowerLimits.AddRuins(-1);
    }

    public static void Leave(Vector3Int cell, Vector3 towerPosition, SpriteRenderer towerBody, Vector3Int[] neighbours)
    {
        var catalog = GameManager.instance != null ? GameManager.instance.Catalog : null;

        var main = Spawn("Ruins", towerPosition, cell, towerBody);
        var sr = main.GetComponent<SpriteRenderer>();
        if (catalog != null && catalog.ruinsSprite != null)
            sr.sprite = catalog.ruinsSprite;
        else
        {
            // Placeholder: the tower lying on its side, greyed out
            sr.color = new Color(0.45f, 0.45f, 0.45f, 0.85f);
            main.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        }
        var ruin = main.GetComponent<Ruin>();
        ruin.holdsSlot = true;
        TowerLimits.AddRuins(1);

        foreach (var offset in neighbours)
        {
            Vector3Int next = cell + offset;
            if (!PlacementGrid.CanPlaceAt(next)) continue;
            var blocked = Spawn("Rubble", PlacementGrid.CellCenter(next), next, towerBody);
            var marker = blocked.GetComponent<SpriteRenderer>();
            marker.sprite = catalog != null ? catalog.blockedCellSprite : null;
            marker.color = new Color(1f, 1f, 1f, 0.35f);
        }
    }

    static GameObject Spawn(string name, Vector3 position, Vector3Int cell, SpriteRenderer towerBody)
    {
        var go = new GameObject(name);
        go.transform.position = position;
        var sr = go.AddComponent<SpriteRenderer>();
        if (towerBody != null)
        {
            sr.sprite = towerBody.sprite;
            sr.sortingLayerID = towerBody.sortingLayerID;
            sr.sortingOrder = towerBody.sortingOrder - 1;
        }
        go.AddComponent<Ruin>();
        PlacementGrid.Occupy(cell, go);
        return go;
    }
}
