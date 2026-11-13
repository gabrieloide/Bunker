using System.Collections.Generic;
using Bunker.Simulation;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

// Shared editor helpers for drawing, validating, splitting, merging, and tile-painting enemy/ally routes.
public static class RouteAuthoringUtility
{
    public static readonly Color DefaultActiveColor = new Color(0.15f, 0.85f, 1f, 1f);
    public static readonly Color BlockedColor = new Color(1f, 0.25f, 0.25f, 1f);
    public static readonly Color StartColor = new Color(0.2f, 1f, 0.3f, 1f);
    public static readonly Color EndColor = new Color(1f, 0.35f, 0.35f, 1f);
    public static readonly Color ForkColor = new Color(1f, 0.82f, 0.2f, 1f);

    static readonly Color[] Palette =
    {
        new Color(0.15f, 0.85f, 1f, 1f),
        new Color(1f, 0.55f, 0.15f, 1f),
        new Color(0.75f, 0.45f, 1f, 1f),
        new Color(0.35f, 1f, 0.55f, 1f),
        new Color(1f, 0.35f, 0.7f, 1f)
    };

    public static Color PickPaletteColor(int index) => Palette[Mathf.Abs(index) % Palette.Length];

    public static Vector3 Snap(Vector3 position)
    {
        if (!PlacementGrid.Available)
        {
            position.x = Mathf.Round(position.x * 2f) * 0.5f;
            position.y = Mathf.Round(position.y * 2f) * 0.5f;
            return position;
        }
        Vector3 centre = PlacementGrid.CellCenter(PlacementGrid.WorldToCell(position));
        centre.z = position.z;
        return centre;
    }

