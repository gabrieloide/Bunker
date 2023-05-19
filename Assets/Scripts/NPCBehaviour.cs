using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCBehaviour : MonoBehaviour
{
    [SerializeField] float radius;
    [SerializeField] float FireRate;
    [SerializeField] float damage = 5;
    [SerializeField] LayerMask Hitable;
    protected IEnumerator MoveAlongPath(Vector3[] path, bool reverse, GameObject obj, float speed, Transform transform)
    {
        int direction = reverse ? -1 : 1;
        int index = reverse ? path.Length - 1 : 0;
        while (index >= 0 && index < path.Length)
        {
            Vector3 targetPosition = path[index];

            RaycastHit2D hit = Physics2D.CircleCast(transform.position, radius, Vector2.zero, 5, Hitable);
            while (hit)
            {
                NPCAttack(hit);
                yield return FireRate;
            }
            Flip(targetPosition.x, obj.transform.position.x, transform);

            while (Vector3.Distance(obj.transform.position, targetPosition) > 0.01f && !hit)
            {
                obj.transform.position = Vector3.MoveTowards(obj.transform.position, targetPosition, speed * Time.deltaTime);
                yield return null;
            }
            index += direction;
        }
    }
    protected void NPCAttack(RaycastHit2D hit)
    {
        GameObject t = hit.collider.gameObject;

        TowerBullet bullet = ObjectPooling.instance.Shoot().GetComponent<TowerBullet>();
        bullet.transform.position = transform.position;

        Vector3 relativePos = t.transform.position - transform.position;
        float angle = Mathf.Atan2(relativePos.y, relativePos.x) * Mathf.Rad2Deg;
        bullet.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        bullet.GetData(t.transform, damage, 0);
    }
    protected void Flip(float PosX, float thisPosX, Transform transform)
    {
        Vector3 localScale = transform.localScale;
        localScale.x = Mathf.Abs(localScale.x) * (PosX < thisPosX ? -1 : 1);
        transform.localScale = localScale;
    }
}
