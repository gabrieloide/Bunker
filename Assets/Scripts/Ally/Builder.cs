using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Soldier sent from the bunker to put a card's object on the map: walks to the site along the grid (no diagonals),
// waits while it is built, then walks the same route back and disappears. Presentation only; enemies ignore it.
public class Builder : MonoBehaviour
{
    [Min(0.1f)] [SerializeField] float speed = 5f;
    [Tooltip("Stops this far from the site along the last leg, on the side it came from, so it doesn't stand on the build")]
    [Min(0f)] [SerializeField] float standOff = 0.7f;
    [Tooltip("Walks the vertical leg first, then the horizontal one")]
    [SerializeField] bool verticalFirst;
    [Tooltip("Walks back as this one instead, e.g. the flag bearer returns as a plain builder once the flag is planted")]
    [SerializeField] Builder returnsAs;

    // onArrive builds and returns how long the building takes
    public void Go(Vector3 home, Vector3 site, Func<float> onArrive)
    {
        StartCoroutine(Run(home, site, onArrive));
    }

    IEnumerator Run(Vector3 home, Vector3 site, Func<float> onArrive)
    {
        transform.position = home;
        List<Vector3> path = GridPath(home, site);

        // Stand off the site along the last non-empty leg
        Vector3 end = path[path.Count - 1];
        int i = path.Count - 2;
        while (i > 0 && (path[i] - end).sqrMagnitude < 1e-6f) i--;
        path[path.Count - 1] = end + Vector3.ClampMagnitude(path[i] - end, standOff);

        yield return Walk(path);
        float buildTime = onArrive != null ? onArrive() : 0f;
        if (buildTime > 0f)
            yield return new WaitForSeconds(buildTime);

        path.Reverse();
        if (returnsAs != null)
        {
            var back = Instantiate(returnsAs, transform.position, Quaternion.identity);
            back.transform.localScale = transform.localScale; // keeps facing if the first leg back is vertical
            back.StartCoroutine(back.ReturnHome(path));
            Destroy(gameObject);
            yield break;
        }
        yield return ReturnHome(path);
    }

    IEnumerator ReturnHome(List<Vector3> path)
    {
        yield return Walk(path);
        Destroy(gameObject);
    }

    const float DeadZoneX = 0.05f;

    // Axis-aligned route home -> site: onto the centre line of the bunker's cell, then one leg per axis
    List<Vector3> GridPath(Vector3 home, Vector3 site)
    {
        var path = new List<Vector3> { home };
        Vector3 from = home;
        if (PlacementGrid.Available)
        {
            Vector3 c = PlacementGrid.CellCenter(PlacementGrid.WorldToCell(home));
            from = verticalFirst ? new Vector3(c.x, home.y, home.z) : new Vector3(home.x, c.y, home.z);
            if ((from - home).sqrMagnitude > 1e-4f)
                path.Add(from);
        }
        Vector3 corner = verticalFirst ? new Vector3(from.x, site.y, home.z) : new Vector3(site.x, from.y, home.z);
        if ((corner - path[path.Count - 1]).sqrMagnitude > 1e-4f)
            path.Add(corner);
        Vector3 dest = new Vector3(site.x, site.y, home.z);
        if ((dest - path[path.Count - 1]).sqrMagnitude > 1e-4f)
            path.Add(dest);
        return path;
    }

    // path[0] is where it stands now
    IEnumerator Walk(List<Vector3> path)
    {
        for (int i = 1; i < path.Count; i++)
            yield return WalkTo(path[i]);
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
        if (Mathf.Abs(dx) < DeadZoneX) return;
        var scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (dx < 0f ? -1f : 1f);
        transform.localScale = scale;
    }
}
