using System.Collections.Generic;
using Bunker.Simulation;
using UnityEditor;
using UnityEngine;

// Draw enemy routes by hand, comfortably: drag flags in the Scene view (they snap to cell centres), Shift+click to add one at
// the end, click a "+" to insert one in a leg, Ctrl+click to delete. Legs through decoration or limits turn red, and the
// inspector shows the length and quick fixes. Everything goes through Undo.
[CustomEditor(typeof(EnemyMovement))]
public class EnemyMovementEditor : Editor
{
    static readonly Color LegColor = new Color(0.15f, 0.85f, 1f, 1f);
    static readonly Color BlockedColor = new Color(1f, 0.25f, 0.25f, 1f);
    static readonly Color StartColor = new Color(0.2f, 1f, 0.3f, 1f);
    static readonly Color EndColor = new Color(1f, 0.35f, 0.35f, 1f);
    const float FlagSize = 0.35f;
    const float PlusSize = 0.18f;

    EnemyMovement Route => (EnemyMovement)target;

    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox(
            "Edit the route in the Scene view:\n" +
            "• Drag a flag to move it (snaps to the cell centre)\n" +
            "• Shift+click: add a flag at the end\n" +
            "• Click a + on a leg: insert a flag there\n" +
            "• Ctrl+click a flag: delete it\n" +
            "Red legs cross decoration or limits.", MessageType.Info);

        Measure(Route, out int length, out int blocked);
        var style = new GUIStyle(EditorStyles.boldLabel);
        if (blocked > 0) style.normal.textColor = BlockedColor;
        EditorGUILayout.LabelField("Length", length + " cells" + (blocked > 0 ? $"   ({blocked} through trees or limits)" : ""), style);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Snap to grid"))
                Edit("Snap route to grid", points =>
                {
                    for (int i = 0; i < points.Count; i++) points[i] = Snap(points[i]);
                });
            if (GUILayout.Button("Ends to spawner & door"))
                Edit("Route ends to spawner and door", SnapEnds);
            if (GUILayout.Button("Reverse"))
                Edit("Reverse route", points => points.Reverse());
        }
        EditorGUILayout.Space();
        DrawDefaultInspector();
    }

    void OnSceneGUI()
    {
        var route = Route;
        var points = route.points;
        if (points == null) return;
        Event e = Event.current;

        // Legs as soldiers walk them (with the L corners), red where they cross decoration or limits
        var walked = route.BuildRoute();
        Vector3Int first = walked.Count > 0 ? PlacementGrid.WorldToCell(walked[0].position) : default;
        Vector3Int last = walked.Count > 0 ? PlacementGrid.WorldToCell(walked[walked.Count - 1].position) : default;
        for (int i = 1; i < walked.Count; i++)
        {
            Handles.color = LegBlocked(walked[i - 1].position, walked[i].position, first, last) ? BlockedColor : LegColor;
            Handles.DrawAAPolyLine(5f, walked[i - 1].position, walked[i].position);
        }

        // "+" on the first leg between two flags: inserts a flag there
        for (int i = 1; i < points.Length; i++)
        {
            Vector3 legEnd = RouteGeometry.TryGetCorner(points[i - 1], points[i], route.verticalFirst, out var corner) ? corner : points[i];
            Vector3 mid = (points[i - 1] + legEnd) * 0.5f;
            Handles.color = Color.white;
            if (Handles.Button(mid, Quaternion.identity, PlusSize, PlusSize * 1.5f, Handles.DotHandleCap))
            {
                int at = i;
                Vector3 inserted = Snap(mid);
                Edit("Insert route flag", list => list.Insert(at, inserted));
                return;
            }
        }

        // Flags: drag to move, Ctrl+click to delete
        for (int i = 0; i < points.Length; i++)
        {
            Handles.color = i == 0 ? StartColor : i == points.Length - 1 ? EndColor : LegColor;
            if (e.control)
            {
                if (Handles.Button(points[i], Quaternion.identity, FlagSize, FlagSize, Handles.SphereHandleCap))
                {
                    int at = i;
                    Edit("Delete route flag", list => list.RemoveAt(at));
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
                    Vector3 snapped = Snap(new Vector3(moved.x, moved.y, points[i].z));
                    if (snapped != points[i])
                        Edit("Move route flag", list => list[at] = snapped);
                }
            }
            Handles.Label(points[i] + new Vector3(0.3f, 0.6f, 0f), i == 0 ? "START" : i == points.Length - 1 ? "END" : i.ToString(), EditorStyles.boldLabel);
        }

        // Shift+click on the map: a new flag at the end
        if (e.type == EventType.MouseDown && e.button == 0 && e.shift && !e.alt && !e.control)
        {
            Vector3 world = HandleUtility.GUIPointToWorldRay(e.mousePosition).origin;
            world.z = points.Length > 0 ? points[points.Length - 1].z : 0f;
            Vector3 added = Snap(world);
            Edit("Add route flag", list => list.Add(added));
            e.Use();
        }
    }

    // Every change goes through Undo and marks the scene dirty
    void Edit(string undoName, System.Action<List<Vector3>> change)
    {
        var route = Route;
        Undo.RecordObject(route, undoName);
        var list = new List<Vector3>(route.points ?? new Vector3[0]);
        change(list);
        route.points = list.ToArray();
        EditorUtility.SetDirty(route);
        SceneView.RepaintAll();
    }

    static Vector3 Snap(Vector3 position)
    {
        if (!PlacementGrid.Available) return position;
        Vector3 centre = PlacementGrid.CellCenter(PlacementGrid.WorldToCell(position));
        centre.z = position.z;
        return centre;
    }

    static void SnapEnds(List<Vector3> points)
    {
        var spawner = FindAnyObjectByType<EnemySpawner>();
        var door = FindAnyObjectByType<AllySpawner>();
        float z = points.Count > 0 ? points[0].z : 0f;
        if (spawner != null)
        {
            var start = Snap(new Vector3(spawner.transform.position.x, spawner.transform.position.y, z));
            if (points.Count == 0) points.Add(start); else points[0] = start;
        }
        if (door != null)
        {
            var end = Snap(new Vector3(door.transform.position.x, door.transform.position.y, z));
            if (points.Count < 2) points.Add(end); else points[points.Count - 1] = end;
        }
    }

    // The route's own two ends don't count: the spawner and the bunker door sit on the map art
    static bool LegBlocked(Vector3 from, Vector3 to, Vector3Int routeStart, Vector3Int routeEnd)
    {
        if (!PlacementGrid.Available) return false;
        var at = PlacementGrid.WorldToCell(from);
        var end = PlacementGrid.WorldToCell(to);
        while (true)
        {
            if (at != routeStart && at != routeEnd && PlacementGrid.BlocksWalking(at)) return true;
            if (at == end) return false;
            if (at.x != end.x) at.x += System.Math.Sign(end.x - at.x);
            else at.y += System.Math.Sign(end.y - at.y);
        }
    }

    // Cells walked, and how many of them sit on decoration or limits (the two ends may: spawner and door sit on the map art)
    static void Measure(EnemyMovement route, out int length, out int blocked)
    {
        var cells = route.RouteCells();
        length = Mathf.Max(0, cells.Count - 1);
        blocked = 0;
        for (int i = 1; i < cells.Count - 1; i++)
            if (PlacementGrid.BlocksWalking(cells[i])) blocked++;
    }
}
