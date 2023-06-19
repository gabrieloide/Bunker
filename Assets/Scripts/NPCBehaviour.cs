using System.Collections;
using UnityEngine;

public class NPCBehaviour : MonoBehaviour
{
    [Range(3, 20)][SerializeField] float radius;
    [SerializeField] protected float FireRate;
    public float fireRateCountDown;
    protected float damage = 5;
    protected LayerMask Hitable;


    protected IEnumerator MoveAlongPath(Vector3[] path, bool reverse, GameObject obj, float speed, Transform transform)
    {
        int direction = reverse ? -1 : 1;
        int index = reverse ? path.Length - 1 : 0;
        fireRateCountDown = FireRate;
        while (index >= 0 && index < path.Length)
        {
            Vector3 targetPosition = path[index];

            RaycastHit2D hit = Physics2D.CircleCast(transform.position, radius, Vector2.right, 0, Hitable);

            Flip(targetPosition.x, obj.transform.position.x, transform);

            while (Vector3.Distance(obj.transform.position, targetPosition) > 0.01f)
            {
                obj.transform.position = Vector3.MoveTowards(obj.transform.position, targetPosition, speed * Time.deltaTime);
                fireRateCountDown -= Time.deltaTime;
                if (hit)
                {
                    NPCAttack(hit);
                }

                yield return null;
            }
            index += direction;
        }
        BehaviourFinalPath();
    }
    protected virtual void BehaviourFinalPath()
    {

    }
    protected void NPCAttack(RaycastHit2D hit)
    {
        if (fireRateCountDown < 0)
        {
            GameObject t = hit.collider.gameObject;

            EnemyBullet bullet = ObjectPooling.instance.EnemyShoot().GetComponent<EnemyBullet>();
            bullet.transform.position = transform.position;

            RotateObjectTo.Rotation(bullet.gameObject, t.transform, transform);

            bullet.GetData(t.transform.position, damage, 0);
            fireRateCountDown = FireRate;
        }
    }
    protected void Flip(float PosX, float thisPosX, Transform transform)
    {
        Vector3 localScale = transform.localScale;
        localScale.x = Mathf.Abs(localScale.x) * (PosX < thisPosX ? -1 : 1);
        transform.localScale = localScale;
    }
}
