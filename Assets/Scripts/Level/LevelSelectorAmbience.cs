using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Adds monochrome ambient polish to the level selector without altering hand-placed node layouts:
// subtle island bobbing, a pulsing silhouette outline on the active frontier node, pixel smoke on
// conquered nodes, drifting star dust, a 2-color pixel planet, and Mario World-style bridge unlock animation.
public class LevelSelectorAmbience : MonoBehaviour
{
    [Header("Island Float")]
    [SerializeField] float floatAmplitude = 0.06f;
    [SerializeField] float floatSpeed = 1.4f;

    [Header("Frontier Silhouette")]
    [SerializeField] Color silhouetteColor = Color.white;
    [SerializeField] float silhouetteOutlineWidth = 1.5f;
    [SerializeField] float silhouetteMinAlpha = 0.45f;
    [SerializeField] float silhouetteMaxAlpha = 0.95f;
    [SerializeField] float silhouettePulseSpeed = 2.6f;

    [Header("Unlock Animation")]
    [SerializeField] float unlockStartDelay = 0.35f;
    [SerializeField] float bridgeFillDuration = 0.65f;

    struct PathEntry
    {
        public string name;
        public int targetLevel;
        public Vector2 fillAxis;
        public float steps;

        public PathEntry(string name, int targetLevel, Vector2 fillAxis, float steps)
        {
            this.name = name;
            this.targetLevel = targetLevel;
            this.fillAxis = fillAxis;
            this.steps = steps;
        }
    }

    static readonly Color LockedPathColor = new Color(0.4f, 0.4f, 0.4f, 0.7f);
    static readonly PathEntry[] PathNodes =
    {
        new PathEntry("stairsplain", 1, Vector2.right, 13f),
        new PathEntry("stairsright", 2, Vector2.right, 12f),
        new PathEntry("stairsup", 3, Vector2.up, 13f),
        new PathEntry("stairsright (1)", 4, Vector2.right, 12f)
    };

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
    SpriteRenderer activeSilhouette;
    Material silhouetteMaterial;
    Material planetMat;
    Material starBurstMat;
    Material sparkleMat;
    Material bridgeFillMat;
    Texture2D pixelTex;
    Texture2D planetTex;
    Texture2D starBurstTex;
    Texture2D sparkleTex;
    Color32[] planetPixels;
    float planetSpinTimer;
    float planetSpinOffset;
    Transform planetTransform;
    Vector3 planetBasePos;
    Material starMat;

    void Start()
    {
        EnsureSpaceBackgroundOrder();
        SetupNodes();
        CreatePixelPlanet();
        CreateStarDust();
    }

    void OnDestroy()
    {
        if (pixelTex != null) Destroy(pixelTex);
        if (planetTex != null) Destroy(planetTex);
        if (starBurstTex != null) Destroy(starBurstTex);
        if (sparkleTex != null) Destroy(sparkleTex);
        if (starMat != null) Destroy(starMat);
        if (planetMat != null) Destroy(planetMat);
        if (starBurstMat != null) Destroy(starBurstMat);
        if (sparkleMat != null) Destroy(sparkleMat);
        if (bridgeFillMat != null) Destroy(bridgeFillMat);
        if (silhouetteMaterial != null) Destroy(silhouetteMaterial);
    }

