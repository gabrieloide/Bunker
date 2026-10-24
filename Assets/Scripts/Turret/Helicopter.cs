using System.Collections;
using Unity.Entities;
using UnityEngine;

// Placed prefab of the helicopter card: flies from the bunker to the drop point, then patrols a zone
// around it as a shooting tower for the card's hover time (chasing the nearest enemy inside the zone,
// wandering when there is none), then flies home and disappears. If it is shot down the bridge removes
// this view like any destroyed tower.
public class Helicopter : MonoBehaviour
{
    HelicopterCardDefinition definition;

    public void Launch(HelicopterCardDefinition def, Vector3 home, Vector3 site)
    {
        definition = def;
        StartCoroutine(Run(home, site));
    }

    IEnumerator Run(Vector3 home, Vector3 site)
    {
        transform.position = home;
        yield return FlyTo(site);

        var bridge = SimulationBridge.Instance;
        Entity entity = bridge != null ? bridge.StartHovering(definition, gameObject, site) : Entity.Null;
        yield return Patrol(bridge, entity, site);
        if (bridge != null)
            bridge.StopHovering(entity);

        yield return FlyTo(home);
        Destroy(gameObject);
    }

    IEnumerator Patrol(SimulationBridge bridge, Entity entity, Vector3 site)
    {
        float end = Time.time + definition.hoverTime;
        Vector3 waypoint = site;
        while (Time.time < end)
        {
            Vector3 position = transform.position;
            if (SimulationBridge.TryGetNearestEnemy(site, definition.patrolRadius, position, out Vector3 enemy))
                waypoint = new Vector3(enemy.x, enemy.y, position.z);
            else if ((position - waypoint).sqrMagnitude < 0.01f)
                waypoint = site + (Vector3)(Random.insideUnitCircle * definition.patrolRadius);

            Face(waypoint.x - position.x);
            position = Vector3.MoveTowards(position, waypoint, definition.patrolSpeed * Time.deltaTime);
            transform.position = position;
            if (bridge != null)
                bridge.MoveHovering(entity, position);
            yield return null;
        }
    }

    IEnumerator FlyTo(Vector3 target)
    {
        target.z = transform.position.z;
        Face(target.x - transform.position.x);
        while ((transform.position - target).sqrMagnitude > 1e-4f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, definition.flySpeed * Time.deltaTime);
            yield return null;
        }
    }

    // Same convention as allies and builders: the art faces right, negative x scale faces left
    void Face(float dx)
    {
        if (Mathf.Abs(dx) < 1e-4f) return;
        var scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (dx < 0f ? -1f : 1f);
        transform.localScale = scale;
    }
}
