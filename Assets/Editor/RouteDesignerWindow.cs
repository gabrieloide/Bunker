using System.Collections.Generic;
using Bunker.Simulation;
using UnityEditor;
using UnityEngine;

// Visual, designer-friendly editor for painting, splitting, merging, and managing all enemy/ally routes in a scene.
public class RouteDesignerWindow : EditorWindow
{
    enum ToolMode
    {
        EditPoints,
        PaintBrush,
        SplitAtFlag,
        MergeTarget
    }

    const float FlagSize = 0.36f;
    const float PlusSize = 0.18f;

    public static bool IsOpen => instance != null;
    static RouteDesignerWindow instance;

    EnemyMovement activeRoute;
    ToolMode mode = ToolMode.EditPoints;
    int tilemapRoadWidth = 2;
    bool brushStartedStroke;
    Vector3Int lastBrushCell;
    Vector2 scrollPos;
    string statusMessage;
    MessageType statusType = MessageType.None;

    [MenuItem("Bunker/Editor Visual de Rutas")]
    public static void Open()
    {
        var win = GetWindow<RouteDesignerWindow>("Editor de Rutas");
        win.minSize = new Vector2(330f, 460f);
        win.Show();
    }

    public static void OpenWithRoute(EnemyMovement route)
    {
        Open();
        if (instance != null && route != null)
        {
            instance.activeRoute = route;
            instance.Repaint();
            SceneView.RepaintAll();
        }
    }

    void OnEnable()
    {
        instance = this;
        SceneView.duringSceneGui += OnSceneGUI;
        Undo.undoRedoPerformed += OnUndoRedo;
        EnsureValidSelection();
    }

    void OnDisable()
    {
        if (instance == this) instance = null;
        SceneView.duringSceneGui -= OnSceneGUI;
        Undo.undoRedoPerformed -= OnUndoRedo;
        SceneView.RepaintAll();
    }

    void OnUndoRedo()
    {
        EnsureValidSelection();
        Repaint();
        SceneView.RepaintAll();
    }

    void EnsureValidSelection()
    {
        if (activeRoute != null) return;
        if (Selection.activeGameObject != null && Selection.activeGameObject.TryGetComponent<EnemyMovement>(out var selected))
        {
            activeRoute = selected;
            return;
        }
        var spawner = FindAnyObjectByType<EnemySpawner>();
        if (spawner != null && spawner.path != null)
        {
            activeRoute = spawner.path;
            return;
        }
        activeRoute = FindAnyObjectByType<EnemyMovement>();
    }

    void OnGUI()
    {
        EnsureValidSelection();

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Editor Visual de Rutas", EditorStyles.boldLabel);

        // Mode toolbar
        EditorGUILayout.BeginHorizontal();
        DrawModeButton("Editar Puntos", ToolMode.EditPoints);
        DrawModeButton("Pintar Camino", ToolMode.PaintBrush);
        DrawModeButton("Dividir en 2", ToolMode.SplitAtFlag);
        DrawModeButton("Unir Final", ToolMode.MergeTarget);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);
        DrawModeHelp();

        EditorGUILayout.Space(6);
        DrawSceneSetupActions();

        EditorGUILayout.Space(6);
        DrawActiveRoutePanel();

        EditorGUILayout.Space(6);
        DrawRouteList();

        EditorGUILayout.Space(6);
        DrawTilemapSection();

