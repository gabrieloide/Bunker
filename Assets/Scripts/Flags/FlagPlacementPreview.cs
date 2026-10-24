using UnityEngine;

// Ghost of the flag shown while dragging a flag card, with a check or a cross next to it
// saying whether it can be planted there.
public class FlagPlacementPreview : MonoBehaviour
{
    [SerializeField] GameObject canPlace;
    [SerializeField] GameObject cannotPlace;

    public void Show(Vector3 position, bool valid)
    {
        transform.position = position;
        gameObject.SetActive(true);
        if (canPlace != null) canPlace.SetActive(valid);
        if (cannotPlace != null) cannotPlace.SetActive(!valid);
    }

    public void Hide() => gameObject.SetActive(false);
}
