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
    Material starMat;

    void Start()
    {
        SetupNodes();
        CreateStarDust();
    }

    void OnDestroy()
    {
        if (pixelTex != null) Destroy(pixelTex);
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
