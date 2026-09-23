using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Every card, enemy and wave number in one window: Bunker > Balance.
// Rows are the real assets (edits are undoable and saved like any inspector change).
public class BalanceWindow : EditorWindow
{
    enum Tab { Cards, Enemies, WavesAndLoot }

    static readonly (Type type, string title)[] CardSections =
    {
        (typeof(TowerCardDefinition), "Towers"),
        (typeof(ArtilleryTowerCardDefinition), "Artillery towers"),
        (typeof(BuffCardDefinition), "Tower buffs"),
        (typeof(HealCardDefinition), "Bunker heals"),
        (typeof(LandMineCardDefinition), "Land mines"),
        (typeof(AirStrikeCardDefinition), "Air strikes"),
    };

    // Long or rarely tuned fields stay in the regular inspector (click the row name)
    static readonly HashSet<string> HiddenColumns = new HashSet<string> { "m_Script", "description" };
    static readonly HashSet<string> PrefabColumns = new HashSet<string> { "handPrefab", "placedPrefab" };

    Tab tab;
    Vector2 scroll;
    bool showPrefabs;
    readonly Dictionary<UnityEngine.Object, SerializedObject> serialized = new Dictionary<UnityEngine.Object, SerializedObject>();
    Editor waveEditor, catalogEditor;

    [MenuItem("Bunker/Balance")]
    static void Open() => GetWindow<BalanceWindow>("Balance");

    void OnFocus() => serialized.Clear();
    void OnProjectChange() { serialized.Clear(); Repaint(); }

    void OnDisable()
    {
        if (waveEditor != null) DestroyImmediate(waveEditor);
        if (catalogEditor != null) DestroyImmediate(catalogEditor);
    }

    void OnGUI()
    {
        tab = (Tab)GUILayout.Toolbar((int)tab, new[] { "Cards", "Enemies", "Waves & Loot" });
        EditorGUILayout.Space(4);
        scroll = EditorGUILayout.BeginScrollView(scroll);
        switch (tab)
        {
            case Tab.Cards: DrawCards(); break;
            case Tab.Enemies: DrawEnemies(); break;
            case Tab.WavesAndLoot: DrawWavesAndLoot(); break;
        }
        EditorGUILayout.EndScrollView();
    }

    // ---------------------------------------------------------------- cards

