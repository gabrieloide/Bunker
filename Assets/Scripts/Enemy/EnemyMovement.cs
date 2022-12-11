using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    public Vector3[] points;
    public Vector3[] Points => points;
    public Vector3 CurrentPosition => _currentPosition;
    Vector3 _currentPosition;
    bool _gameStarted;

    private void Start()
    {
        _gameStarted = true;
        _currentPosition = transform.position;

    }
    private void Update()
    {
        
    }
    public Vector3 GetPointsPosition(int index)
    {
        return CurrentPosition + Points[index];
    }
    private void OnDrawGizmos()
    {
        if (!_gameStarted && transform.hasChanged)
        {
            _currentPosition = transform.position;
        }
        for (int i = 0; i < points.Length; i++)
        {
            Gizmos.color = Color.black;
            Gizmos.DrawWireSphere(points[i] + _currentPosition, 0.5f);
            if (i < points.Length -1)
            {
                Gizmos.color = Color.gray;
                Gizmos.DrawLine(points[i] + _currentPosition, points[i + 1] + _currentPosition);
            }
        }
    }


}
