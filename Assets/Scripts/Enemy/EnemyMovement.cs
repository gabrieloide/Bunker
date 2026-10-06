using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    public Color pathColor = new Color(0.15f, 0.85f, 1f, 1f);
    public float waypointRadius = 0.35f;
    public bool showLabels = true;

    public Vector3[] points = new Vector3[0];
    public Vector3[] Points => points;
    public Vector3 CurrentPosition => _gameStarted ? _currentPosition : transform.position;
    Vector3 _currentPosition;
    bool _gameStarted;

    private void Awake()
    {
        _gameStarted = true;
        _currentPosition = transform.position;
    }

    private void Start()
    {
        _gameStarted = true;
        _currentPosition = transform.position;
    }

    public Vector3 GetPointsPosition(int index)
    {
        if (points == null || index < 0 || index >= points.Length)
            return CurrentPosition;
        return CurrentPosition + points[index];
    }

    private void OnDrawGizmos()
    {
        if (points == null || points.Length == 0) return;

        Vector3 origin = CurrentPosition;

        for (int i = 0; i < points.Length; i++)
        {
            Vector3 worldPos = origin + points[i];

            // Color-code start, waypoints, and end (bunker)
            if (i == 0)
            {
                Gizmos.color = new Color(0.2f, 1f, 0.3f, 0.9f); // Green for Spawner/Start
                Gizmos.DrawSphere(worldPos, waypointRadius * 1.3f);
                Gizmos.color = Color.white;
                Gizmos.DrawWireSphere(worldPos, waypointRadius * 1.6f);
            }
            else if (i == points.Length - 1)
            {
                Gizmos.color = new Color(1f, 0.25f, 0.25f, 0.95f); // Red for Bunker/End
                Gizmos.DrawSphere(worldPos, waypointRadius * 1.4f);
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(worldPos, waypointRadius * 1.7f);
            }
            else
            {
                Gizmos.color = pathColor;
                Gizmos.DrawSphere(worldPos, waypointRadius);
                Gizmos.color = Color.white;
                Gizmos.DrawWireSphere(worldPos, waypointRadius * 1.15f);
            }

            // Draw line to next point
            if (i < points.Length - 1)
            {
                Vector3 nextPos = origin + points[i + 1];
                Gizmos.color = pathColor;
                Gizmos.DrawLine(worldPos, nextPos);

                // Draw directional marker along line
                Vector3 midPoint = (worldPos + nextPos) * 0.5f;
                Vector3 dir = (nextPos - worldPos).normalized;
                Vector3 perp = new Vector3(-dir.y, dir.x, 0f) * 0.25f;
                Gizmos.color = Color.white;
                Gizmos.DrawLine(midPoint - dir * 0.3f + perp, midPoint);
                Gizmos.DrawLine(midPoint - dir * 0.3f - perp, midPoint);
            }

#if UNITY_EDITOR
            if (showLabels)
            {
                string label = i == 0 ? $"{gameObject.name} [START]" :
                               i == points.Length - 1 ? $"{gameObject.name} [END]" :
                               $"{i}";
                var style = new GUIStyle();
                style.normal.textColor = (i == 0) ? Color.green : (i == points.Length - 1) ? Color.red : Color.cyan;
                style.fontStyle = FontStyle.Bold;
                style.fontSize = 11;
                UnityEditor.Handles.Label(worldPos + Vector3.up * 0.5f, label, style);
            }
#endif
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (points == null || points.Length < 2) return;
#if UNITY_EDITOR
        Vector3 origin = CurrentPosition;
        Vector3[] worldPoints = new Vector3[points.Length];
        for (int i = 0; i < points.Length; i++)
            worldPoints[i] = origin + points[i];

        UnityEditor.Handles.color = pathColor;
        UnityEditor.Handles.DrawAAPolyLine(6f, worldPoints);
#endif
    }
}
