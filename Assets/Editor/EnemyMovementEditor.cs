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
    const float FlagSize = 0.35f;
    const float PlusSize = 0.18f;

    EnemyMovement Route => (EnemyMovement)target;

    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Abrir Editor Visual de Rutas (Modo Facil)", GUILayout.Height(30)))
            RouteDesignerWindow.OpenWithRoute(Route);

        EditorGUILayout.Space(4);

        if (Route.transform.position != Vector3.zero)
        {
            EditorGUILayout.HelpBox(
                "Este objeto no esta en (0, 0, 0). Los puntos de la ruta usan coordenadas del mundo.",
                MessageType.Warning);
            if (GUILayout.Button("Centrar objeto en (0, 0, 0)"))
            {
                Undo.RecordObject(Route.transform, "Reset route transform");
                Route.transform.position = Vector3.zero;
            }
        }

        EditorGUILayout.HelpBox(
            "Editar ruta en Scene View:\n" +
            "• Arrastra una bandera para moverla (snap a celda)\n" +
            "• Shift + clic: agregar bandera al final\n" +
            "• Clic en + sobre un tramo: insertar bandera\n" +
            "• Ctrl + clic en una bandera: borrarla\n" +
            "Los tramos rojos cruzan arboles o limites.", MessageType.Info);

        RouteAuthoringUtility.Measure(Route, out int length, out int blocked);
        var style = new GUIStyle(EditorStyles.boldLabel);
        if (blocked > 0) style.normal.textColor = RouteAuthoringUtility.BlockedColor;
        EditorGUILayout.LabelField("Length", length + " cells" + (blocked > 0 ? $"   ({blocked} through trees or limits)" : ""), style);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Snap to grid"))
                RouteAuthoringUtility.Edit(Route, "Snap route to grid", points =>
                {
                    for (int i = 0; i < points.Count; i++) points[i] = RouteAuthoringUtility.Snap(points[i]);
                });
            if (GUILayout.Button("Ends to spawner & door"))
                RouteAuthoringUtility.Edit(Route, "Route ends to spawner and door", points => RouteAuthoringUtility.SnapEnds(Route, points));
            if (GUILayout.Button("Reverse"))
                RouteAuthoringUtility.Edit(Route, "Reverse route", points => points.Reverse());
        }
        EditorGUILayout.Space();
        DrawDefaultInspector();
    }

    void OnSceneGUI()
    {
        if (RouteDesignerWindow.IsOpen) return;

        var route = Route;
        var points = route.points;
        if (points == null) return;
        Event e = Event.current;

        RouteAuthoringUtility.DrawRouteLegs(route, RouteAuthoringUtility.DefaultActiveColor, 5f, false);

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
            Handles.color = i == 0 ? RouteAuthoringUtility.StartColor : i == points.Length - 1 ? RouteAuthoringUtility.EndColor : RouteAuthoringUtility.DefaultActiveColor;
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
                        RouteAuthoringUtility.Edit(route, "Move route flag", list => list[at] = snapped);
                }
            }
            Handles.Label(points[i] + new Vector3(0.3f, 0.6f, 0f), i == 0 ? "START" : i == points.Length - 1 ? "END" : i.ToString(), EditorStyles.boldLabel);
        }

        // Shift+click on the map: a new flag at the end
        if (e.type == EventType.MouseDown && e.button == 0 && e.shift && !e.alt && !e.control)
        {
            Vector3 world = HandleUtility.GUIPointToWorldRay(e.mousePosition).origin;
            world.z = points.Length > 0 ? points[points.Length - 1].z : 0f;
            Vector3 added = RouteAuthoringUtility.Snap(world);
            RouteAuthoringUtility.Edit(route, "Add route flag", list => list.Add(added));
            e.Use();
        }
    }
}
