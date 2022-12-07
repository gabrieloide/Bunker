using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(EnemyMovement))]
public class MovementEditor : Editor
{
    EnemyMovement Points => target as EnemyMovement;
    private void OnSceneGUI()
    {
        Handles.color = Color.cyan;
        for (int i = 0; i < Points.Points.Length; i++)
        {
            EditorGUI.BeginChangeCheck();

            Vector3 currentWaypointPoint = Points.CurrentPosition + Points.Points[i];
            Vector3 newWaypointPoint = Handles.FreeMoveHandle(currentWaypointPoint,
                Quaternion.identity , 0.7f,
                new Vector3(0.3f, 0.3f, 0.3f), Handles.SphereHandleCap);

            GUIStyle textStyle = new GUIStyle();
            textStyle.fontStyle = FontStyle.Bold;
            textStyle.fontSize = 16;
            textStyle.normal.textColor = Color.white;
            Vector3 textAllingment = Vector3.down * 0.35f + Vector3.right * 0.35f;
            Handles.Label(Points.CurrentPosition + Points.Points[i] + textAllingment, $"{ i + 1}", textStyle);
            EditorGUI.EndChangeCheck();
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Free Move Handle");
                Points.Points[i] = newWaypointPoint - Points.CurrentPosition;
            }
        }
    }
}
