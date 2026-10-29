using UnityEngine;
using UnityEngine.UIElements;

// UI that belongs to the play field (hand, deck, card info, life bars, damage numbers; UI/Game.uxml).
// The panel renders into a texture laid over the camera image before the camera's image effects,
// so it gets the same CRT look as the map. One panel unit is one unit of the old hand canvas that hung
// from the camera, so the hand keeps its size relative to the map whatever the zoom.
[RequireComponent(typeof(UIDocument))]
public class GameUIPanel : MonoBehaviour
{
    public static GameUIPanel Instance { get; private set; }

    [SerializeField] Shader compositeShader;
    // World size of one panel unit: the old camera canvas spread 245 units across 16 world units
    const float WorldUnitsPerPanelUnit = 16f / 245f;

    UIDocument document;
    RenderTexture target;
    Material composite;

    public VisualElement Root => document.rootVisualElement;
    public VisualElement Hand => Root.Q("hand");
    public VisualElement World => Root.Q("world");

    void Awake()
    {
        Instance = this;
        document = GetComponent<UIDocument>();
        // Own copy: the target texture must not end up in the shared asset
        var settings = Instantiate(document.panelSettings);
        settings.scaleMode = PanelScaleMode.ConstantPixelSize;
        settings.SetScreenToPanelSpaceFunction(ScreenToTexture);
        document.panelSettings = settings;
        composite = new Material(compositeShader);
        Resize();
    }

    void OnEnable()
    {
        Root.pickingMode = PickingMode.Ignore;
        Camera.onPostRender += Composite;
    }

    void OnDisable() => Camera.onPostRender -= Composite;

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (target != null) target.Release();
        Destroy(composite);
    }

    void LateUpdate()
    {
        if (target.width != Screen.width || target.height != Screen.height)
            Resize();

        var cam = Camera.main;
        if (cam != null && cam.orthographic)
            document.panelSettings.scale = target.height * WorldUnitsPerPanelUnit / (2f * cam.orthographicSize);
    }

    void Resize()
    {
        if (target != null) target.Release();
        target = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32) { name = "GameUI" };
        document.panelSettings.targetTexture = target;
    }

    // Pointer input arrives in screen pixels (y down); the texture covers the screen one to one
    Vector2 ScreenToTexture(Vector2 screen) =>
        target == null ? screen : new Vector2(screen.x * target.width / Screen.width, screen.y * target.height / Screen.height);

    // OnPostRender runs before OnRenderImage, so the CRT filter processes the UI with the map
    void Composite(Camera cam)
    {
        if (cam != Camera.main || target == null) return;
        composite.mainTexture = target;
        GL.PushMatrix();
        GL.LoadOrtho();
        composite.SetPass(0);
        GL.Begin(GL.QUADS);
        GL.TexCoord2(0, 0); GL.Vertex3(0, 0, 0);
        GL.TexCoord2(0, 1); GL.Vertex3(0, 1, 0);
        GL.TexCoord2(1, 1); GL.Vertex3(1, 1, 0);
        GL.TexCoord2(1, 0); GL.Vertex3(1, 0, 0);
        GL.End();
        GL.PopMatrix();
    }

    // Panel position of a world point
    public Vector2 WorldToPanel(Vector3 world) =>
        RuntimePanelUtils.CameraTransformWorldToPanel(Root.panel, world, Camera.main);

    // Panel units covered by one world unit at the current zoom
    public float UnitsPerWorld => Vector2.Distance(WorldToPanel(Vector3.zero), WorldToPanel(Vector3.up));
}
