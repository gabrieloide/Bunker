using UnityEngine;

// Presentation for a mortar shell entity. Position comes from the simulation; this only points the
// sprite along the flight, fades it near the apex and marks the splash area on the ground.
public class ArtilleryShellView : MonoBehaviour
{
    [SerializeField] SpriteRenderer body;
    [Tooltip("Ground ring showing where the shell lands and its splash radius; detached on spawn")]
    [SerializeField] DottedRing impactMarker;
    [SerializeField] Color markerColor = new Color32(34, 35, 35, 255);
    [Range(0f, 1f)][SerializeField] float markerStartAlpha = 0.35f;
    [Tooltip("Fraction of the apex height where the shell starts fading out")]
    [Range(0f, 1f)][SerializeField] float fadeStart = 0.6f;

    Vector3 origin;
    Vector3 impact;
    float height;
    float lastY;
    bool falling;

    public void Init(Vector3 origin, Vector3 impact, float height, float splashRadius)
    {
        this.origin = origin;
        this.impact = impact;
        this.height = height;
        lastY = transform.position.y;
        falling = false;

        if (impactMarker != null)
        {
            impactMarker.transform.SetParent(null, false);
            impactMarker.transform.position = impact;
            impactMarker.SetRadius(splashRadius);
            impactMarker.SetColor(WithAlpha(markerColor, markerStartAlpha));
        }
        Refresh();
    }

    void LateUpdate() => Refresh();

    void Refresh()
    {
        float y = transform.position.y;
        // Rising sits on the muzzle column, falling on the impact column; the apex swap reads as a drop in y
        if (y < lastY - 1e-4f) falling = true;
        lastY = y;

        if (body != null)
        {
            body.flipY = falling;
            float above = y - (falling ? impact.y : origin.y);
            float fade = height > 0f ? Mathf.InverseLerp(height, height * fadeStart, above) : 1f;
            body.color = WithAlpha(body.color, fade);
        }

        if (impactMarker != null && falling)
        {
            float landed = height > 0f ? 1f - Mathf.Clamp01((y - impact.y) / height) : 1f;
            impactMarker.SetColor(WithAlpha(markerColor, Mathf.Lerp(markerStartAlpha, markerColor.a, landed)));
        }
    }

    void OnDestroy()
    {
        if (impactMarker != null)
            Destroy(impactMarker.gameObject);
    }

    static Color WithAlpha(Color c, float a)
    {
        c.a = a;
        return c;
    }
}