    void DrawCards()
    {
        var catalog = FindAssets<CardCatalog>().FirstOrDefault();
        var all = FindAssets<CardDefinition>();
        int totalWeight = catalog != null ? catalog.TotalWeight() : 0;

        using (new EditorGUILayout.HorizontalScope())
        {
            showPrefabs = GUILayout.Toggle(showPrefabs, "Show prefab columns", EditorStyles.toolbarButton, GUILayout.Width(150));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70))) serialized.Clear();
        }

        DrawCardWarnings(all, catalog);

        foreach (var (type, title) in CardSections)
        {
            var rows = all.Where(d => d.GetType() == type).Cast<UnityEngine.Object>().ToList();
            if (rows.Count == 0) continue;

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            DrawTable(rows, (row, _) =>
            {
                var def = (CardDefinition)row;
                bool inCatalog = catalog != null && catalog.cards.Contains(def);
                string chance = !inCatalog ? "—" : totalWeight > 0 ? $"{100f * def.dropWeight / totalWeight:0.#}%" : "0%";
                GUILayout.Label(new GUIContent(chance, inCatalog ? "Share of card drops" : "Not in the catalog: never drops"), GUILayout.Width(50));
            }, "Drop %");
        }
    }

    void DrawCardWarnings(List<CardDefinition> all, CardCatalog catalog)
    {
        var warnings = new List<string>();
        if (catalog == null) warnings.Add("No CardCatalog asset: kills drop nothing and there is no starting hand.");
        foreach (var def in all)
        {
            if (def.handPrefab == null) { warnings.Add($"{def.name}: no hand prefab."); continue; }
            var card = def.handPrefab.GetComponent<Card>();
            if (card == null || card.definition != def) warnings.Add($"{def.name}: its hand prefab points to '{(card != null && card.definition != null ? card.definition.name : "nothing")}'.");
            if (catalog != null && !catalog.cards.Contains(def)) warnings.Add($"{def.name}: not in the CardCatalog (never drops).");
            if (def is TowerCardDefinition && (def.placedPrefab == null || def.placedPrefab.GetComponent<TurretCard>() == null))
                warnings.Add($"{def.name}: placed prefab needs a TurretCard.");
            if (def is ArtilleryTowerCardDefinition && def.placedPrefab != null && def.placedPrefab.GetComponent<ArtilleryAuthoring>() == null)
                warnings.Add($"{def.name}: placed prefab needs an ArtilleryAuthoring.");
            if ((def is LandMineCardDefinition || def is AirStrikeCardDefinition) && def.placedPrefab == null)
                warnings.Add($"{def.name}: no placed prefab.");
        }
        if (warnings.Count > 0)
            EditorGUILayout.HelpBox(string.Join("\n", warnings), MessageType.Warning);
    }

    // ---------------------------------------------------------------- enemies

    void DrawEnemies()
    {
        var roster = FindRoster();
        var wave = FindAssets<WaveBalanceConfig>().FirstOrDefault();
        var rows = roster.Count > 0
            ? roster.Cast<UnityEngine.Object>().ToList()
            : FindAssets<EnemyData>().Cast<UnityEngine.Object>().ToList();

        EditorGUILayout.HelpBox(roster.Count > 0
            ? "Roster order from the open scene's EnemySpawner (unlock order). # is the roster index."
            : "Open SceneGame to see the roster order and the boss.", MessageType.None);

        DrawTable(rows, (row, index) =>
        {
            string label = roster.Count > 0 ? index.ToString() : "";
            if (roster.Count > 0 && wave != null && wave.bossEveryWaves > 0 && index == wave.bossTypeIndex) label += " boss";
            GUILayout.Label(label, GUILayout.Width(50));
        }, "#");
    }

    static List<EnemyData> FindRoster()
    {
        var spawner = FindFirstObjectByType<EnemySpawner>();
        if (spawner == null || spawner.Enemies == null) return new List<EnemyData>();
        return spawner.Enemies
            .Select(p => p != null && p.TryGetComponent(out Enemy e) ? e.Data : null)
            .Where(d => d != null)
            .ToList();
    }

    // ---------------------------------------------------------------- waves & loot

    void DrawWavesAndLoot()
    {
        var wave = FindAssets<WaveBalanceConfig>().FirstOrDefault();
        var catalog = FindAssets<CardCatalog>().FirstOrDefault();

        EditorGUILayout.LabelField("Waves", EditorStyles.boldLabel);
        if (wave != null) DrawInline(wave, ref waveEditor);
        else EditorGUILayout.HelpBox("No WaveBalanceConfig asset.", MessageType.Warning);

        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("Card catalog (drops + starting hand)", EditorStyles.boldLabel);
        if (catalog == null)
        {
            EditorGUILayout.HelpBox("No CardCatalog asset.", MessageType.Warning);
            return;
        }
        if (GUILayout.Button("Add every CardDefinition in the project to the drop pool"))
        {
            Undo.RecordObject(catalog, "Sync card catalog");
            foreach (var def in FindAssets<CardDefinition>())
                if (!catalog.cards.Contains(def)) catalog.cards.Add(def);
            catalog.cards.RemoveAll(c => c == null);
            EditorUtility.SetDirty(catalog);
        }
        DrawInline(catalog, ref catalogEditor);
    }

    static void DrawInline(UnityEngine.Object target, ref Editor cached)
    {
        Editor.CreateCachedEditor(target, null, ref cached);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            cached.OnInspectorGUI();
    }

    // ---------------------------------------------------------------- generic table

    // One row per asset, one column per visible serialized field; `extra` draws a computed first column
    void DrawTable(List<UnityEngine.Object> rows, Action<UnityEngine.Object, int> extra, string extraHeader)
    {
        if (rows.Count == 0)
        {
            EditorGUILayout.LabelField("Nothing found.");
            return;
        }

        var columns = Columns(Serialized(rows[0]));

        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label("Asset", EditorStyles.miniBoldLabel, GUILayout.Width(130));
            GUILayout.Label(extraHeader, EditorStyles.miniBoldLabel, GUILayout.Width(50));
            foreach (var (path, label, tooltip, width) in columns)
                GUILayout.Label(new GUIContent(label, tooltip), EditorStyles.miniBoldLabel, GUILayout.Width(width));
        }

        for (int i = 0; i < rows.Count; i++)
        {
            var so = Serialized(rows[i]);
            so.Update();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent(rows[i].name, "Select the asset"), EditorStyles.linkLabel, GUILayout.Width(130)))
                {
                    Selection.activeObject = rows[i];
                    EditorGUIUtility.PingObject(rows[i]);
                }
                extra(rows[i], i);
                foreach (var (path, _, _, width) in columns)
                {
                    var prop = so.FindProperty(path);
                    if (prop != null)
                        EditorGUILayout.PropertyField(prop, GUIContent.none, GUILayout.Width(width));
                    else
                        GUILayout.Space(width + 4);
                }
            }
            so.ApplyModifiedProperties();
        }
    }

    List<(string path, string label, string tooltip, float width)> Columns(SerializedObject so)
    {
        var columns = new List<(string, string, string, float)>();
        var it = so.GetIterator();
        for (bool enter = true; it.NextVisible(enter); enter = false)
        {
            if (HiddenColumns.Contains(it.name)) continue;
            if (!showPrefabs && PrefabColumns.Contains(it.name)) continue;
            columns.Add((it.propertyPath, it.displayName, string.IsNullOrEmpty(it.tooltip) ? it.displayName : it.tooltip, Width(it)));
        }
        return columns;
    }

    static float Width(SerializedProperty prop)
    {
        switch (prop.propertyType)
        {
            case SerializedPropertyType.String: return 130;
            case SerializedPropertyType.ObjectReference: return 140;
            case SerializedPropertyType.Enum: return 100;
            case SerializedPropertyType.Boolean: return 20;
            case SerializedPropertyType.Float:
            case SerializedPropertyType.Integer:
                // Range sliders need room; plain numbers stay narrow
                return prop.name.ToLowerInvariant().Contains("range") ? 150 : 60;
            default: return 120;
        }
    }

    SerializedObject Serialized(UnityEngine.Object target)
    {
        if (!serialized.TryGetValue(target, out var so) || so.targetObject == null)
            serialized[target] = so = new SerializedObject(target);
        return so;
    }

    static List<T> FindAssets<T>() where T : UnityEngine.Object =>
        AssetDatabase.FindAssets($"t:{typeof(T).Name}")
            .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(a => a != null)
            .OrderBy(a => a.name)
            .ToList();
}
