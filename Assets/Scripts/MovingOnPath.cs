using System.Collections;
using UnityEngine;

public class MovingOnPath : MonoBehaviour
{



}
public interface IMovable
{
    IEnumerator MoveAlongPath(Vector3[] path, bool reverse, GameObject obj, float speed, Transform transform);
}