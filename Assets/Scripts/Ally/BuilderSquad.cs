using System;
using UnityEngine;

// Sends a Builder out of the bunker for every card that puts an object on the map.
// Without it in the scene, cards place their object straight away.
public class BuilderSquad : MonoBehaviour
{
    public static BuilderSquad Instance { get; private set; }

    [SerializeField] Builder builderPrefab;

    void Awake() => Instance = this;

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public bool Available => builderPrefab != null;

    // onArrive builds and returns how long the building takes
    public void Send(Vector3 site, Func<float> onArrive)
    {
        var builder = Instantiate(builderPrefab, transform.position, Quaternion.identity);
        builder.Go(transform.position, site, onArrive);
    }

    // Placeholder (the same slot marker shown while dragging) that holds the site until the builder arrives
    public static GameObject MarkSite(Vector3 site)
    {
        var ui = UIManager.instance;
        if (ui == null || ui.TowerSlotAnimation == null) return new GameObject("BuildSite");
        var marker = Instantiate(ui.TowerSlotAnimation, site - ui.offset, Quaternion.identity);
        marker.name = "BuildSite";
        marker.SetActive(true);
        return marker;
    }
}