    // In URP 2D, MeshRenderers on Default layer draw after Background layer SpriteRenderers,
    // which would paint the opaque SpaceBackground quad over the planet and star dust.
    void EnsureSpaceBackgroundOrder()
    {
        var bg = GameObject.Find("SpaceBackground");
        if (bg == null) return;
        var mr = bg.GetComponent<MeshRenderer>();
        if (mr == null) return;
        mr.sortingLayerName = "Background";
        mr.sortingOrder = -10;
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

        if (activeSilhouette != null)
        {
            float pulse = Mathf.Sin(t * silhouettePulseSpeed) * 0.5f + 0.5f;
            Color c = silhouetteColor;
            c.a = Mathf.Lerp(silhouetteMinAlpha, silhouetteMaxAlpha, pulse);
            activeSilhouette.color = c;
        }

        if (planetTex != null && planetTransform != null)
        {
            // Gentle orbital drift so the planet feels like distant space scenery, not a static prop
            Vector3 p = planetBasePos;
            p.x += Mathf.Sin(t * 0.22f) * 0.25f;
            p.y += Mathf.Cos(t * 0.35f) * 0.14f;
            planetTransform.localPosition = p;

            // Stepwise 6 FPS pixel-art surface rotation (strictly 2 colors: white & dark)
            planetSpinTimer += Time.unscaledDeltaTime;
            if (planetSpinTimer >= 1f / 6f)
            {
                planetSpinTimer -= 1f / 6f;
                planetSpinOffset = (planetSpinOffset + 0.35f) % 40f;
                RedrawPlanet(planetSpinOffset);
            }
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
        planetPixels = new Color32[size * size];
        RedrawPlanet(0f);

        var sprite = Sprite.Create(planetTex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
        var go = new GameObject("PixelPlanetAmbience");
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(cam.transform.position.x - halfW * 0.66f, cam.transform.position.y + halfH * 0.42f, 0f);
        go.transform.localScale = Vector3.one * 0.85f;
        go.transform.rotation = Quaternion.Euler(0f, 0f, 8f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = "Background";
        sr.sortingOrder = 2;

        var spriteShader = Shader.Find("Sprites/Default");
        if (spriteShader != null)
        {
            planetMat = new Material(spriteShader);
            sr.sharedMaterial = planetMat;
        }

        planetTransform = go.transform;
        planetBasePos = go.transform.localPosition;
    }

    void RedrawPlanet(float spinOffset)
    {
        const int size = 64;
        Color32 transparent = new Color32(0, 0, 0, 0);
        Color32 white = new Color32(255, 255, 255, 255);
        Color32 dark = new Color32(18, 18, 18, 255);

        float cx = (size - 1) * 0.5f;
        float cy = (size - 1) * 0.5f;
        float r = 20f;
        Vector2 lightDir = new Vector2(-0.65f, 0.75f).normalized;

        // Craters wrap horizontally around the sphere as spinOffset advances
        Vector3[] craters = {
            new Vector3(-6f, 5f, 3.0f),
            new Vector3(6f, -3f, 2.3f),
            new Vector3(-14f, -7f, 2.0f)
        };

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                Color32 col = transparent;

                float rx = dx * 0.92f + dy * 0.39f;
                float ry = -dx * 0.39f + dy * 0.92f;
                float ringDist = Mathf.Sqrt((rx / 30f) * (rx / 30f) + (ry / 9f) * (ry / 9f));
                bool onRing = Mathf.Abs(ringDist - 1f) < 0.08f && ((x + y) % 2 == 0);
                bool ringInFront = ry < 0f;

                if (dist <= r)
                {
                    if (dist >= r - 1.15f)
                    {
                        col = white;
                    }
                    else
                    {
                        float ndotl = (dx * lightDir.x + dy * lightDir.y) / r;
                        bool inCrater = false;
                        for (int c = 0; c < craters.Length; c++)
                        {
                            float shiftedX = ((craters[c].x + spinOffset + 20f) % 40f) - 20f;
                            float cd = Vector2.Distance(new Vector2(dx, dy), new Vector2(shiftedX, craters[c].y));
                            if (cd < craters[c].z) { inCrater = true; break; }
                        }

                        if (inCrater)
                            col = dark;
                        else if (ndotl > 0.25f)
                            col = white;
                        else if (ndotl > -0.15f)
                            col = ((x + y) % 2 == 0) ? white : dark;
                        else
                            col = dark;
                    }
                }

                if (onRing && (dist > r || ringInFront))
                    col = white;

                planetPixels[y * size + x] = col;
            }
        }

        planetTex.SetPixels32(planetPixels);
        planetTex.Apply();
    }

    void SetupNodes()
    {
        var nodes = new List<WorldLevelNode>(FindObjectsByType<WorldLevelNode>(FindObjectsSortMode.InstanceID));
        nodes.Sort((a, b) => a.levelIndex.CompareTo(b.levelIndex));

        int pendingUnlock = LevelProgress.ConsumePendingUnlock();
        WorldLevelNode pendingNode = null;
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

            if (node.levelIndex == pendingUnlock && unlocked && FindPathIndex(pendingUnlock) >= 0)
            {
                pendingNode = node;
                pendingNode.SetPendingReveal(true);
            }

            if (completed)
            {
                var smoke = new BunkerSmoke(visual, 0.3f);
                smoke.SetDestroyed();
            }
        }

        if (frontier == null && nodes.Count > 0)
            frontier = nodes[0];

        ApplyPathOpacity(pendingNode != null ? pendingUnlock : -1);

        if (pendingNode != null)
        {
            StartCoroutine(PlayUnlockSequence(pendingUnlock, pendingNode, frontier));
        }
        else if (frontier != null)
        {
            AttachSilhouette(frontier);
        }
    }

