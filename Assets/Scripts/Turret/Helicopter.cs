using System.Collections;
using Unity.Entities;
using UnityEngine;

// Placed prefab of the helicopter card: flies from the bunker to the drop point, hovers there as a
// shooting tower for the card's hover time, then flies home and disappears. If it is shot down the
// bridge removes this view like any destroyed tower.
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
        yield return new WaitForSeconds(definition.hoverTime);
        if (bridge != null)
            bridge.StopHovering(entity);

        yield return FlyTo(home);
        Destroy(gameObject);
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
