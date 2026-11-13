using System.Collections.Generic;
using UnityEngine;

// Adds monochrome ambient polish to the level selector without altering hand-placed node layouts:
// subtle island bobbing, a rotating DottedRing beacon on the active frontier node, pixel smoke on
// conquered nodes, and a slow drifting black-and-white star dust particle layer.
public class LevelSelectorAmbience : MonoBehaviour
{
    [Header("Island Float")]
    [SerializeField] float floatAmplitude = 0.06f;
    [SerializeField] float floatSpeed = 1.4f;

    [Header("Frontier Beacon")]
    [SerializeField] float beaconBaseRadius = 1.25f;
    [SerializeField] float beaconPulseAmount = 0.08f;
    [SerializeField] float beaconSpinSpeed = 18f;

    [Header("Star Dust")]
    [SerializeField] int maxParticles = 45;
    [SerializeField] float driftSpeed = 0.25f;

    struct FloatEntry
    {
        public Transform target;
        public Vector3 baseLocalPos;
        public float phase;
    }

    readonly List<FloatEntry> floaters = new List<FloatEntry>();
    DottedRing activeBeacon;
    Transform beaconTransform;
    Texture2D pixelTex;
    Texture2D planetTex;
    Material starMat;

    void Start()
    {
        SetupNodes();
        CreatePixelPlanet();
        CreateStarDust();
    }

    void OnDestroy()
    {
        if (pixelTex != null) Destroy(pixelTex);
        if (planetTex != null) Destroy(planetTex);
        if (starMat != null) Destroy(starMat);
    }

    void Update()
    {
        float t = Time.unscaledTime;
        for (int i = 0; i < floaters.Count; i++)
        {
            var f = floaters[i];
            if (f.target == null) continue;
            Vector3 p = f.baseLocalPos;
            p.y += Mathf.Sin(t * floatSpeed + f.phase) * floatAmplitude;
            f.target.localPosition = p;
        }

        if (activeBeacon != null && beaconTransform != null)
        {
            beaconTransform.Rotate(0f, 0f, -beaconSpinSpeed * Time.unscaledDeltaTime);
            float pulse = beaconBaseRadius + Mathf.Sin(t * 2.4f) * beaconPulseAmount;
            activeBeacon.SetRadius(pulse);
        }
    }