    public static Vector3Int WorldToCell(Vector3 world)
    {
        return PlacementGrid.Available
            ? PlacementGrid.WorldToCell(world)
            : new Vector3Int(Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.y), 0);
    }

    public static Vector3 CellCenter(Vector3Int cell, float z = 0f)
    {
        Vector3 p = PlacementGrid.Available ? PlacementGrid.CellCenter(cell) : new Vector3(cell.x, cell.y, 0f);
        p.z = z;
        return p;
    }

    public static void Edit(EnemyMovement route, string undoName, System.Action<List<Vector3>> change)
    {
        if (route == null) return;
        Undo.RecordObject(route, undoName);
        if (route.transform.position != Vector3.zero)
        {
            Undo.RecordObject(route.transform, undoName);
            route.transform.position = Vector3.zero;
        }
        var list = new List<Vector3>(route.points ?? new Vector3[0]);
        change(list);
        route.points = list.ToArray();
        EditorUtility.SetDirty(route);
        SceneView.RepaintAll();
    }

    public static EnemySpawner FindDirectSpawner(EnemyMovement route)
    {
        if (route == null) return null;
        var spawners = Object.FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None);
        foreach (var s in spawners)
            if (s != null && s.path == route) return s;
        return null;
    }

    public static Vector3? FindBunkerDoorPosition()
    {
        var door = Object.FindAnyObjectByType<AllySpawner>();
        if (door != null) return Snap(new Vector3(door.transform.position.x, door.transform.position.y, 0f));
        var player = Object.FindAnyObjectByType<TowerPlayer>();
        if (player != null) return Snap(new Vector3(player.transform.position.x, player.transform.position.y, 0f));
        return null;
    }

    public static void SnapEnds(EnemyMovement route, List<Vector3> points)
    {
        float z = points.Count > 0 ? points[0].z : 0f;
        var spawner = FindDirectSpawner(route);
        if (spawner == null && !HasAnyParent(route))
        {
            var all = Object.FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None);
            if (all.Length == 1) spawner = all[0];
        }

        if (spawner != null)
        {
            var start = Snap(new Vector3(spawner.transform.position.x, spawner.transform.position.y, z));
            if (points.Count == 0) points.Add(start); else points[0] = start;
        }

        if (!route.Forks)
        {
            var doorPos = FindBunkerDoorPosition();
            if (doorPos.HasValue)
            {
                var end = new Vector3(doorPos.Value.x, doorPos.Value.y, z);
                if (points.Count < 2) points.Add(end); else points[points.Count - 1] = end;
            }
        }
    }

    public static bool HasAnyParent(EnemyMovement route)
    {
        if (route == null) return false;
        var all = Object.FindObjectsByType<EnemyMovement>(FindObjectsSortMode.None);
        foreach (var other in all)
        {
            if (other == null || other == route || !other.Forks) continue;
            foreach (var b in other.branches)
                if (b == route) return true;
        }
        return false;
    }

    public static List<EnemyMovement> FindParents(EnemyMovement route)
    {
        var parents = new List<EnemyMovement>();
        if (route == null) return parents;
        var all = Object.FindObjectsByType<EnemyMovement>(FindObjectsSortMode.None);
        foreach (var other in all)
        {
            if (other == null || other == route || !other.Forks) continue;
            foreach (var b in other.branches)
            {
                if (b == route && !parents.Contains(other))
                    parents.Add(other);
            }
        }
        return parents;
    }

    public static bool LegBlocked(Vector3 from, Vector3 to, Vector3Int routeStart, Vector3Int routeEnd, bool ignoreEndpoints)
    {
        if (!PlacementGrid.Available) return false;
        var at = PlacementGrid.WorldToCell(from);
        var end = PlacementGrid.WorldToCell(to);
        int guard = 0;
        while (guard++ < 2048)
        {
            bool isEndpoint = ignoreEndpoints && (at == routeStart || at == routeEnd);
            if (!isEndpoint && PlacementGrid.BlocksWalking(at)) return true;
            if (at == end) return false;
            if (at.x != end.x) at.x += System.Math.Sign(end.x - at.x);
            else at.y += System.Math.Sign(end.y - at.y);
        }
        return false;
    }

    public static void Measure(EnemyMovement route, out int length, out int blocked)
    {
        length = 0;
        blocked = 0;
        if (route == null) return;
        var cells = route.RouteCells();
        length = Mathf.Max(0, cells.Count - 1);
        for (int i = 1; i < cells.Count - 1; i++)
            if (PlacementGrid.Available && PlacementGrid.BlocksWalking(cells[i])) blocked++;
    }

    public static void DrawRouteLegs(EnemyMovement route, Color baseColor, float width, bool dim)
    {
        if (route == null || route.points == null || route.points.Length == 0) return;

        var walked = route.BuildRoute();
        Vector3Int first = walked.Count > 0 ? WorldToCell(walked[0].position) : default;
        Vector3Int last = walked.Count > 0 ? WorldToCell(walked[walked.Count - 1].position) : default;

        Color legCol = dim ? new Color(baseColor.r, baseColor.g, baseColor.b, 0.45f) : baseColor;
        Color badCol = dim ? new Color(BlockedColor.r, BlockedColor.g, BlockedColor.b, 0.55f) : BlockedColor;

        for (int i = 1; i < walked.Count; i++)
        {
            bool isBlocked = LegBlocked(walked[i - 1].position, walked[i].position, first, last, true);
            Handles.color = isBlocked ? badCol : legCol;
            Handles.DrawAAPolyLine(width, walked[i - 1].position, walked[i].position);
        }

        if (route.Forks && walked.Count > 0)
        {
            Vector3 fork = walked[walked.Count - 1].position;
            foreach (var branch in route.branches)
            {
                if (branch == null || branch.points == null || branch.points.Length == 0) continue;
                Vector3 branchStart = branch.points[0];
                Vector3Int bCell = WorldToCell(branchStart);
                if (RouteGeometry.TryGetCorner(fork, branchStart, false, out Vector3 corner))
                {
                    Handles.color = LegBlocked(fork, corner, last, bCell, false) ? badCol : legCol;
                    Handles.DrawAAPolyLine(width, fork, corner);
                    Handles.color = LegBlocked(corner, branchStart, last, bCell, false) ? badCol : legCol;
                    Handles.DrawAAPolyLine(width, corner, branchStart);
                }
                else
                {
                    Handles.color = LegBlocked(fork, branchStart, last, bCell, false) ? badCol : legCol;
                    Handles.DrawAAPolyLine(width, fork, branchStart);
                }
            }
        }
    }

    // Splits `route` at `flagIndex`, returning the newly created secondary branch for immediate editing.
    public static EnemyMovement SplitRouteAt(EnemyMovement route, int flagIndex)
    {
        if (route == null || route.points == null || route.points.Length == 0) return null;
        flagIndex = Mathf.Clamp(flagIndex, 0, route.points.Length - 1);

        var parentTransform = route.transform.parent;
        Vector3 forkPos = route.points[flagIndex];
        float z = forkPos.z;
        Vector3? doorPos = FindBunkerDoorPosition();
        Vector3 toDoor = doorPos.HasValue ? (doorPos.Value - forkPos) : Vector3.left * 6f;

        // Perpendicular offset so the new branch clearly separates from the fork
        Vector3 perp = Mathf.Abs(toDoor.x) >= Mathf.Abs(toDoor.y) ? Vector3.up * 2f : Vector3.right * 2f;
        Vector3 branchBStart = Snap(forkPos + perp);
        if (branchBStart == forkPos) branchBStart = Snap(forkPos + Vector3.up * 2f);
        Vector3 branchBEnd = doorPos.HasValue ? new Vector3(doorPos.Value.x, doorPos.Value.y, z) : Snap(branchBStart + toDoor);

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Split route");

        Undo.RecordObject(route, "Split route");
        if (route.transform.position != Vector3.zero)
        {
            Undo.RecordObject(route.transform, "Split route");
            route.transform.position = Vector3.zero;
        }

        int existingRoutes = Object.FindObjectsByType<EnemyMovement>(FindObjectsSortMode.None).Length;

        if (flagIndex < route.points.Length - 1)
        {
            // Mid-route split: keep [0..flagIndex] on trunk, move [flagIndex+1..end] into Branch A, create Branch B
            var trunkPoints = new Vector3[flagIndex + 1];
            for (int i = 0; i <= flagIndex; i++) trunkPoints[i] = route.points[i];

            var branchAPoints = new Vector3[route.points.Length - flagIndex - 1];
            for (int i = 0; i < branchAPoints.Length; i++) branchAPoints[i] = route.points[flagIndex + 1 + i];

            var oldBranches = route.branches;

            var branchA = CreateChildRouteObject(route.name + "_RamaA", parentTransform, PickPaletteColor(existingRoutes), branchAPoints, route.flagRadius, route.bunkerRadius);
            branchA.verticalFirst = route.verticalFirst;
            branchA.branches = oldBranches ?? new EnemyMovement[0];

            // Offset Branch B opposite to Branch A's first step when possible
            Vector3 dirA = branchAPoints[0] - forkPos;
            if (dirA.sqrMagnitude > 0.01f)
            {
                Vector3 side = Mathf.Abs(dirA.x) >= Mathf.Abs(dirA.y) ? Vector3.down * 2f : Vector3.left * 2f;
                branchBStart = Snap(forkPos + side);
            }
            var branchB = CreateChildRouteObject(route.name + "_RamaB", parentTransform, PickPaletteColor(existingRoutes + 1), new[] { branchBStart, branchBEnd }, route.flagRadius, route.bunkerRadius);

            route.points = trunkPoints;
            route.branches = new[] { branchA, branchB };
            EditorUtility.SetDirty(route);
            EditorUtility.SetDirty(branchA);
            EditorUtility.SetDirty(branchB);
            Undo.CollapseUndoOperations(undoGroup);
            SceneView.RepaintAll();
            return branchB;
        }

        // Splitting at the final flag of `route`
        if (route.Forks)
        {
            // Already forks here: add one more branch
            var branchExtra = CreateChildRouteObject(route.name + "_Rama" + (route.branches.Length + 1), parentTransform, PickPaletteColor(existingRoutes), new[] { branchBStart, branchBEnd }, route.flagRadius, route.bunkerRadius);
            var list = new List<EnemyMovement>(route.branches) { branchExtra };
            route.branches = list.ToArray();
            EditorUtility.SetDirty(route);
            Undo.CollapseUndoOperations(undoGroup);
            SceneView.RepaintAll();
            return branchExtra;
        }
        else
        {
            Vector3 branchAStart = Snap(forkPos - perp);
            var branchA = CreateChildRouteObject(route.name + "_RamaA", parentTransform, PickPaletteColor(existingRoutes), new[] { branchAStart, branchBEnd }, route.flagRadius, route.bunkerRadius);
            var branchB = CreateChildRouteObject(route.name + "_RamaB", parentTransform, PickPaletteColor(existingRoutes + 1), new[] { branchBStart, branchBEnd }, route.flagRadius, route.bunkerRadius);
            route.branches = new[] { branchA, branchB };
            EditorUtility.SetDirty(route);
            Undo.CollapseUndoOperations(undoGroup);
            SceneView.RepaintAll();
            return branchB;
        }
    }

    // Connects `sourceRoute`'s end into `targetRoute` at `targetFlagIndex` (supports 1->2->1 convergence).
    public static void MergeRouteInto(EnemyMovement sourceRoute, EnemyMovement targetRoute, int targetFlagIndex)
    {
        if (sourceRoute == null || targetRoute == null || sourceRoute == targetRoute) return;
        if (targetRoute.points == null || targetRoute.points.Length == 0) return;

        targetFlagIndex = Mathf.Clamp(targetFlagIndex, 0, targetRoute.points.Length - 1);

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Merge routes");

        EnemyMovement tailToJoin = targetRoute;

        if (targetFlagIndex > 0)
        {
            // Split targetRoute at targetFlagIndex so [targetFlagIndex..end] becomes the shared tail
            var trunkPoints = new Vector3[targetFlagIndex];
            for (int i = 0; i < targetFlagIndex; i++) trunkPoints[i] = targetRoute.points[i];

            var tailPoints = new Vector3[targetRoute.points.Length - targetFlagIndex];
            for (int i = 0; i < tailPoints.Length; i++) tailPoints[i] = targetRoute.points[targetFlagIndex + i];

            int count = Object.FindObjectsByType<EnemyMovement>(FindObjectsSortMode.None).Length;
            var sharedTail = CreateChildRouteObject(targetRoute.name + "_Union", targetRoute.transform.parent, PickPaletteColor(count), tailPoints, targetRoute.flagRadius, targetRoute.bunkerRadius);
            sharedTail.verticalFirst = targetRoute.verticalFirst;
            sharedTail.branches = targetRoute.branches ?? new EnemyMovement[0];

            Undo.RecordObject(targetRoute, "Merge routes");
            targetRoute.points = trunkPoints;
            targetRoute.branches = new[] { sharedTail };
            EditorUtility.SetDirty(targetRoute);
            EditorUtility.SetDirty(sharedTail);

            tailToJoin = sharedTail;
        }

        Undo.RecordObject(sourceRoute, "Merge routes");
        var branches = new List<EnemyMovement>(sourceRoute.branches ?? new EnemyMovement[0]);
        if (!branches.Contains(tailToJoin))
            branches.Add(tailToJoin);
        sourceRoute.branches = branches.ToArray();
        EditorUtility.SetDirty(sourceRoute);

        Undo.CollapseUndoOperations(undoGroup);
        SceneView.RepaintAll();
    }

    public static EnemyMovement CreateNewSpawnerAndRoute(Vector3 spawnWorldPos)
    {
        var waves = GameObject.Find("Waves");
        Transform parent = waves != null ? waves.transform : null;

        var existingSpawners = Object.FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None);
        int index = existingSpawners.Length + 1;

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Create new spawner and route");

        var frontGo = new GameObject($"Frente_{index}");
        Undo.RegisterCreatedObjectUndo(frontGo, "Create new spawner and route");
        if (parent != null) frontGo.transform.SetParent(parent, false);
        frontGo.transform.position = Vector3.zero;

        Vector3 snappedSpawn = Snap(new Vector3(spawnWorldPos.x, spawnWorldPos.y, 0f));

        var spawnerGo = new GameObject($"SpawnerEnemy_{index}");
        Undo.RegisterCreatedObjectUndo(spawnerGo, "Create new spawner and route");
        spawnerGo.transform.SetParent(frontGo.transform, false);
        spawnerGo.transform.position = snappedSpawn;
        var spawner = spawnerGo.AddComponent<EnemySpawner>();

        if (existingSpawners.Length > 0 && existingSpawners[0] != null)
            spawner.Enemies = existingSpawners[0].Enemies;

        Vector3 endPos = FindBunkerDoorPosition() ?? Snap(snappedSpawn + Vector3.left * 8f);
        var route = CreateChildRouteObject($"Path_{index}", frontGo.transform, PickPaletteColor(index - 1), new[] { snappedSpawn, endPos }, 0.5f, 0.6f);
        spawner.path = route;

        EditorUtility.SetDirty(spawner);
        EditorUtility.SetDirty(route);
        Undo.CollapseUndoOperations(undoGroup);
        SceneView.RepaintAll();
        return route;
    }

    public static EnemyBaseView CreateAdditionalEnemyBase(Vector3 worldPos)
    {
        var existing = Object.FindAnyObjectByType<EnemyBaseView>();
        Vector3 snapped = Snap(new Vector3(worldPos.x, worldPos.y, 0f));
        if (existing == null)
        {
            Debug.LogWarning("No hay ninguna EnemyBaseView existente en la escena para clonar.");
            return null;
        }
        var copy = Object.Instantiate(existing, snapped, Quaternion.identity, existing.transform.parent);
        int count = Object.FindObjectsByType<EnemyBaseView>(FindObjectsSortMode.None).Length;
        copy.name = $"EnemyBase_{count}";
        Undo.RegisterCreatedObjectUndo(copy.gameObject, "Add enemy base");
        return copy;
    }

    public static void DeleteRoute(EnemyMovement route)
    {
        if (route == null) return;
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Delete route");

        // Unlink from any parent routes
        foreach (var parent in FindParents(route))
        {
            Undo.RecordObject(parent, "Delete route");
            var list = new List<EnemyMovement>(parent.branches);
            list.RemoveAll(b => b == null || b == route);
            parent.branches = list.ToArray();
            EditorUtility.SetDirty(parent);
        }

        // Unlink direct spawner if it only belongs to this route
        var spawner = FindDirectSpawner(route);
        if (spawner != null)
            Undo.DestroyObjectImmediate(spawner.gameObject);

        Undo.DestroyObjectImmediate(route.gameObject);
        Undo.CollapseUndoOperations(undoGroup);
        SceneView.RepaintAll();
    }

    static EnemyMovement CreateChildRouteObject(string name, Transform parent, Color color, Vector3[] points, float flagRadius, float bunkerRadius)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create route");
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.position = Vector3.zero;
        var em = go.AddComponent<EnemyMovement>();
        em.pathColor = color;
        em.points = points;
        em.flagRadius = flagRadius;
        em.bunkerRadius = bunkerRadius;
        return em;
    }

    // Paints road border tiles around all active routes onto the Grid/Path Tilemap (matching LevelBuilder's Map_0..Map_9 art).
    public static bool PaintRoadTilemap(IEnumerable<EnemyMovement> routes, int roadWidth, out string message)
    {
        var grid = Object.FindAnyObjectByType<Grid>();
        if (grid == null)
        {
            message = "No se encontró ningún Grid en la escena.";
            return false;
        }

        var pathTm = grid.transform.Find("Path")?.GetComponent<Tilemap>();
        if (pathTm == null)
        {
            message = "No se encontró el Tilemap llamado 'Path' dentro de Grid.";
            return false;
        }

        Tile[] roadTiles = new Tile[10];
        for (int i = 0; i < 10; i++)
            roadTiles[i] = AssetDatabase.LoadAssetAtPath<Tile>($"Assets/Tiles/Map_{i}.asset");

        if (roadTiles[0] == null || roadTiles[1] == null || roadTiles[8] == null)
        {
            message = "No se encontraron los tiles de carretera (Assets/Tiles/Map_0..Map_9).";
            return false;
        }

        var roadCells = new HashSet<Vector3Int>();
        foreach (var route in routes)
        {
            if (route == null) continue;
            var cells = route.RouteCells();
            for (int i = 0; i < cells.Count; i++)
            {
                Vector3Int c = new Vector3Int(cells[i].x, cells[i].y, 0);
                roadCells.Add(c);
                if (roadWidth >= 2)
                {
                    bool verticalMove = i > 0 && cells[i].x == cells[i - 1].x && cells[i].y != cells[i - 1].y;
                    if (verticalMove)
                        roadCells.Add(new Vector3Int(c.x + 1, c.y, 0));
                    else
                        roadCells.Add(new Vector3Int(c.x, c.y + 1, 0));
                }
            }
        }

        if (roadCells.Count == 0)
        {
            message = "No hay celdas de ruta para pintar.";
            return false;
        }

        Vector3? doorWorld = FindBunkerDoorPosition();
        Vector3Int? doorCell = doorWorld.HasValue ? WorldToCell(doorWorld.Value) : (Vector3Int?)null;

        Undo.RegisterCompleteObjectUndo(pathTm, "Paint road tilemap");
        pathTm.ClearAllTiles();

        foreach (var p in roadCells)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    var np = new Vector3Int(p.x + dx, p.y + dy, 0);
                    if (roadCells.Contains(np) || pathTm.HasTile(np)) continue;

                    // Keep bunker entrance clear
                    if (doorCell.HasValue && Mathf.Abs(np.x - doorCell.Value.x) <= 1 && Mathf.Abs(np.y - doorCell.Value.y) <= 1)
                        continue;

                    bool hasU = roadCells.Contains(new Vector3Int(np.x, np.y + 1, 0));
                    bool hasD = roadCells.Contains(new Vector3Int(np.x, np.y - 1, 0));
                    bool hasL = roadCells.Contains(new Vector3Int(np.x - 1, np.y, 0));
                    bool hasR = roadCells.Contains(new Vector3Int(np.x + 1, np.y, 0));

                    if (hasU && hasR) pathTm.SetTile(np, roadTiles[7]);
                    else if (hasU && hasL) pathTm.SetTile(np, roadTiles[9]);
                    else if (hasD && hasR) pathTm.SetTile(np, roadTiles[0]);
                    else if (hasD && hasL) pathTm.SetTile(np, roadTiles[2]);
                    else if (hasD) pathTm.SetTile(np, roadTiles[1]);
                    else if (hasU) pathTm.SetTile(np, roadTiles[8]);
                    else if (hasR) pathTm.SetTile(np, roadTiles[4]);
                    else if (hasL) pathTm.SetTile(np, roadTiles[5]);
                    else
                    {
                        bool hasUR = roadCells.Contains(new Vector3Int(np.x + 1, np.y + 1, 0));
                        bool hasUL = roadCells.Contains(new Vector3Int(np.x - 1, np.y + 1, 0));
                        bool hasDR = roadCells.Contains(new Vector3Int(np.x + 1, np.y - 1, 0));
                        bool hasDL = roadCells.Contains(new Vector3Int(np.x - 1, np.y - 1, 0));

                        if (hasUR) pathTm.SetTile(np, roadTiles[7]);
                        else if (hasUL) pathTm.SetTile(np, roadTiles[9]);
                        else if (hasDR) pathTm.SetTile(np, roadTiles[0]);
                        else if (hasDL) pathTm.SetTile(np, roadTiles[2]);
                    }
                }
            }
        }

        pathTm.RefreshAllTiles();
        EditorUtility.SetDirty(pathTm);
        message = $"Carretera pintada en Tilemap 'Path' ({roadCells.Count} celdas).";
        return true;
    }
}
