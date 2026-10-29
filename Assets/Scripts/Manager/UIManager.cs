using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class UIManager : MonoBehaviour
{
    [Header("Sounds")]
    [SerializeField] AK.Wwise.Event ShowBoxText;
    [Space]
    
    public static UIManager instance;
    public Texture2D cursorDefault, cursorTexture;
    [Space]
    [Tooltip("UI Toolkit HUD (UI/HUD.uxml): score, wave, towers and bunker life")]
    [SerializeField] UIDocument hud;
    public UIDocument HudDocument => hud;
    public PanelSettings HudPanelSettings => hud != null ? hud.panelSettings : null;
    [Space]
    [Header("CRT Compositing")]
    [Tooltip("UIComposite shader: composites HUD texture into camera backbuffer before RetroCRTFilter")]
    [SerializeField] Shader compositeShader;
    private RenderTexture hudTarget;
    private Material hudComposite;
    [Space]
    public bool ShowTowerSlot;
    public GameObject TowerSlotAnimation;
    public Vector3 offset;
    [Space]
    [Header("Life Turret")]
    [Tooltip("Bar over the bunker; found on the TowerPlayer when empty")]
    [SerializeField] WorldLifeBar LifeSlider;
    [Space]
    [Header("Deck")]
    public LeanTweenType TweenDeckOut;
    public float TimeMovement;

    private int lastScore = -1;
    private int scoreRollStart = 0;
    private int targetScore = 0;
    private int currentDisplayedScore = 0;
    private float scoreRollTimer = 0f;
    private const float ScoreRollDuration = 0.35f;
    private bool isRollingScore = false;

    private int lastWave = -1;
    private int lastPrepSeconds = -1;
    private bool wasPrepPhase;

    private float currentLife = 1f;
    private float targetLife = 1f;
    private const float LifeTransitionSpeed = 2.5f;
    private bool lifeInitialized = false;
    private float lastLife = -1f;

    private float lifebarShakeTimer = 0f;
    private const float LifebarShakeDuration = 0.12f;
    private const float LifebarShakeMaxOffset = 1.75f;

    private ChangeCardText cardPanel;
    private Label waveText, scoreText, towerCountText;
    private VisualElement lifebarElement, lifeTrack, lifeFill;

    private class ActiveDamageChunk
    {
        public VisualElement element;
        public float elapsed;
        public float sign;
    }

    private readonly List<ActiveDamageChunk> activeDamageChunks = new();
    private int chunkCounter = 0;
    private const float ChunkDuration = 0.45f;
    private const float ChunkPhase1Duration = 0.15f;
    private const float ChunkHudPhase1Drop = 11f; // 8px - 14px range in HUD
    private const float ChunkHudExtraDrop = 12f;

    void Awake()
    {
        if (!instance)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        if (instance != null && instance != this) return;

        SetupHudCompositing();

        if (hud != null)
        {
            var root = hud.rootVisualElement;
            root.pickingMode = PickingMode.Ignore; // cards and towers below must keep receiving clicks
            scoreText = root.Q<Label>("score");
            waveText = root.Q<Label>("wave");
            towerCountText = root.Q<Label>("towers");
            lifebarElement = root.Q("lifebar");
            lifeTrack = root.Q("lifebar-track") ?? lifebarElement;
            lifeFill = root.Q("life-fill");
        }

        lifeInitialized = false;
        lastLife = -1f;

        SetScore(SimEventDispatcher.Score, 0);
        SetWave(SimEventDispatcher.Wave, SimEventDispatcher.CurrentBuff);
        SetLifeNormalized(SimEventDispatcher.Health01);

        SimEventDispatcher.OnScoreChanged += SetScore;
        SimEventDispatcher.OnWaveChanged += SetWave;
        SimEventDispatcher.OnPlayerHit += OnPlayerHit;
        SimEventDispatcher.OnBunkerHealed += OnBunkerHealed;
        TowerLimits.Changed += UpdateTowerCount;
        UpdateTowerCount();
    }

    private void OnDisable()
    {
        Camera.onPostRender -= CompositeHud;

        SimEventDispatcher.OnScoreChanged -= SetScore;
        SimEventDispatcher.OnWaveChanged -= SetWave;
        SimEventDispatcher.OnPlayerHit -= OnPlayerHit;
        SimEventDispatcher.OnBunkerHealed -= OnBunkerHealed;
        TowerLimits.Changed -= UpdateTowerCount;

        ClearDamageChunks();
        lifeInitialized = false;
        lastLife = -1f;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;

        Camera.onPostRender -= CompositeHud;
        ClearDamageChunks();
        lifeInitialized = false;
        lastLife = -1f;
        if (hudTarget != null)
        {
            hudTarget.Release();
            hudTarget = null;
        }
        if (hudComposite != null)
        {
            Destroy(hudComposite);
            hudComposite = null;
        }
    }

    // Composites the HUD UIDocument through a RenderTexture in Camera.onPostRender
    // so it sits before RetroCRTFilter.OnRenderImage and receives authentic CRT curvature, scanlines and glow.
    void SetupHudCompositing()
    {
        if (hud == null || hud.panelSettings == null) return;

        var settings = Instantiate(hud.panelSettings);
        settings.clearColor = true;
        settings.colorClearValue = new Color(0f, 0f, 0f, 0f);
        settings.SetScreenToPanelSpaceFunction(ScreenToTexture);
        hud.panelSettings = settings;

        if (compositeShader == null)
            compositeShader = Shader.Find("Hidden/Bunker/UIComposite");
        if (compositeShader != null && hudComposite == null)
            hudComposite = new Material(compositeShader);

        ResizeHudTexture();
        Camera.onPostRender -= CompositeHud;
        Camera.onPostRender += CompositeHud;

        var pause = FindAnyObjectByType<PauseMenu>(FindObjectsInactive.Include);
        if (pause != null) pause.EnsureCompositedSettings();

        var matchResult = FindAnyObjectByType<MatchResultScreen>(FindObjectsInactive.Include);
        if (matchResult != null) matchResult.EnsureCompositedSettings();
    }

    void ResizeHudTexture()
    {
        if (hudTarget != null) hudTarget.Release();
        hudTarget = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32) { name = "HUD_RT" };
        if (hud != null && hud.panelSettings != null)
            hud.panelSettings.targetTexture = hudTarget;
    }

    Vector2 ScreenToTexture(Vector2 screen) =>
        hudTarget == null ? screen : new Vector2(screen.x * hudTarget.width / Screen.width, screen.y * hudTarget.height / Screen.height);

    void CompositeHud(Camera cam)
    {
        if (cam != Camera.main || hudTarget == null || hudComposite == null) return;
        hudComposite.mainTexture = hudTarget;
        GL.PushMatrix();
        GL.LoadOrtho();
        hudComposite.SetPass(0);
        GL.Begin(GL.QUADS);
        GL.TexCoord2(0, 0); GL.Vertex3(0, 0, 0);
        GL.TexCoord2(0, 1); GL.Vertex3(0, 1, 0);
        GL.TexCoord2(1, 1); GL.Vertex3(1, 1, 0);
        GL.TexCoord2(1, 0); GL.Vertex3(1, 0, 0);
        GL.End();
        GL.PopMatrix();
    }

    void LateUpdate()
    {
        if (hudTarget != null && (hudTarget.width != Screen.width || hudTarget.height != Screen.height))
            ResizeHudTexture();
    }

    // OnEnable can run before GameManager.Awake, so the limit is read again once everything exists
    void Start() => UpdateTowerCount();

    void UpdateTowerCount()
    {
        if (towerCountText == null) return;
        int max = GameManager.instance != null && GameManager.instance.Catalog != null ? GameManager.instance.Catalog.maxTowers : 0;
        towerCountText.text = max > 0 ? $"TOWERS\n{TowerLimits.Count}/{max}" : $"TOWERS\n{TowerLimits.Count}";
    }

    // Camera shake feedback when a tower drop is refused
    public void FlashTowerLimit()
    {
        if (towerCountText == null) return;
        CameraShake.MicroShake();
    }

    private void SetScore(int newScore, int delta)
    {
        if (scoreText == null) return;

        if (lastScore < 0)
        {
            lastScore = newScore;
            targetScore = newScore;
            currentDisplayedScore = newScore;
            scoreText.text = $"SCORE\n{newScore:000000}";
            return;
        }

        if (newScore != lastScore)
        {
            scoreRollStart = currentDisplayedScore;
            targetScore = newScore;
            scoreRollTimer = 0f;
            isRollingScore = true;
            lastScore = newScore;
        }
    }

    public void UpdateScoreRoll()
    {
        if (!isRollingScore || scoreText == null) return;

        scoreRollTimer += Time.deltaTime;
        float t = Mathf.Clamp01(scoreRollTimer / ScoreRollDuration);
        // Ease-Out Cubic: f(t) = 1 - (1 - t)^3
        float ease = 1f - Mathf.Pow(1f - t, 3f);
        int rolled = Mathf.RoundToInt(Mathf.Lerp((float)scoreRollStart, (float)targetScore, ease));
        currentDisplayedScore = rolled;
        scoreText.text = $"SCORE\n{rolled:000000}";

        if (t >= 1f)
        {
            currentDisplayedScore = targetScore;
            scoreText.text = $"SCORE\n{targetScore:000000}";
            isRollingScore = false;
        }
    }

    private void SetWave(int newWave, Bunker.Simulation.EnemyBuffKind buff)
    {
        if (waveText != null && !SimulationBridge.IsPreparationPhase)
        {
            if (newWave != lastWave || wasPrepPhase)
            {
                bool isIncrease = lastWave > 0 && newWave > lastWave;
                lastWave = newWave;
                wasPrepPhase = false;
                waveText.text = $"WAVE\n{newWave:000000}";
                if (isIncrease)
                {
                    CameraShake.MicroShake();
                }
            }
        }
    }

    private void UpdateWaveDisplay()
    {
        if (waveText == null) return;

        if (SimulationBridge.IsPreparationPhase)
        {
            int secondsLeft = Mathf.CeilToInt(SimEventDispatcher.PhaseTimer);
            if (!wasPrepPhase || secondsLeft != lastPrepSeconds)
            {
                wasPrepPhase = true;
                lastPrepSeconds = secondsLeft;
                waveText.text = $"PREP\n{secondsLeft:00}s";
            }
        }
        else if (wasPrepPhase || SimEventDispatcher.Wave != lastWave)
        {
            bool transitionedFromPrep = wasPrepPhase;
            int newWave = SimEventDispatcher.Wave;
            wasPrepPhase = false;
            lastWave = newWave;
            waveText.text = $"WAVE\n{lastWave:000000}";

            if (transitionedFromPrep || newWave > 1)
            {
                CameraShake.MicroShake();
            }
        }
    }

    private void OnPlayerHit(float damage, int remainingHP, float normalizedHP)
    {
        SetLifeNormalized(normalizedHP);
        TriggerLifeHitFeedback();
    }

    private void OnBunkerHealed(float amount, int remainingHP, float normalizedHP)
    {
        SetLifeNormalized(normalizedHP);
    }

    private void TriggerLifeHitFeedback()
    {
        if (lifebarElement != null)
        {
            // Subtle micro-vibration max 1.5px - 2.0px for 0.12s
            lifebarShakeTimer = LifebarShakeDuration;
        }
    }

    private void SetLifeNormalized(float normalizedHP)
    {
        float target = Mathf.Clamp01(normalizedHP);

        if (!lifeInitialized)
        {
            lifeInitialized = true;
            currentLife = target;
            targetLife = target;
            if (lifeFill != null)
                lifeFill.style.width = Length.Percent(currentLife * 100f);
        }
        else
        {
            if (target < currentLife - 0.0005f)
            {
                float prevLife = currentLife;
                float newLife = target;
                float delta = prevLife - newLife;
                currentLife = newLife;
                targetLife = newLife;
                if (lifeFill != null)
                    lifeFill.style.width = Length.Percent(currentLife * 100f);
                SpawnDamageChunk(newLife, delta);
            }
            else
            {
                targetLife = target;
            }
        }

        if (LifeSlider == null)
        {
            var tower = FindFirstObjectByType<TowerPlayer>();
            if (tower != null) LifeSlider = tower.GetComponentInChildren<WorldLifeBar>(true);
        }
        if (LifeSlider != null && !Mathf.Approximately(normalizedHP, lastLife))
        {
            lastLife = normalizedHP;
            LifeSlider.SetValue(normalizedHP, 1f);
        }
    }

    private void SpawnDamageChunk(float startLeft01, float width01)
    {
        if (lifeTrack == null || width01 <= 0.001f) return;

        var chunk = new VisualElement { pickingMode = PickingMode.Ignore };
        chunk.AddToClassList("lifebar-chunk");
        chunk.style.position = Position.Absolute;
        chunk.style.left = Length.Percent(Mathf.Clamp01(startLeft01) * 100f);
        chunk.style.width = Length.Percent(Mathf.Clamp01(width01) * 100f);
        chunk.style.top = 0f;
        chunk.style.bottom = 0f;
        chunk.style.height = Length.Percent(100f);
        chunk.style.backgroundColor = Color.black;
        chunk.style.borderLeftColor = new Color(0.941f, 0.965f, 0.941f, 1f); // #F0F6F0
        chunk.style.borderRightColor = new Color(0.941f, 0.965f, 0.941f, 1f);
        chunk.style.borderTopColor = new Color(0.941f, 0.965f, 0.941f, 1f);
        chunk.style.borderBottomColor = new Color(0.941f, 0.965f, 0.941f, 1f);
        chunk.style.borderLeftWidth = 1f;
        chunk.style.borderRightWidth = 1f;
        chunk.style.borderTopWidth = 1f;
        chunk.style.borderBottomWidth = 1f;
        chunk.style.transformOrigin = new StyleTransformOrigin(new TransformOrigin(Length.Percent(50), Length.Percent(50)));

        lifeTrack.Add(chunk);

        float sign = (chunkCounter++ % 2 == 0) ? 1f : -1f;
        activeDamageChunks.Add(new ActiveDamageChunk
        {
            element = chunk,
            elapsed = 0f,
            sign = sign
        });
    }

    private void UpdateDamageChunks()
    {
        if (activeDamageChunks.Count == 0) return;

        for (int i = activeDamageChunks.Count - 1; i >= 0; i--)
        {
            var chunk = activeDamageChunks[i];
            chunk.elapsed += Time.deltaTime;
            float t = chunk.elapsed;

            if (t >= ChunkDuration)
            {
                chunk.element.RemoveFromHierarchy();
                activeDamageChunks.RemoveAt(i);
                continue;
            }

            if (t < ChunkPhase1Duration)
            {
                // Phase 1 (0.0s to 0.15s):
                // Comes out of the lifebar (translate downwards 8-14px)
                // with Z-axis rotation shake (+-6 to +-12 degrees)
                float p1 = t / ChunkPhase1Duration;
                float easeY = 1f - Mathf.Pow(1f - p1, 2f);
                float y = ChunkHudPhase1Drop * easeY;

                float osc = Mathf.Sin(t * 48f);
                float amp = Mathf.Lerp(11f, 7f, p1);
                float angle = osc * amp * chunk.sign;

                chunk.element.style.translate = new Translate(0f, y);
                chunk.element.style.rotate = new Rotate(Angle.Degrees(angle));
                chunk.element.style.opacity = 1f;
                chunk.element.style.scale = new Scale(Vector2.one);
            }
            else
            {
                // Phase 2 (0.15s to 0.45s):
                // Slight shake/tilt in Z while dropping a bit more and fading/shrinking vertically
                float p2 = (t - ChunkPhase1Duration) / (ChunkDuration - ChunkPhase1Duration);
                float extra = ChunkHudExtraDrop * (p2 * p2);
                float y = ChunkHudPhase1Drop + extra;

                float decay = 1f - p2;
                float osc = Mathf.Sin(t * 32f);
                float jitterAmp = Mathf.Lerp(5f, 1f, p2);
                float baseTilt = 3.5f * chunk.sign;
                float angle = (osc * jitterAmp + baseTilt) * decay;

                float opacity = Mathf.Clamp01(1f - p2);
                float scaleY = Mathf.Clamp01(1f - p2 * 0.75f);

                chunk.element.style.translate = new Translate(0f, y);
                chunk.element.style.rotate = new Rotate(Angle.Degrees(angle));
                chunk.element.style.opacity = opacity;
                chunk.element.style.scale = new Scale(new Vector2(1f, scaleY));
            }
        }
    }

    private void ClearDamageChunks()
    {
        for (int i = 0; i < activeDamageChunks.Count; i++)
        {
            activeDamageChunks[i].element.RemoveFromHierarchy();
        }
        activeDamageChunks.Clear();
    }

    private void UpdateLife()
    {
        if (lifeFill != null && !Mathf.Approximately(currentLife, targetLife))
        {
            currentLife = Mathf.MoveTowards(currentLife, targetLife, LifeTransitionSpeed * Time.deltaTime);
            lifeFill.style.width = Length.Percent(currentLife * 100f);
        }
    }

    private void UpdateLifebarShake()
    {
        if (lifebarElement == null) return;

        if (lifebarShakeTimer > 0f)
        {
            lifebarShakeTimer -= Time.deltaTime;
            if (lifebarShakeTimer > 0f)
            {
                // Max 1.5px - 2.0px offset (1.75px amplitude)
                float offsetX = (Mathf.PerlinNoise(Time.time * 50f, 0f) - 0.5f) * 2f * LifebarShakeMaxOffset;
                float offsetY = (Mathf.PerlinNoise(0f, Time.time * 50f) - 0.5f) * 2f * LifebarShakeMaxOffset;
                lifebarElement.style.translate = new Translate(offsetX, offsetY);
            }
            else
            {
                lifebarElement.style.translate = new Translate(0f, 0f);
            }
        }
    }

    void Update()
    {
        showTowerSlotAnimation();
        ShowDeck();
        UpdateWaveDisplay();
        UpdateScoreRoll();
        UpdateLife();
        UpdateLifebarShake();
        UpdateDamageChunks();
    }

    void showTowerSlotAnimation()
    {
        if (TowerSlotAnimation == null) return;

        // Position is driven by the dragged Card (snapped to the placement grid when applicable)
        if (TowerSlotAnimation.activeSelf != ShowTowerSlot)
            TowerSlotAnimation.SetActive(ShowTowerSlot);
    }

    // Right click on a hand card: toggles its detail panel (single reusable instance)
    public void ToggleCardBox(Card card, string _name, string _description)
    {
        if (!EnsureCardPanel()) return;

        if (cardPanel.Owner == card)
        {
            cardPanel.Hide(card);
            return;
        }

        ShowBoxText.Post(gameObject);
        cardPanel.Show(card, _name, _description, card.View);
    }

    public void HideCardBox(Card card = null)
    {
        if (cardPanel != null)
            cardPanel.Hide(card);
    }

    // The panel lives next to GameUIPanel (UI/Game.uxml)
    bool EnsureCardPanel()
    {
        if (cardPanel == null)
            cardPanel = FindAnyObjectByType<ChangeCardText>();
        return cardPanel != null;
    }

    public void ShowDeck()
    {
        if (CardDrop.instance == null) return;

        // Shown while it holds something playable in this phase (towers in preparation, allies during a wave)
        bool playable = CardDrop.instance.HasPlayableCard;
        if (!CardDrop.instance.DeckShown && playable)
        {
            CardDrop.instance.ShowDeck();
        }
        else if (CardDrop.instance.DeckShown && !playable)
        {
            CardDrop.instance.HideDeck();
        }
    }
}
