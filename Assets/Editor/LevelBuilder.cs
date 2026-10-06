using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class LevelBuilder
{
    // =========================================================================
    // 1. TEMPLATE LEVEL BUILDER
    // =========================================================================
    [MenuItem("Bunker/1. Reset Template Level")]
    public static void BuildTemplateLevel()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Template_Level.unity", OpenSceneMode.Single);

        var grid = GameObject.Find("Grid");
        if (grid != null)
        {
            var pathTm = grid.transform.Find("Path")?.GetComponent<Tilemap>();
            if (pathTm != null) { pathTm.ClearAllTiles(); pathTm.RefreshAllTiles(); }

            var decoTm = grid.transform.Find("Decoration")?.GetComponent<Tilemap>();
            if (decoTm != null) { decoTm.ClearAllTiles(); decoTm.RefreshAllTiles(); }

            var limitsTm = grid.transform.Find("Limits")?.GetComponent<Tilemap>();
            if (limitsTm != null)
            {
                limitsTm.ClearAllTiles();
                var borderTile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tiles/Limits/asdsad_3.asset")
                              ?? AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tiles/Map_10.asset");
                if (borderTile != null)
                {
                    for (int x = 0; x <= 52; x++)
                    {
                        limitsTm.SetTile(new Vector3Int(x, 20, 0), borderTile);
                        limitsTm.SetTile(new Vector3Int(x, -16, 0), borderTile);
                    }
                    for (int y = -16; y <= 20; y++)
                    {
                        limitsTm.SetTile(new Vector3Int(0, y, 0), borderTile);
                        limitsTm.SetTile(new Vector3Int(52, y, 0), borderTile);
                    }
                }
                limitsTm.RefreshAllTiles();
            }
        }

        var wavesObj = GameObject.Find("Waves");
        if (wavesObj != null)
        {
            for (int i = wavesObj.transform.childCount - 1; i >= 0; i--)
            {
                var child = wavesObj.transform.GetChild(i);
                if (child.name != "WaveManager")
                    Object.DestroyImmediate(child.gameObject);
            }

            var waveObj = new GameObject("Wave");
            waveObj.transform.SetParent(wavesObj.transform, false);

            var spawnerObj = new GameObject("SpawnerEnemy");
            spawnerObj.transform.SetParent(waveObj.transform, false);
            spawnerObj.transform.position = new Vector3(48f, -0.5f, 0f);
            var spawner = spawnerObj.AddComponent<EnemySpawner>();

            var pathObj = new GameObject("Path");
            pathObj.transform.SetParent(waveObj.transform, false);
            pathObj.transform.position = Vector3.zero;
            var path = pathObj.AddComponent<EnemyMovement>();
            path.pathColor = Color.cyan;
            path.points = new Vector3[]
            {
                new Vector3(48f, -0.5f, 0f),
                new Vector3(6f, -0.5f, 0f)
            };
            spawner.path = path;
        }

        var enemyBase = GameObject.Find("EnemyBase") ?? GameObject.Find("EnemyBase_North");
        if (enemyBase != null)
        {
            enemyBase.name = "EnemyBase";
            enemyBase.transform.position = new Vector3(48f, -0.5f, 0f);
        }
        var enemyPT = GameObject.Find("EnemyPT") ?? GameObject.Find("EnemyPT_North");
        if (enemyPT != null)
        {
            enemyPT.name = "EnemyPT";
            enemyPT.transform.position = new Vector3(48f, -0.5f, 0f);
        }

        var extraBase = GameObject.Find("EnemyBase_South");
        if (extraBase != null) Object.DestroyImmediate(extraBase);
        var extraPT = GameObject.Find("EnemyPT_South");
        if (extraPT != null) Object.DestroyImmediate(extraPT);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[LevelBuilder] Template_Level.unity reset successfully.");
    }

    // =========================================================================
    // 2. AUTHENTIC LEVEL 04 DOUBLE FRONT BUILDER
    // =========================================================================
    [MenuItem("Bunker/2. Generate Authentic Level 04 Double Front")]
    public static void BuildLevel04DoubleFront()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Level04_DoubleFront.unity", OpenSceneMode.Single);

        var grid = GameObject.Find("Grid");
        if (grid == null)
        {
            Debug.LogError("[LevelBuilder] Grid GameObject not found!");
            return;
        }

        var pathTm = grid.transform.Find("Path")?.GetComponent<Tilemap>();
        var decoTm = grid.transform.Find("Decoration")?.GetComponent<Tilemap>();
        var limitsTm = grid.transform.Find("Limits")?.GetComponent<Tilemap>();

        // STEP 1: LOAD ALL AUTHENTIC BUNKER TILES
        var voidTile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tiles/image 402@2x.asset");
        var topCliffTile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tiles/Limits/image 455.asset");
        var cliffDropTile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tiles/Limits/asdsad_0.asset");
        var bottomCliffTile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tiles/Limits/image 450.asset");
        var leftCliffTile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tiles/Limits/asdsad_1.asset");
        var rightCliffTile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tiles/Limits/image 452.asset");
        var cornerTL = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tiles/Limits/image 456.asset");
        var cornerTR = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tiles/image 471.asset");
        var cornerBL = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tiles/Limits/asdsad_2.asset");
        var cornerBR = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tiles/Limits/image 453.asset");

        Tile[] roadTiles = new Tile[11];
        for (int i = 0; i <= 10; i++)
            roadTiles[i] = AssetDatabase.LoadAssetAtPath<Tile>($"Assets/Tiles/Map_{i}.asset");

        // 9-Slice Forest Canopy Tiles
        var treeTL = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tiles/image 399.asset");
        var treeTop = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tiles/image 400@2x.asset");
        var treeTR = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tiles/image 401@2x.asset");
        var treeLeft = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tiles/image 403@2x.asset");
        var treeCenter = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tiles/image 402@2x.asset");
        var treeRight = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tiles/image 404@2x.asset");
        var treeBL = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tiles/image 407.asset");
        var treeBottom = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tiles/image 405@2x.asset");
        var treeBR = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tiles/image 406@2x.asset");

        // STEP 2: CLEAR OLD TILEMAPS
        if (pathTm != null) pathTm.ClearAllTiles();
        if (decoTm != null) decoTm.ClearAllTiles();
        if (limitsTm != null) limitsTm.ClearAllTiles();

        // STEP 3: CARVE AUTHENTIC ISLAND ON LIMITS
        // Define Island mask
        bool IsIsland(int x, int y)
        {
            // West Peninsula (Bunker defensive zone)
            if (x >= 2 && x <= 16 && y >= -6 && y <= 6) return true;
            // Central Plateau & Connecting Corridor
            if (x >= 15 && x <= 40 && y >= -4 && y <= 4) return true;
            // North Pass Corridor
            if (x >= 15 && x <= 50 && y >= 2 && y <= 16)
            {
                // Eastern ocean inlet between the two gates
                if (x >= 42 && y <= 6) return false;
                return true;
            }
            // South Pass Corridor
            if (x >= 15 && x <= 50 && y >= -13 && y <= 0)
            {
                // Eastern ocean inlet between the two gates
                if (x >= 42 && y >= -4) return false;
                return true;
            }
            return false;
        }

        // Paint Limits across full camera movement view
        int minX = -8, maxX = 60, minY = -18, maxY = 26;
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                if (IsIsland(x, y)) continue; // Playable ground is empty on Limits

                var p = new Vector3Int(x, y, 0);
                // Check if this cell is on the perimeter of the island
                bool landBelow = IsIsland(x, y - 1);
                bool landBelow2 = IsIsland(x, y - 2);
                bool landAbove = IsIsland(x, y + 1);
                bool landLeft = IsIsland(x - 1, y);
                bool landRight = IsIsland(x + 1, y);

                if (landBelow)
                {
                    // Top cliff ledge
                    limitsTm.SetTile(p, topCliffTile);
                }
                else if (landBelow2 && y <= 18)
                {
                    // Secondary rock ledge above top cliff
                    limitsTm.SetTile(p, topCliffTile ?? voidTile);
                }
                else if (landAbove)
                {
                    // Bottom cliff rim
                    limitsTm.SetTile(p, bottomCliffTile);
                }
                else if (landRight && !landAbove && !landBelow)
                {
                    // Left vertical cliff wall
                    limitsTm.SetTile(p, leftCliffTile);
                }
                else if (landLeft && !landAbove && !landBelow)
                {
                    // Right vertical cliff wall
                    limitsTm.SetTile(p, rightCliffTile);
                }
                else
                {
                    // Deep ocean / dark void
                    limitsTm.SetTile(p, voidTile);
                }
            }
        }

        // STEP 4: AUTHENTIC HOLLOW ROADS ON PATH TILEMAP
        // We define the walkable road cells (width = 2 empty cells)
        var roadCells = new HashSet<Vector3Int>();

        void AddRoadH(int x1, int x2, int y)
        {
            int startX = Mathf.Min(x1, x2);
            int endX = Mathf.Max(x1, x2);
            for (int x = startX; x <= endX; x++)
            {
                roadCells.Add(new Vector3Int(x, y, 0));
                roadCells.Add(new Vector3Int(x, y + 1, 0));
            }
        }

        void AddRoadV(int x, int y1, int y2)
        {
            int startY = Mathf.Min(y1, y2);
            int endY = Mathf.Max(y1, y2);
            for (int y = startY; y <= endY; y++)
            {
                roadCells.Add(new Vector3Int(x, y, 0));
                roadCells.Add(new Vector3Int(x + 1, y, 0));
            }
        }

        // North Route Road Geometry:
        // Gate (48, 14) -> West to (38, 14) -> South to (38, 9) -> West to (24, 9) -> South to (24, 3) -> West to (16, 3) -> South to (16, 0)
        AddRoadH(38, 48, 14);
        AddRoadV(38, 9, 14);
        AddRoadH(24, 38, 9);
        AddRoadV(24, 3, 9);
        AddRoadH(16, 24, 3);
        AddRoadV(16, 0, 3);

        // South Route Road Geometry:
        // Gate (48, -11) -> West to (36, -11) -> North to (36, -6) -> West to (26, -6) -> South to (26, -10) -> West to (16, -10) -> North to (16, 0)
        AddRoadH(36, 48, -11);
        AddRoadV(36, -11, -6);
        AddRoadH(26, 36, -6);
        AddRoadV(26, -10, -6);
        AddRoadH(16, 26, -10);
        AddRoadV(16, -10, 0);

        // Common Final Avenue into the Bunker:
        // From (16, 0) straight West to Bunker entrance at (7, 0)
        AddRoadH(7, 16, 0);

        // Now draw ONLY the outline border tiles on Path tilemap! (Center is left completely EMPTY)
        foreach (var p in roadCells)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    var np = new Vector3Int(p.x + dx, p.y + dy, 0);
                    if (roadCells.Contains(np) || pathTm.HasTile(np)) continue;

                    // Keep Bunker door open: don't place vertical wall blocking the entrance at x <= 6, y = 0..1
                    if (np.x <= 6 && (np.y == 0 || np.y == 1)) continue;

                    bool hasU = roadCells.Contains(new Vector3Int(np.x, np.y + 1, 0));
                    bool hasD = roadCells.Contains(new Vector3Int(np.x, np.y - 1, 0));
                    bool hasL = roadCells.Contains(new Vector3Int(np.x - 1, np.y, 0));
                    bool hasR = roadCells.Contains(new Vector3Int(np.x + 1, np.y, 0));

                    // Corner analysis
                    if (hasU && hasR) pathTm.SetTile(np, roadTiles[7]);      // Bottom-Left Outer Corner
                    else if (hasU && hasL) pathTm.SetTile(np, roadTiles[9]); // Bottom-Right Outer Corner
                    else if (hasD && hasR) pathTm.SetTile(np, roadTiles[0]); // Top-Left Outer Corner
                    else if (hasD && hasL) pathTm.SetTile(np, roadTiles[2]); // Top-Right Outer Corner
                    else if (hasD) pathTm.SetTile(np, roadTiles[1]);         // Top Border
                    else if (hasU) pathTm.SetTile(np, roadTiles[8]);         // Bottom Border
                    else if (hasR) pathTm.SetTile(np, roadTiles[4]);         // Left Border
                    else if (hasL) pathTm.SetTile(np, roadTiles[5]);         // Right Border
                    else
                    {
                        // Diagonal corners
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

        // STEP 5: AUTHENTIC DECORATION (9-SLICE FOREST GROVES & NATURAL SPECKLES)
        void DrawForest(int startX, int startY, int width, int height)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int wx = startX + x;
                    int wy = startY + y;
                    var p = new Vector3Int(wx, wy, 0);

                    // Don't overlap roads, road borders, or void
                    if (roadCells.Contains(p) || pathTm.HasTile(p) || !IsIsland(wx, wy)) continue;

                    Tile t;
                    if (y == height - 1 && x == 0) t = treeTL;
                    else if (y == height - 1 && x == width - 1) t = treeTR;
                    else if (y == height - 1) t = treeTop;
                    else if (y == 0 && x == 0) t = treeBL;
                    else if (y == 0 && x == width - 1) t = treeBR;
                    else if (y == 0) t = treeBottom;
                    else if (x == 0) t = treeLeft;
                    else if (x == width - 1) t = treeRight;
                    else t = treeCenter;

                    decoTm.SetTile(p, t);
                }
            }
        }

        // Place 5 authentic, organic forest groves
        DrawForest(7, 3, 5, 3);    // North-West woods
        DrawForest(7, -5, 5, 3);   // South-West woods
        DrawForest(28, -2, 5, 4);  // Central Divide grove
        DrawForest(42, 9, 5, 3);   // North-East forest
        DrawForest(38, -9, 5, 3);  // South-East woods

        // Natural grass & pebble speckles
        int[] speckleX = { 9, 10, 14, 15, 20, 21, 22, 23, 27, 33, 35, 37, 41, 46, 47 };
        int[] speckleY = { 2, -2, 5, -5, 6, -4, 7, -8, 2, -1, 5, -7, 8, 13, -12 };
        for (int i = 0; i < speckleX.Length; i++)
        {
            var sp = new Vector3Int(speckleX[i], speckleY[i], 0);
            if (!roadCells.Contains(sp) && !pathTm.HasTile(sp) && !decoTm.HasTile(sp) && IsIsland(sp.x, sp.y))
            {
                Tile detailTile = (i % 3 == 0) ? roadTiles[3] : (i % 3 == 1) ? roadTiles[6] : roadTiles[10];
                decoTm.SetTile(sp, detailTile);
            }
        }

        // STEP 6: REFRESH ALL TILEMAPS & COLLIDERS
        limitsTm.RefreshAllTiles();
        pathTm.RefreshAllTiles();
        decoTm.RefreshAllTiles();

        // STEP 7: SETUP MANAGERS, BASES, AND ENEMY PATHS
        var bunker = GameObject.Find("PrincipalTurrent");
        if (bunker != null)
        {
            bunker.transform.position = new Vector3(5f, 0.5f, 0f);
        }

        // Waves & Double Front Spawners
        var wavesObj = GameObject.Find("Waves");
        if (wavesObj != null)
        {
            // North Front
            Transform northFront = wavesObj.transform.Find("NorthFront");
            if (northFront == null)
            {
                var go = new GameObject("NorthFront");
                go.transform.SetParent(wavesObj.transform, false);
                northFront = go.transform;
            }

            Transform spawnerN = northFront.Find("SpawnerEnemy_North");
            if (spawnerN == null)
            {
                var sGo = new GameObject("SpawnerEnemy_North");
                sGo.transform.SetParent(northFront, false);
                spawnerN = sGo.transform;
            }
            spawnerN.position = new Vector3(48.5f, 14.5f, 0f);
            var spawnerCompN = spawnerN.GetComponent<EnemySpawner>() ?? spawnerN.gameObject.AddComponent<EnemySpawner>();

            Transform pathN = northFront.Find("Path_North");
            if (pathN == null)
            {
                var pGo = new GameObject("Path_North");
                pGo.transform.SetParent(northFront, false);
                pathN = pGo.transform;
            }
            pathN.position = Vector3.zero;
            var emN = pathN.GetComponent<EnemyMovement>() ?? pathN.gameObject.AddComponent<EnemyMovement>();
            emN.pathColor = Color.cyan;
            emN.points = new Vector3[]
            {
                new Vector3(48.5f, 14.5f, 0f),
                new Vector3(38.5f, 14.5f, 0f),
                new Vector3(38.5f, 9.5f, 0f),
                new Vector3(24.5f, 9.5f, 0f),
                new Vector3(24.5f, 3.5f, 0f),
                new Vector3(16.5f, 3.5f, 0f),
                new Vector3(16.5f, 0.5f, 0f),
                new Vector3(6.5f, 0.5f, 0f)
            };
            spawnerCompN.path = emN;

            // South Front
            Transform southFront = wavesObj.transform.Find("SouthFront");
            if (southFront == null)
            {
                var go = new GameObject("SouthFront");
                go.transform.SetParent(wavesObj.transform, false);
                southFront = go.transform;
            }

            Transform spawnerS = southFront.Find("SpawnerEnemy_South");
            if (spawnerS == null)
            {
                var sGo = new GameObject("SpawnerEnemy_South");
                sGo.transform.SetParent(southFront, false);
                spawnerS = sGo.transform;
            }
            spawnerS.position = new Vector3(48.5f, -10.5f, 0f);
            var spawnerCompS = spawnerS.GetComponent<EnemySpawner>() ?? spawnerS.gameObject.AddComponent<EnemySpawner>();

            Transform pathS = southFront.Find("Path_South");
            if (pathS == null)
            {
                var pGo = new GameObject("Path_South");
                pGo.transform.SetParent(southFront, false);
                pathS = pGo.transform;
            }
            pathS.position = Vector3.zero;
            var emS = pathS.GetComponent<EnemyMovement>() ?? pathS.gameObject.AddComponent<EnemyMovement>();
            emS.pathColor = new Color(1f, 0.5f, 0.1f, 1f); // Orange
            emS.points = new Vector3[]
            {
                new Vector3(48.5f, -10.5f, 0f),
                new Vector3(36.5f, -10.5f, 0f),
                new Vector3(36.5f, -5.5f, 0f),
                new Vector3(26.5f, -5.5f, 0f),
                new Vector3(26.5f, -9.5f, 0f),
                new Vector3(16.5f, -9.5f, 0f),
                new Vector3(16.5f, 0.5f, 0f),
                new Vector3(6.5f, 0.5f, 0f)
            };
            spawnerCompS.path = emS;
        }

        // Enemy Bases
        var baseN = GameObject.Find("EnemyBase_North") ?? GameObject.Find("EnemyBase");
        if (baseN != null)
        {
            baseN.name = "EnemyBase_North";
            baseN.transform.position = new Vector3(48.5f, 14.5f, 0f);
        }

        var baseS = GameObject.Find("EnemyBase_South");
        if (baseS == null && baseN != null)
        {
            baseS = Object.Instantiate(baseN, new Vector3(48.5f, -10.5f, 0f), Quaternion.identity);
            baseS.name = "EnemyBase_South";
        }
        else if (baseS != null)
        {
            baseS.transform.position = new Vector3(48.5f, -10.5f, 0f);
        }

        // Camera movement bounds
        var cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(26f, 1f, -10f);
            var cm = cam.GetComponent<CameraMovement>();
            if (cm != null)
            {
                cm.minX = 6f;
                cm.maxX = 48f;
                cm.minY = -8f;
                cm.maxY = 16f;
                cm.speed = 8f;
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[LevelBuilder] Authentic Level 04 Double Front successfully generated and saved!");
    }
}