        if (!string.IsNullOrEmpty(statusMessage))
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(statusMessage, statusType);
        }
    }

    void DrawModeButton(string label, ToolMode targetMode)
    {
        bool current = mode == targetMode;
        Color prev = GUI.backgroundColor;
        if (current) GUI.backgroundColor = new Color(0.45f, 0.85f, 1f, 1f);
        if (GUILayout.Button(label, GUILayout.Height(28)))
        {
            mode = targetMode;
            brushStartedStroke = false;
            SceneView.RepaintAll();
        }
        GUI.backgroundColor = prev;
    }

    void DrawModeHelp()
    {
        string help = mode switch
        {
            ToolMode.EditPoints => "Arrastra banderas para moverlas. Shift + clic agrega al final. Clic en + inserta esquina. Ctrl + clic borra bandera. Clic en otro camino para seleccionarlo.",
            ToolMode.PaintBrush => "Haz clic y arrastra sobre el mapa en la Scene View para pintar el camino casilla a casilla. Las esquinas en L se crean solas.",
            ToolMode.SplitAtFlag => "Haz clic sobre cualquier bandera del camino seleccionado en la Scene View para dividirlo en 2 ramas desde ese punto.",
            ToolMode.MergeTarget => "Haz clic sobre la bandera de otro camino en la Scene View donde quieres volver a unir el final de este tramo.",
            _ => ""
        };
        EditorGUILayout.HelpBox(help, MessageType.Info);
    }

    void DrawSceneSetupActions()
    {
        EditorGUILayout.LabelField("Frentes y Escena", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("+ Nuevo Camino y Spawner", GUILayout.Height(26)))
            {
                Vector3 pos = ScenePivotOrBunkerOffset();
                var created = RouteAuthoringUtility.CreateNewSpawnerAndRoute(pos);
                if (created != null)
                {
                    SelectRoute(created);
                    mode = ToolMode.PaintBrush;
                    SetStatus($"Creado '{created.name}'. Pinta su recorrido en la Scene View.", MessageType.Info);
                }
            }

            if (GUILayout.Button("+ Base Enemiga", GUILayout.Height(26), GUILayout.Width(115)))
            {
                Vector3 pos = ScenePivotOrBunkerOffset();
                var createdBase = RouteAuthoringUtility.CreateAdditionalEnemyBase(pos);
                if (createdBase != null)
                {
                    Selection.activeGameObject = createdBase.gameObject;
                    SetStatus($"Base '{createdBase.name}' creada. Muevela al inicio del nuevo camino.", MessageType.Info);
                }
            }
        }
    }

    void DrawActiveRoutePanel()
    {
        EditorGUILayout.LabelField("Camino Seleccionado", EditorStyles.boldLabel);
        if (activeRoute == null)
        {
            EditorGUILayout.HelpBox("No hay ningun camino en la escena. Pulsa '+ Nuevo Camino y Spawner'.", MessageType.Warning);
            return;
        }

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(activeRoute.name, EditorStyles.boldLabel);
                if (GUILayout.Button("Ver en Escena", GUILayout.Width(95)))
                    FrameRoute(activeRoute);
                if (GUILayout.Button("Inspector", GUILayout.Width(72)))
                    Selection.activeGameObject = activeRoute.gameObject;
            }

            RouteAuthoringUtility.Measure(activeRoute, out int length, out int blocked);
            var infoStyle = new GUIStyle(EditorStyles.label);
            if (blocked > 0) infoStyle.normal.textColor = RouteAuthoringUtility.BlockedColor;
            string branchesInfo = activeRoute.Forks ? $"   Se divide en {activeRoute.branches.Length} ramas" : "   Llega al bunker";
            EditorGUILayout.LabelField($"Longitud: {length} celdas" + (blocked > 0 ? $"   ({blocked} bloqueadas)" : "") + branchesInfo, infoStyle);

            if (activeRoute.transform.position != Vector3.zero)
            {
                EditorGUILayout.HelpBox("Posicion del objeto distinta de (0,0,0).", MessageType.Warning);
                if (GUILayout.Button("Centrar en (0, 0, 0)"))
                {
                    Undo.RecordObject(activeRoute.transform, "Reset route transform");
                    activeRoute.transform.position = Vector3.zero;
                }
            }

            EditorGUILayout.Space(3);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Re-pintar desde inicio"))
                {
                    RouteAuthoringUtility.Edit(activeRoute, "Reset route points", points =>
                    {
                        var spawner = RouteAuthoringUtility.FindDirectSpawner(activeRoute);
                        Vector3 start = spawner != null
                            ? RouteAuthoringUtility.Snap(new Vector3(spawner.transform.position.x, spawner.transform.position.y, 0f))
                            : (points.Count > 0 ? points[0] : RouteAuthoringUtility.Snap(ScenePivotOrBunkerOffset()));
                        points.Clear();
                        points.Add(start);
                    });
                    mode = ToolMode.PaintBrush;
                    SetStatus("Modo pincel activo: arrastra desde el inicio hacia el bunker.", MessageType.Info);
                }

                if (GUILayout.Button("Conectar Spawner / Bunker"))
                {
                    RouteAuthoringUtility.Edit(activeRoute, "Snap route ends", points => RouteAuthoringUtility.SnapEnds(activeRoute, points));
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Invertir sentido"))
                {
                    RouteAuthoringUtility.Edit(activeRoute, "Reverse route", points => points.Reverse());
                }

                if (GUILayout.Button("Dividir en 2 aqui"))
                {
                    int splitIndex = activeRoute.points != null && activeRoute.points.Length > 2
                        ? activeRoute.points.Length / 2
                        : (activeRoute.points != null ? Mathf.Max(0, activeRoute.points.Length - 1) : 0);
                    var secondBranch = RouteAuthoringUtility.SplitRouteAt(activeRoute, splitIndex);
                    if (secondBranch != null)
                    {
                        SelectRoute(secondBranch);
                        mode = ToolMode.PaintBrush;
                        SetStatus($"Camino dividido. Ahora edita o pinta '{secondBranch.name}'.", MessageType.Info);
                    }
                }

                Color prev = GUI.backgroundColor;
                GUI.backgroundColor = new Color(1f, 0.55f, 0.55f, 1f);
                if (GUILayout.Button("Borrar Ruta", GUILayout.Width(85)))
                {
                    if (EditorUtility.DisplayDialog("Borrar camino", $"Seguro que quieres borrar '{activeRoute.name}'?", "Borrar", "Cancelar"))
                    {
                        var toDelete = activeRoute;
                        activeRoute = null;
                        RouteAuthoringUtility.DeleteRoute(toDelete);
                        EnsureValidSelection();
                        SetStatus("Ruta eliminada.", MessageType.Info);
                        GUI.backgroundColor = prev;
                        return;
                    }
                }
                GUI.backgroundColor = prev;
            }
        }
    }

    void DrawRouteList()
    {
        var allRoutes = FindObjectsByType<EnemyMovement>(FindObjectsSortMode.None);
        var spawners = FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None);

        EditorGUILayout.LabelField($"Caminos en Escena ({allRoutes.Length})", EditorStyles.boldLabel);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.MinHeight(120), GUILayout.MaxHeight(220));
        var drawn = new HashSet<EnemyMovement>();

        // Draw trees starting from spawner roots first
        foreach (var spawner in spawners)
        {
            if (spawner == null || spawner.path == null) continue;
            DrawRouteTreeRow(spawner.path, 0, drawn, $"[{spawner.name}] ");
        }

        // Then any orphan routes not reached from a spawner
        foreach (var route in allRoutes)
        {
            if (route == null || drawn.Contains(route)) continue;
            if (!RouteAuthoringUtility.HasAnyParent(route))
                DrawRouteTreeRow(route, 0, drawn, "");
        }

        // Finally any remaining cycles/disconnected branches
        foreach (var route in allRoutes)
        {
            if (route == null || drawn.Contains(route)) continue;
            DrawRouteTreeRow(route, 0, drawn, "");
        }

        EditorGUILayout.EndScrollView();
    }

    void DrawRouteTreeRow(EnemyMovement route, int depth, HashSet<EnemyMovement> visited, string prefix)
    {
        if (route == null || !visited.Add(route)) return;

        RouteAuthoringUtility.Measure(route, out int length, out int blocked);
        bool selected = route == activeRoute;

        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            GUILayout.Space(depth * 14f);

            Rect swatch = GUILayoutUtility.GetRect(12f, 18f, GUILayout.Width(12f));
            EditorGUI.DrawRect(swatch, route.pathColor);

            string title = prefix + route.name;
            if (route.Forks) title += $" (divide en {route.branches.Length})";
            if (blocked > 0) title += $" [{blocked} bloq]";

            GUIStyle btnStyle = new GUIStyle(EditorStyles.label);
            btnStyle.fontStyle = selected ? FontStyle.Bold : FontStyle.Normal;
            if (blocked > 0) btnStyle.normal.textColor = RouteAuthoringUtility.BlockedColor;

            if (GUILayout.Button(title, btnStyle))
                SelectRoute(route);

            GUILayout.FlexibleSpace();
            GUILayout.Label($"{length}c", EditorStyles.miniLabel, GUILayout.Width(32));

            if (GUILayout.Button(selected ? "Activo" : "Elegir", GUILayout.Width(54)))
                SelectRoute(route);
        }

        if (route.Forks)
        {
            foreach (var branch in route.branches)
            {
                if (branch != null)
                    DrawRouteTreeRow(branch, depth + 1, visited, "");
            }
        }
    }

    void DrawTilemapSection()
    {
        EditorGUILayout.LabelField("Arte en Tilemap (Opcional)", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            tilemapRoadWidth = EditorGUILayout.IntPopup("Ancho de carretera", tilemapRoadWidth, new[] { "1 celda", "2 celdas (clasico)" }, new[] { 1, 2 });
            if (GUILayout.Button("Pintar Carretera en Tilemap 'Path'", GUILayout.Height(26)))
            {
                var allRoutes = FindObjectsByType<EnemyMovement>(FindObjectsSortMode.None);
                bool ok = RouteAuthoringUtility.PaintRoadTilemap(allRoutes, tilemapRoadWidth, out string msg);
                SetStatus(msg, ok ? MessageType.Info : MessageType.Warning);
            }
        }
    }

    void OnSceneGUI(SceneView sceneView)
    {
        var allRoutes = FindObjectsByType<EnemyMovement>(FindObjectsSortMode.None);
        if (allRoutes == null || allRoutes.Length == 0) return;

        EnsureValidSelection();
        Event e = Event.current;

        // Draw inactive routes dimmed, and allow clicking them to switch selection
        foreach (var route in allRoutes)
        {
            if (route == null || route == activeRoute) continue;
            RouteAuthoringUtility.DrawRouteLegs(route, route.pathColor, 3.5f, true);
            DrawInactiveRoutePoints(route, e);
        }

        if (activeRoute == null) return;

        RouteAuthoringUtility.DrawRouteLegs(activeRoute, RouteAuthoringUtility.DefaultActiveColor, 5.5f, false);

        switch (mode)
        {
            case ToolMode.EditPoints:
                HandleEditPointsScene(activeRoute, e);
                break;
            case ToolMode.PaintBrush:
                HandlePaintBrushScene(activeRoute, e);
                break;
            case ToolMode.SplitAtFlag:
                HandleSplitScene(activeRoute, e);
                break;
            case ToolMode.MergeTarget:
                HandleMergeScene(activeRoute, allRoutes, e);
                break;
        }
    }

    void DrawInactiveRoutePoints(EnemyMovement route, Event e)
    {
        if (route.points == null) return;
        for (int i = 0; i < route.points.Length; i++)
        {
            Handles.color = new Color(route.pathColor.r, route.pathColor.g, route.pathColor.b, 0.55f);
            if (mode != ToolMode.MergeTarget && mode != ToolMode.PaintBrush)
            {
                if (Handles.Button(route.points[i], Quaternion.identity, FlagSize * 0.65f, FlagSize * 0.8f, Handles.SphereHandleCap))
                {
                    SelectRoute(route);
                    e.Use();
                    return;
                }
            }
            else
            {
                Handles.SphereHandleCap(0, route.points[i], Quaternion.identity, FlagSize * 0.55f, EventType.Repaint);
            }
        }
    }

    void HandleEditPointsScene(EnemyMovement route, Event e)
    {
        var points = route.points;
        if (points == null) return;

        // "+" on the first leg between two flags: inserts a flag there
        for (int i = 1; i < points.Length; i++)
        {
            Vector3 legEnd = RouteGeometry.TryGetCorner(points[i - 1], points[i], route.verticalFirst, out var corner) ? corner : points[i];
            Vector3 mid = (points[i - 1] + legEnd) * 0.5f;
            Handles.color = Color.white;
            if (Handles.Button(mid, Quaternion.identity, PlusSize, PlusSize * 1.5f, Handles.DotHandleCap))
            {
                int at = i;
                Vector3 inserted = RouteAuthoringUtility.Snap(mid);
                RouteAuthoringUtility.Edit(route, "Insert route flag", list => list.Insert(at, inserted));
                return;
            }
        }

        // Flags: drag to move, Ctrl+click to delete
        for (int i = 0; i < points.Length; i++)
        {
            Handles.color = i == 0 ? RouteAuthoringUtility.StartColor
                : (i == points.Length - 1 && !route.Forks) ? RouteAuthoringUtility.EndColor
                : (i == points.Length - 1 && route.Forks) ? RouteAuthoringUtility.ForkColor
                : RouteAuthoringUtility.DefaultActiveColor;

            if (e.control)
            {
                if (Handles.Button(points[i], Quaternion.identity, FlagSize, FlagSize, Handles.SphereHandleCap))
                {
                    int at = i;
                    RouteAuthoringUtility.Edit(route, "Delete route flag", list => list.RemoveAt(at));
                    return;
                }
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                Vector3 moved = Handles.FreeMoveHandle(points[i], FlagSize, Vector3.zero, Handles.SphereHandleCap);
                if (EditorGUI.EndChangeCheck())
                {
                    int at = i;
                    Vector3 snapped = RouteAuthoringUtility.Snap(new Vector3(moved.x, moved.y, points[i].z));
                    if (snapped != points[i])
                    {
                        RouteAuthoringUtility.Edit(route, "Move route flag", list => list[at] = snapped);
                        KeepSpawnerSynced(route, at, snapped);
                    }
                }
            }

            string tag = i == 0 ? "INICIO" : (i == points.Length - 1 ? (route.Forks ? "DIVIDE" : "FIN") : i.ToString());
            Handles.Label(points[i] + new Vector3(0.3f, 0.55f, 0f), tag, EditorStyles.boldLabel);
        }

        // Shift+click on the map: add flag at end
        if (e.type == EventType.MouseDown && e.button == 0 && e.shift && !e.alt && !e.control)
        {
            Vector3 world = MouseWorld(e, points.Length > 0 ? points[points.Length - 1].z : 0f);
            Vector3 added = RouteAuthoringUtility.Snap(world);
            RouteAuthoringUtility.Edit(route, "Add route flag", list => list.Add(added));
            e.Use();
        }
    }

    void HandlePaintBrushScene(EnemyMovement route, Event e)
    {
        if (e.alt) return;

        // Right-click or Escape exits brush mode
        if ((e.type == EventType.MouseDown && e.button == 1) || (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape))
        {
            mode = ToolMode.EditPoints;
            brushStartedStroke = false;
            Repaint();
            e.Use();
            return;
        }

        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        float z = route.points != null && route.points.Length > 0 ? route.points[0].z : 0f;
        Vector3 world = MouseWorld(e, z);
        Vector3Int cell = RouteAuthoringUtility.WorldToCell(world);
        Vector3 center = RouteAuthoringUtility.CellCenter(cell, z);

        // Draw brush cell preview
        bool blocked = PlacementGrid.Available && PlacementGrid.BlocksWalking(cell);
        Vector2 cellSize = PlacementGrid.Available ? PlacementGrid.CellSize : Vector2.one;
        Handles.color = blocked ? RouteAuthoringUtility.BlockedColor : RouteAuthoringUtility.StartColor;
        Handles.DrawWireCube(center, new Vector3(cellSize.x * 0.92f, cellSize.y * 0.92f, 0.05f));
        SceneView.RepaintAll();

        if (e.type == EventType.MouseDown && e.button == 0)
        {
            Undo.RecordObject(route, "Paint route");
            if (route.transform.position != Vector3.zero)
            {
                Undo.RecordObject(route.transform, "Paint route");
                route.transform.position = Vector3.zero;
            }
            brushStartedStroke = true;
            lastBrushCell = cell;

            var list = new List<Vector3>(route.points ?? new Vector3[0]);
            AppendBrushCell(list, cell, z);
            route.points = list.ToArray();
            EditorUtility.SetDirty(route);
            e.Use();
            Repaint();
        }
        else if (e.type == EventType.MouseDrag && e.button == 0 && brushStartedStroke)
        {
            if (cell != lastBrushCell)
            {
                lastBrushCell = cell;
                var list = new List<Vector3>(route.points ?? new Vector3[0]);
                AppendBrushCell(list, cell, z);
                route.points = list.ToArray();
                EditorUtility.SetDirty(route);
                Repaint();
            }
            e.Use();
        }
        else if (e.type == EventType.MouseUp && e.button == 0 && brushStartedStroke)
        {
            brushStartedStroke = false;
            e.Use();
        }
    }

    // Extends `points` cleanly toward `targetCell` using minimal orthogonal corners (no redundant points along straight lines).
    static void AppendBrushCell(List<Vector3> points, Vector3Int targetCell, float z)
    {
        Vector3 targetPos = RouteAuthoringUtility.CellCenter(targetCell, z);
        if (points.Count == 0)
        {
            points.Add(targetPos);
            return;
        }

        Vector3Int lastCell = RouteAuthoringUtility.WorldToCell(points[points.Count - 1]);
        if (targetCell == lastCell) return;

        if (points.Count == 1)
        {
            if (targetCell.x != lastCell.x && targetCell.y != lastCell.y)
            {
                Vector3Int corner = new Vector3Int(targetCell.x, lastCell.y, lastCell.z);
                points.Add(RouteAuthoringUtility.CellCenter(corner, z));
            }
            points.Add(targetPos);
            return;
        }

        Vector3Int prevCell = RouteAuthoringUtility.WorldToCell(points[points.Count - 2]);
        bool prevHorizontal = prevCell.y == lastCell.y && prevCell.x != lastCell.x;
        bool prevVertical = prevCell.x == lastCell.x && prevCell.y != lastCell.y;

        if (prevHorizontal && targetCell.y == lastCell.y)
        {
            if (targetCell == prevCell)
                points.RemoveAt(points.Count - 1);
            else
                points[points.Count - 1] = targetPos;
            return;
        }

        if (prevVertical && targetCell.x == lastCell.x)
        {
            if (targetCell == prevCell)
                points.RemoveAt(points.Count - 1);
            else
                points[points.Count - 1] = targetPos;
            return;
        }

        // User turned a corner: extend straight along the previous axis if diagonal, then add target
        if (targetCell.x != lastCell.x && targetCell.y != lastCell.y)
        {
            Vector3Int corner = prevHorizontal
                ? new Vector3Int(targetCell.x, lastCell.y, lastCell.z)
                : new Vector3Int(lastCell.x, targetCell.y, lastCell.z);
            if (corner != lastCell)
            {
                if (prevHorizontal && corner.y == prevCell.y)
                    points[points.Count - 1] = RouteAuthoringUtility.CellCenter(corner, z);
                else if (prevVertical && corner.x == prevCell.x)
                    points[points.Count - 1] = RouteAuthoringUtility.CellCenter(corner, z);
                else
                    points.Add(RouteAuthoringUtility.CellCenter(corner, z));
            }
        }

        points.Add(targetPos);
    }

    void HandleSplitScene(EnemyMovement route, Event e)
    {
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            mode = ToolMode.EditPoints;
            Repaint();
            e.Use();
            return;
        }

        var points = route.points;
        if (points == null) return;

        for (int i = 0; i < points.Length; i++)
        {
            Handles.color = RouteAuthoringUtility.ForkColor;
            Handles.DrawWireDisc(points[i], Vector3.forward, FlagSize * 1.4f);
            if (Handles.Button(points[i], Quaternion.identity, FlagSize * 1.1f, FlagSize * 1.3f, Handles.SphereHandleCap))
            {
                var branch = RouteAuthoringUtility.SplitRouteAt(route, i);
                if (branch != null)
                {
                    SelectRoute(branch);
                    mode = ToolMode.PaintBrush;
                    SetStatus($"Bifurcacion creada. Pinta '{branch.name}'.", MessageType.Info);
                }
                e.Use();
                return;
            }
            Handles.Label(points[i] + new Vector3(0.3f, 0.65f, 0f), "DIVIDIR", EditorStyles.boldLabel);
        }
    }

    void HandleMergeScene(EnemyMovement route, EnemyMovement[] allRoutes, Event e)
    {
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            mode = ToolMode.EditPoints;
            Repaint();
            e.Use();
            return;
        }

        foreach (var other in allRoutes)
        {
            if (other == null || other == route || other.points == null) continue;
            for (int i = 0; i < other.points.Length; i++)
            {
                Handles.color = RouteAuthoringUtility.StartColor;
                Handles.DrawWireDisc(other.points[i], Vector3.forward, FlagSize * 1.3f);
                if (Handles.Button(other.points[i], Quaternion.identity, FlagSize * 1.05f, FlagSize * 1.25f, Handles.SphereHandleCap))
                {
                    RouteAuthoringUtility.MergeRouteInto(route, other, i);
                    mode = ToolMode.EditPoints;
                    SetStatus($"'{route.name}' ahora se une con '{other.name}'.", MessageType.Info);
                    e.Use();
                    return;
                }
                Handles.Label(other.points[i] + new Vector3(0.3f, 0.65f, 0f), "UNIR AQUI", EditorStyles.boldLabel);
            }
        }
    }

    static void KeepSpawnerSynced(EnemyMovement route, int pointIndex, Vector3 snappedPos)
    {
        if (pointIndex != 0) return;
        var spawner = RouteAuthoringUtility.FindDirectSpawner(route);
        if (spawner != null)
        {
            Undo.RecordObject(spawner.transform, "Move spawner with route start");
            spawner.transform.position = new Vector3(snappedPos.x, snappedPos.y, spawner.transform.position.z);
        }
    }

    void SelectRoute(EnemyMovement route)
    {
        activeRoute = route;
        if (route != null)
            Selection.activeGameObject = route.gameObject;
        Repaint();
        SceneView.RepaintAll();
    }

    static void FrameRoute(EnemyMovement route)
    {
        if (route == null || route.points == null || route.points.Length == 0 || SceneView.lastActiveSceneView == null) return;
        Bounds b = new Bounds(route.points[0], Vector3.one * 4f);
        for (int i = 1; i < route.points.Length; i++)
            b.Encapsulate(route.points[i]);
        SceneView.lastActiveSceneView.Frame(b, false);
    }

    static Vector3 ScenePivotOrBunkerOffset()
    {
        if (SceneView.lastActiveSceneView != null)
            return SceneView.lastActiveSceneView.pivot;
        var door = RouteAuthoringUtility.FindBunkerDoorPosition();
        return door.HasValue ? door.Value + Vector3.right * 12f : Vector3.zero;
    }

    static Vector3 MouseWorld(Event e, float z)
    {
        Vector3 world = HandleUtility.GUIPointToWorldRay(e.mousePosition).origin;
        world.z = z;
        return world;
    }

    void SetStatus(string message, MessageType type)
    {
        statusMessage = message;
        statusType = type;
        Repaint();
    }
}