    void CreatePixelPlanet()
    {
        var cam = Camera.main;
        if (cam == null) return;

        float halfH = cam.orthographic ? cam.orthographicSize : 5f;
        float halfW = halfH * cam.aspect;

        const int size = 64;
        planetTex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        var pixels = new Color32[size * size];

        Color32 transparent = new Color32(0, 0, 0, 0);
        Color32 white = new Color32(235, 235, 235, 210);
        Color32 mid = new Color32(130, 130, 130, 180);
        Color32 dark = new Color32(34, 35, 35, 210);

        float cx = (size - 1) * 0.5f;
        float cy = (size - 1) * 0.5f;
        float r = 20f;
        Vector2 lightDir = new Vector2(-0.65f, 0.75f).normalized;

        // Craters in normalized local coords
        Vector3[] craters = {
            new Vector3(-6f, 5f, 3.2f),
            new Vector3(4f, -3f, 2.4f),
            new Vector3(-2f, -8f, 1.9f)
        };

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                Color32 col = transparent;

                // Tilted ring behind/around the planet
                float rx = dx * 0.92f + dy * 0.39f;
                float ry = -dx * 0.39f + dy * 0.92f;
                float ringDist = Mathf.Sqrt((rx / 30f) * (rx / 30f) + (ry / 9f) * (ry / 9f));
                bool onRing = Mathf.Abs(ringDist - 1f) < 0.08f && ((x + y) % 2 == 0);
                bool ringInFront = ry < 0f;

                if (dist <= r)
                {
                    if (dist >= r - 1.2f)
                    {
                        col = white;
                    }
                    else
                    {
                        float ndotl = (dx * lightDir.x + dy * lightDir.y) / r;
                        bool inCrater = false;
                        for (int c = 0; c < craters.Length; c++)
                        {
                            float cd = Vector2.Distance(new Vector2(dx, dy), new Vector2(craters[c].x, craters[c].y));
                            if (cd < craters[c].z) { inCrater = true; break; }
                        }

                        if (inCrater)
                            col = ((x + y) % 2 == 0) ? dark : mid;
                        else if (ndotl > 0.32f)
                            col = white;
                        else if (ndotl > -0.05f)
                            col = ((x + y) % 2 == 0) ? white : mid;
                        else if (ndotl > -0.35f)
                            col = ((x + y) % 2 == 0) ? mid : dark;
                        else
                            col = dark;
                    }
                }

                if (onRing && (dist > r || ringInFront))
                    col = white;

                pixels[y * size + x] = col;
            }
        }

        planetTex.SetPixels32(pixels);
        planetTex.Apply();

        var sprite = Sprite.Create(planetTex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
        var go = new GameObject("PixelPlanetAmbience");
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(cam.transform.position.x - halfW * 0.66f, cam.transform.position.y + halfH * 0.42f, 0f);
        go.transform.localScale = Vector3.one * 0.85f;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = "Background";
        sr.sortingOrder = 2;

        floaters.Add(new FloatEntry
        {
            target = go.transform,
            baseLocalPos = go.transform.localPosition,
            phase = 0.5f
        });
    }

    void SetupNodes()
    {
        var nodes = new List<WorldLevelNode>(FindObjectsByType<WorldLevelNode>(FindObjectsSortMode.InstanceID));
        nodes.Sort((a, b) => a.levelIndex.CompareTo(b.levelIndex));

        WorldLevelNode frontier = null;
        for (int i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            if (node == null) continue;

            Transform visual = node.transform.Find("TurretVisual");
            if (visual == null) visual = node.transform;

            floaters.Add(new FloatEntry
            {
                target = visual,
                baseLocalPos = visual.localPosition,
                phase = i * 1.15f
            });

            bool unlocked = LevelProgress.IsUnlocked(node.levelIndex);
            bool completed = LevelProgress.IsCompleted(node.levelIndex);

            if (unlocked && !completed && frontier == null)
                frontier = node;

            if (completed)
            {
                var smoke = new BunkerSmoke(visual, 0.3f);
                smoke.SetDestroyed();
            }
        }

        if (frontier == null && nodes.Count > 0)
            frontier = nodes[0];

        if (frontier != null)
            AttachBeacon(frontier.transform);
    }

    void AttachBeacon(Transform target)
    {
        var ringMat = Resources.Load<Material>("EnemyFlagRing");
        if (ringMat == null) return;

        var go = new GameObject("ActiveSectorBeacon", typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(target, false);
        go.transform.localPosition = Vector3.zero;

        var renderer = go.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = ringMat;

        activeBeacon = go.AddComponent<DottedRing>();
        activeBeacon.SetColor(new Color(1f, 1f, 1f, 0.65f));
        activeBeacon.SetRadius(beaconBaseRadius);
        beaconTransform = go.transform;
    }

    void CreateStarDust()
    {
        var cam = Camera.main;
        if (cam == null) return;

        float halfH = cam.orthographic ? cam.orthographicSize : 5f;
        float halfW = halfH * cam.aspect;

        var go = new GameObject("StarDustAmbience");
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 0f);

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.startLifetime = 10f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.1f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 1f, 1f, 0.25f),
            new Color(1f, 1f, 1f, 0.6f));
        main.maxParticles = maxParticles;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.useUnscaledTime = true;

        var emission = ps.emission;
        emission.rateOverTime = maxParticles / 10f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(halfW * 2.2f, halfH * 2.2f, 0.1f);

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.x = new ParticleSystem.MinMaxCurve(-driftSpeed, -driftSpeed * 0.4f);
        vel.y = new ParticleSystem.MinMaxCurve(-driftSpeed * 0.2f, driftSpeed * 0.2f);
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        pixelTex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        pixelTex.SetPixel(0, 0, Color.white);
        pixelTex.Apply();

        var shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            starMat = new Material(shader) { mainTexture = pixelTex };
            var psr = go.GetComponent<ParticleSystemRenderer>();
            psr.sharedMaterial = starMat;
            psr.sortingLayerName = "Background";
            psr.sortingOrder = 1;
        }
    }
}
