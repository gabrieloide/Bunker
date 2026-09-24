using System;
using System.Collections;
using UnityEngine;

// Soldier sent from the bunker to put a card's object on the map: walks to the site, waits while it is
// built, then walks back and disappears. Presentation only; enemies ignore it.
public class Builder : MonoBehaviour
{
    [Min(0.1f)] [SerializeField] float speed = 5f;
    [Tooltip("Stops this far from the site, on the side it came from, so it doesn't stand on the build")]
    [Min(0f)] [SerializeField] float standOff = 0.7f;

    // onArrive builds and returns how long the building takes
    public void Go(Vector3 home, Vector3 site, Func<float> onArrive)
    {
        StartCoroutine(Run(home, site, onArrive));
    }

    IEnumerator Run(Vector3 home, Vector3 site, Func<float> onArrive)
    {
        transform.position = home;
        Vector3 toHome = home - site;
        toHome.z = 0f;
        Vector3 stand = site + (toHome.sqrMagnitude > standOff * standOff ? toHome.normalized * standOff : Vector3.zero);

        yield return WalkTo(stand);
        float buildTime = onArrive != null ? onArrive() : 0f;
        if (buildTime > 0f)
            yield return new WaitForSeconds(buildTime);
        yield return WalkTo(home);
        Destroy(gameObject);
    }

    IEnumerator WalkTo(Vector3 target)
    {
        target.z = transform.position.z;
        Face(target.x - transform.position.x);
        while ((transform.position - target).sqrMagnitude > 1e-4f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            yield return null;
        }
    }

    // Same convention as enemies and allies: the art faces right, negative x scale faces left
    void Face(float dx)
    {
        if (Mathf.Abs(dx) < 1e-4f) return;
        var scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (dx < 0f ? -1f : 1f);
        transform.localScale = scale;
    }
}