    static int FindPathIndex(int targetLevel)
    {
        for (int i = 0; i < PathNodes.Length; i++)
        {
            if (PathNodes[i].targetLevel == targetLevel)
                return i;
        }
        return -1;
    }

    void ApplyPathOpacity(int hiddenTargetLevel)
    {
        for (int i = 0; i < PathNodes.Length; i++)
        {
            var entry = PathNodes[i];
            var go = GameObject.Find(entry.name);
            if (go == null) continue;

            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) continue;

            bool lit = LevelProgress.IsUnlocked(entry.targetLevel) && entry.targetLevel != hiddenTargetLevel;
            sr.color = lit ? Color.white : LockedPathColor;
        }
    }

    IEnumerator PlayUnlockSequence(int targetLevel, WorldLevelNode targetNode, WorldLevelNode frontier)
    {
        int pathIdx = FindPathIndex(targetLevel);
        PathEntry entry = PathNodes[pathIdx];
        GameObject pathGo = GameObject.Find(entry.name);
        SpriteRenderer baseSr = pathGo != null ? pathGo.GetComponent<SpriteRenderer>() : null;

        GameObject fillGo = null;
        if (baseSr != null && baseSr.sprite != null)
        {
            var shader = Shader.Find("Bunker/SpriteFillReveal");
            if (shader != null)
            {
                bridgeFillMat = new Material(shader);
                bridgeFillMat.SetVector("_FillAxis", new Vector4(entry.fillAxis.x, entry.fillAxis.y, 0f, 0f));
                bridgeFillMat.SetFloat("_FillSteps", entry.steps);
                bridgeFillMat.SetFloat("_FillProgress", 0f);
                bridgeFillMat.SetVector("_UVBounds", ComputeSpriteUVBounds(baseSr.sprite));

                fillGo = new GameObject("BridgeUnlockFill", typeof(SpriteRenderer));
                fillGo.transform.SetParent(pathGo.transform, false);
                fillGo.transform.localPosition = Vector3.zero;
                fillGo.transform.localScale = Vector3.one;

                var fillSr = fillGo.GetComponent<SpriteRenderer>();
                fillSr.sprite = baseSr.sprite;
                fillSr.flipX = baseSr.flipX;
                fillSr.flipY = baseSr.flipY;
                fillSr.sortingLayerID = baseSr.sortingLayerID;
                fillSr.sortingOrder = baseSr.sortingOrder + 1;
                fillSr.color = Color.white;
                fillSr.sharedMaterial = bridgeFillMat;
            }
        }

        if (unlockStartDelay > 0f)
            yield return new WaitForSecondsRealtime(unlockStartDelay);

        if (bridgeFillMat != null && bridgeFillDuration > 0f)
        {
            float elapsed = 0f;
            int lastTickStep = -1;
            while (elapsed < bridgeFillDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / bridgeFillDuration);
                bridgeFillMat.SetFloat("_FillProgress", t);

                int step = Mathf.FloorToInt(t * entry.steps);
                if (step > lastTickStep && step % 2 == 0)
                {
                    lastTickStep = step;
                    SoundBank.Play(Sfx.UiClick);
                }

                yield return null;
            }
            bridgeFillMat.SetFloat("_FillProgress", 1f);
        }

        if (baseSr != null)
            baseSr.color = Color.white;
        if (fillGo != null)
            Destroy(fillGo);

        if (targetNode != null)
        {
            targetNode.RevealUnlocked();
            SoundBank.Play(Sfx.Buff);
            SpawnUnlockStarBurst(targetNode.transform.position);
        }

        if (frontier != null)
            AttachSilhouette(frontier);
    }

    static Vector4 ComputeSpriteUVBounds(Sprite sprite)
    {
        Vector2[] uvs = sprite.uv;
        if (uvs == null || uvs.Length == 0)
            return new Vector4(0f, 0f, 1f, 1f);

        float minU = float.MaxValue;
        float minV = float.MaxValue;
        float maxU = float.MinValue;
        float maxV = float.MinValue;
        for (int i = 0; i < uvs.Length; i++)
        {
            if (uvs[i].x < minU) minU = uvs[i].x;
            if (uvs[i].y < minV) minV = uvs[i].y;
            if (uvs[i].x > maxU) maxU = uvs[i].x;
            if (uvs[i].y > maxV) maxV = uvs[i].y;
        }
        return new Vector4(minU, minV, Mathf.Max(0.0001f, maxU - minU), Mathf.Max(0.0001f, maxV - minV));
    }

    void SpawnUnlockStarBurst(Vector3 center)
    {
        var spriteShader = Shader.Find("Sprites/Default");
        if (spriteShader == null) return;

        if (starBurstTex == null) starBurstTex = CreatePixelStarTexture();
        if (sparkleTex == null) sparkleTex = CreateSparkleTexture();
        if (starBurstMat == null) starBurstMat = new Material(spriteShader) { mainTexture = starBurstTex };
        if (sparkleMat == null) sparkleMat = new Material(spriteShader) { mainTexture = sparkleTex };

        // Primary Mario World-style ring of 4-point pixel stars popping out around the bunker
        CreateStarRingSystem(center, starBurstMat, 10, 0.48f, 1.45f, 0.48f, 0.62f, 0.85f, 0f);
        // Secondary staggered ring of smaller pixel sparkles for extra crispness
        StartCoroutine(SpawnDelayedSparkles(center + new Vector3(0f, 0.05f, 0f)));
    }

    IEnumerator SpawnDelayedSparkles(Vector3 center)
    {
        yield return new WaitForSecondsRealtime(0.08f);
        if (sparkleMat != null)
            CreateStarRingSystem(center, sparkleMat, 12, 0.58f, 1.8f, 0.24f, 0.34f, 0.7f, 15f);
    }

    void CreateStarRingSystem(
        Vector3 center,
        Material mat,
        int count,
        float startRadius,
        float radialSpeed,
        float minSize,
        float maxSize,
        float lifetime,
        float angleOffsetDeg)
    {
        var go = new GameObject("UnlockStarBurst");
        go.transform.SetParent(transform, false);
        go.transform.position = center;

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = lifetime;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startColor = Color.white;
        main.maxParticles = count + 4;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.useUnscaledTime = true;

        var emission = ps.emission;
        emission.enabled = false;

        var limitVel = ps.limitVelocityOverLifetime;
        limitVel.enabled = true;
        limitVel.limit = 0.12f;
        limitVel.dampen = 0.14f;

        var sizeCurve = new AnimationCurve(
            new Keyframe(0f, 0.2f),
            new Keyframe(0.14f, 1f),
            new Keyframe(0.36f, 0.48f),
            new Keyframe(0.56f, 0.96f),
            new Keyframe(0.8f, 0.55f),
            new Keyframe(1f, 0f));
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0f, 1f) });
        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = grad;

        var psr = go.GetComponent<ParticleSystemRenderer>();
        psr.sharedMaterial = mat;
        psr.sortingLayerName = "Default";
        psr.sortingOrder = 25;

        for (int i = 0; i < count; i++)
        {
            float angle = (angleOffsetDeg + i * (360f / count)) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            float jitter = (i % 2 == 0) ? 1.0f : 0.88f;
            var emit = new ParticleSystem.EmitParams
            {
                position = center + dir * (startRadius * jitter),
                velocity = dir * (radialSpeed * jitter),
                startSize = Mathf.Lerp(minSize, maxSize, (i % 3) * 0.5f),
                startLifetime = lifetime
            };
            ps.Emit(emit, 1);
        }

        ps.Play();
        Destroy(go, lifetime + 0.35f);
    }

    static Texture2D CreatePixelStarTexture()
    {
        const int size = 11;
        int c = size / 2;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        var pixels = new Color32[size * size];
        bool[,] whiteMask = new bool[size, size];
        Color32 clear = new Color32(0, 0, 0, 0);
        Color32 white = new Color32(255, 255, 255, 255);
        Color32 black = new Color32(0, 0, 0, 255);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int dx = Mathf.Abs(x - c);
                int dy = Mathf.Abs(y - c);
                whiteMask[x, y] = (dx == 0 && dy <= 4) || (dy == 0 && dx <= 4) || (dx <= 1 && dy <= 1);
            }
        }

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                if (whiteMask[x, y])
                {
                    pixels[y * size + x] = white;
                    continue;
                }
                bool adj = (x > 0 && whiteMask[x - 1, y])
                    || (x < size - 1 && whiteMask[x + 1, y])
                    || (y > 0 && whiteMask[x, y - 1])
                    || (y < size - 1 && whiteMask[x, y + 1]);
                pixels[y * size + x] = adj ? black : clear;
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }

    static Texture2D CreateSparkleTexture()
    {
        const int size = 7;
        int c = size / 2;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        var pixels = new Color32[size * size];
        bool[,] whiteMask = new bool[size, size];
        Color32 clear = new Color32(0, 0, 0, 0);
        Color32 white = new Color32(255, 255, 255, 255);
        Color32 black = new Color32(0, 0, 0, 255);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int dx = Mathf.Abs(x - c);
                int dy = Mathf.Abs(y - c);
                whiteMask[x, y] = dx + dy <= 2;
            }
        }

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                if (whiteMask[x, y])
                {
                    pixels[y * size + x] = white;
                    continue;
                }
                bool adj = (x > 0 && whiteMask[x - 1, y])
                    || (x < size - 1 && whiteMask[x + 1, y])
                    || (y > 0 && whiteMask[x, y - 1])
                    || (y < size - 1 && whiteMask[x, y + 1]);
                pixels[y * size + x] = adj ? black : clear;
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }

    void AttachSilhouette(WorldLevelNode frontier)
    {
        Transform visual = frontier.transform.Find("TurretVisual");
        if (visual == null) visual = frontier.transform;

        var visualRenderer = visual.GetComponent<SpriteRenderer>();
        if (visualRenderer == null || visualRenderer.sprite == null) return;

        var shader = Shader.Find("Bunker/SpriteSilhouetteOutline");
        if (shader == null) return;

        silhouetteMaterial = new Material(shader);
        silhouetteMaterial.SetFloat("_OutlineWidth", silhouetteOutlineWidth);

        var go = new GameObject("ActiveSectorSilhouette", typeof(SpriteRenderer));
        go.transform.SetParent(visual, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = Vector3.one;

        activeSilhouette = go.GetComponent<SpriteRenderer>();
        activeSilhouette.sprite = visualRenderer.sprite;
        activeSilhouette.flipX = visualRenderer.flipX;
        activeSilhouette.flipY = visualRenderer.flipY;
        activeSilhouette.sortingLayerID = visualRenderer.sortingLayerID;
        activeSilhouette.sortingOrder = visualRenderer.sortingOrder - 1;
        activeSilhouette.sharedMaterial = silhouetteMaterial;
        activeSilhouette.color = silhouetteColor;
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
