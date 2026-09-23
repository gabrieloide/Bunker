using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

// Every card, enemy and wave number in one window: Bunker > Balance.
// Cells are bound to the real assets, so edits are undoable and saved like any inspector change.
public class BalanceWindow : EditorWindow
{
    const string CardFolder = "Assets/ScriptableObjects/CardDefinitions";
    // Drop % edits rewrite the weights so they add up to this (0.1% resolution)
    const int WeightTotal = 1000;

    enum Section { Towers, Artillery, Buffs, Heals, Mines, AirStrikes, Enemies, Waves, Loot }

    struct SectionInfo
    {
        public string Title, Subtitle, Group;
        public Type CardType;
    }

    static readonly Dictionary<Section, SectionInfo> Sections = new Dictionary<Section, SectionInfo>
    {
        { Section.Towers, new SectionInfo { Group = "CARDS", Title = "Towers", CardType = typeof(TowerCardDefinition), Subtitle = "Towers that shoot straight bullets. DPS = damage × burst × rate." } },
        { Section.Artillery, new SectionInfo { Group = "CARDS", Title = "Artillery", CardType = typeof(ArtilleryTowerCardDefinition), Subtitle = "Towers that lob shells with splash damage. The placed prefab needs an ArtilleryAuthoring." } },
        { Section.Buffs, new SectionInfo { Group = "CARDS", Title = "Tower buffs", CardType = typeof(BuffCardDefinition), Subtitle = "Attack / Speed multiply the stat; Bullet Pen adds flat armor penetration." } },
        { Section.Heals, new SectionInfo { Group = "CARDS", Title = "Bunker heals", CardType = typeof(HealCardDefinition), Subtitle = "Restore bunker life when played." } },
        { Section.Mines, new SectionInfo { Group = "CARDS", Title = "Land mines", CardType = typeof(LandMineCardDefinition), Subtitle = "Explode on the first enemy that steps on them." } },
        { Section.AirStrikes, new SectionInfo { Group = "CARDS", Title = "Air strikes", CardType = typeof(AirStrikeCardDefinition), Subtitle = "A plane crosses the screen and fires a volley." } },
        { Section.Enemies, new SectionInfo { Group = "ENEMIES", Title = "Enemies", Subtitle = "Roster order = unlock order (read from the open scene's EnemySpawner). DPS = damage ÷ shot interval." } },
        { Section.Waves, new SectionInfo { Group = "RULES", Title = "Waves", Subtitle = "Spawning, progression, bosses and the random enemy buff of each wave." } },
        { Section.Loot, new SectionInfo { Group = "RULES", Title = "Loot & starting hand", Subtitle = "How often kills drop a card, which card comes out, and the hand you start with." } },
    };

    // Short headers; the field's tooltip is shown on hover
    static readonly Dictionary<string, (string label, float width)> ColumnLabels = new Dictionary<string, (string, float)>
    {
        { "displayName", ("Name", 124) }, { "dropWeight", ("Weight", 58) },
        { "handPrefab", ("Hand prefab", 150) }, { "placedPrefab", ("Placed prefab", 150) },
        { "life", ("Life", 58) }, { "damage", ("Damage", 62) }, { "bulletPen", ("Pen", 50) },
        { "fireRate", ("Rate /s", 60) }, { "burstCount", ("Burst", 52) }, { "burstInterval", ("Burst gap", 70) },
        { "range", ("Range", 56) }, { "initialCooldown", ("1st shot", 62) },
        { "splashRadius", ("Splash", 60) }, { "riseTime", ("Rise", 56) }, { "fallTime", ("Fall", 56) }, { "height", ("Apex", 56) },
        { "buffType", ("Stat", 130) }, { "amount", ("Amount", 70) }, { "healAmount", ("Heal", 70) },
        { "shotCount", ("Shots", 60) }, { "shotInterval", ("Shot gap", 70) },
        { "_life", ("Life", 64) }, { "_damage", ("Damage", 64) }, { "_attackInterval", ("Shot every", 80) },
        { "_attackRange", ("Range", 60) }, { "_defense", ("Armor", 60) }, { "_moveSpeed", ("Speed", 60) }, { "Score", ("Score", 60) },
    };

    static readonly HashSet<string> HiddenFields = new HashSet<string> { "m_Script", "description" };
    static readonly HashSet<string> PrefabFields = new HashSet<string> { "handPrefab", "placedPrefab" };

    class Row
    {
        public Object Asset;
        public SerializedObject So;
        public int RosterIndex = -1;
    }

    [SerializeField] Section section = Section.Towers;
    [SerializeField] bool showPrefabs;
    string search = "";

    VisualElement nav, content;
    readonly Dictionary<Section, VisualElement> navItems = new Dictionary<Section, VisualElement>();
    // Read-only cells (DPS, drop %) recomputed on a timer so they follow edits made anywhere
    readonly HashSet<VisualElement> liveCells = new HashSet<VisualElement>();
    readonly Dictionary<Column, Comparison<Row>> comparers = new Dictionary<Column, Comparison<Row>>();

    [MenuItem("Bunker/Balance")]
    static void Open()
    {
        var window = GetWindow<BalanceWindow>();
        window.minSize = new Vector2(760, 420);
    }

    void OnProjectChange() => Rebuild();
    void OnFocus() { if (content != null) RefreshNav(); }

    void CreateGUI()
    {
        titleContent = new GUIContent("Balance");
        comparers.Clear();
        var root = rootVisualElement;
        var sheet = AssetDatabase.FindAssets("BalanceWindow t:StyleSheet")
            .Select(g => AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetDatabase.GUIDToAssetPath(g)))
            .FirstOrDefault(s => s != null);
        if (sheet != null) root.styleSheets.Add(sheet);

        var shell = new VisualElement();
        shell.AddToClassList("root");
        root.Add(shell);

        nav = new VisualElement();
        nav.AddToClassList("sidebar");
        shell.Add(nav);

        content = new VisualElement();
        content.AddToClassList("content");
        shell.Add(content);

        BuildNav();
        Rebuild();
        root.schedule.Execute(UpdateLiveCells).Every(250);
    }

    // ---------------------------------------------------------------- sidebar

    void BuildNav()
    {
        nav.Clear();
        navItems.Clear();
        nav.Add(Label("Balance", "sidebar__title"));

        string group = null;
        foreach (var pair in Sections)
        {
            if (pair.Value.Group != group)
            {
                group = pair.Value.Group;
                nav.Add(Label(group, "sidebar__group"));
            }

            var s = pair.Key;
            var item = new VisualElement();
            item.AddToClassList("nav-item");
            item.Add(Label(pair.Value.Title, "nav-item__label"));
            var count = Label("", "nav-item__count");
            count.name = "count";
            item.Add(count);
            var warn = new VisualElement { name = "warn" };
            warn.AddToClassList("nav-item__warn");
            item.Add(warn);
            item.RegisterCallback<ClickEvent>(_ => { section = s; search = ""; Rebuild(); });
            nav.Add(item);
            navItems[s] = item;
        }
        RefreshNav();
    }

    void RefreshNav()
    {
        var cards = FindAssets<CardDefinition>();
        var warnings = CardWarnings(cards, Catalog());
        foreach (var pair in navItems)
        {
            var info = Sections[pair.Key];
            int count = info.CardType != null ? cards.Count(c => c.GetType() == info.CardType)
                : pair.Key == Section.Enemies ? FindAssets<EnemyData>().Count : -1;
            var countLabel = pair.Value.Q<Label>("count");
            countLabel.text = count.ToString();
            countLabel.style.display = count >= 0 ? DisplayStyle.Flex : DisplayStyle.None;

            bool warn = info.CardType != null && warnings.Any(w => w.asset != null && w.asset.GetType() == info.CardType)
                        || pair.Key == Section.Loot && warnings.Any(w => w.asset == null || w.asset is CardCatalog);
            pair.Value.Q("warn").style.display = warn ? DisplayStyle.Flex : DisplayStyle.None;
            pair.Value.EnableInClassList("nav-item--selected", pair.Key == section);
        }
    }

    // ---------------------------------------------------------------- content

    void Rebuild()
    {
        if (content == null) return;
        content.Clear();
        liveCells.Clear();
        RefreshNav();

        var info = Sections[section];
        content.Add(Label(info.Title, "header__title"));
        content.Add(Label(info.Subtitle, "header__subtitle"));

        if (info.CardType != null) BuildCardSection(info.CardType);
        else if (section == Section.Enemies) BuildEnemySection();
        else if (section == Section.Waves) BuildWavesSection();
        else BuildLootSection();
    }

    VisualElement Toolbar(bool withSearch)
    {
        var bar = new VisualElement();
        bar.AddToClassList("toolbar");
        if (withSearch)
        {
            var field = new ToolbarSearchField { value = search };
            field.AddToClassList("toolbar__search");
            field.RegisterValueChangedCallback(e => { search = e.newValue; RebuildTableOnly(); });
            bar.Add(field);
        }
        bar.Add(Spacer());
        return bar;
    }

    VisualElement tableHost;
    Action rebuildTable;

    void RebuildTableOnly()
    {
        liveCells.Clear();
        rebuildTable?.Invoke();
    }

    // ---------------------------------------------------------------- cards

    void BuildCardSection(Type cardType)
    {
        var bar = Toolbar(true);
        var prefabs = new Toggle { text = "Prefab columns", value = showPrefabs };
        prefabs.AddToClassList("toolbar__toggle");
        prefabs.RegisterValueChangedCallback(e => { showPrefabs = e.newValue; RebuildTableOnly(); });
        bar.Add(prefabs);
        bar.Add(Button("+ New card", () => CreateCard(cardType)));
        content.Add(bar);

        var catalog = Catalog();
        AddWarnings(CardWarnings(FindAssets<CardDefinition>(), catalog).Where(w => w.asset != null && w.asset.GetType() == cardType).ToList());

        tableHost = new VisualElement { style = { flexGrow = 1 } };
        content.Add(tableHost);
        rebuildTable = () =>
        {
            tableHost.Clear();
            var rows = FindAssets<CardDefinition>()
                .Where(d => d.GetType() == cardType && Matches(d))
                .Select(d => new Row { Asset = d, So = new SerializedObject(d) })
                .ToList();

            var extra = new List<Column>();
            if (typeof(TowerCardDefinition).IsAssignableFrom(cardType))
                extra.Add(DerivedColumn(rows, "DPS", "Damage per second at full uptime (damage × burst × rate)", 56,
                    row => { var t = (TowerCardDefinition)row.Asset; return (t.damage * t.burstCount * t.fireRate).ToString("0.#"); },
                    row => { var t = (TowerCardDefinition)row.Asset; return t.damage * t.burstCount * t.fireRate; }));
            if (cardType == typeof(AirStrikeCardDefinition))
                extra.Add(DerivedColumn(rows, "Total", "Damage of the whole volley (damage × shots)", 60,
                    row => { var a = (AirStrikeCardDefinition)row.Asset; return (a.damage * a.shotCount).ToString("0.#"); },
                    row => { var a = (AirStrikeCardDefinition)row.Asset; return a.damage * a.shotCount; }));

            tableHost.Add(BuildTable(rows, first: DropColumn(catalog, rows), extra: extra));
        };
        rebuildTable();
    }

    void CreateCard(Type cardType)
    {
        if (!AssetDatabase.IsValidFolder(CardFolder))
            AssetDatabase.CreateFolder("Assets/ScriptableObjects", "CardDefinitions");
        var def = (CardDefinition)CreateInstance(cardType);
        def.displayName = "New card";
        AssetDatabase.CreateAsset(def, AssetDatabase.GenerateUniqueAssetPath($"{CardFolder}/New {Sections[section].Title}.asset"));

        var catalog = Catalog();
        if (catalog != null)
        {
            Undo.RecordObject(catalog, "Add card to catalog");
            catalog.cards.Add(def);
            EditorUtility.SetDirty(catalog);
        }
        AssetDatabase.SaveAssets();
        Selection.activeObject = def;
        Rebuild();
    }

    // ---------------------------------------------------------------- enemies

    void BuildEnemySection()
    {
        content.Add(Toolbar(true));
        var wave = FindAssets<WaveBalanceConfig>().FirstOrDefault();
        var roster = FindRoster();
        if (roster.Count == 0)
            AddWarnings(new List<(string, Object)> { ("Open SceneGame to see the roster order and which enemy is the boss.", null) });

        tableHost = new VisualElement { style = { flexGrow = 1 } };
        content.Add(tableHost);
        rebuildTable = () =>
        {
            tableHost.Clear();
            var rows = (roster.Count > 0 ? roster : FindAssets<EnemyData>())
                .Select((d, i) => new Row { Asset = d, So = new SerializedObject(d), RosterIndex = roster.Count > 0 ? i : -1 })
                .Where(r => Matches(r.Asset))
                .ToList();

            var order = new Column
            {
                name = "order", title = "#", width = 70, resizable = false, makeHeader = Header("#", "Roster index (unlock order)"),
                makeCell = () =>
                {
                    var cell = CellRow();
                    var index = Label("", "cell--derived");
                    index.style.flexGrow = 1;
                    cell.Add(index);
                    var badge = Label("BOSS", "boss-badge");
                    cell.Add(badge);
                    return cell;
                },
            };
            order.bindCell = (e, i) =>
            {
                var row = rows[i];
                e.Q<Label>().text = row.RosterIndex >= 0 ? row.RosterIndex.ToString() : "–";
                bool boss = wave != null && wave.bossEveryWaves > 0 && row.RosterIndex == wave.bossTypeIndex;
                e.Q<Label>(className: "boss-badge").style.display = boss ? DisplayStyle.Flex : DisplayStyle.None;
            };
            SetComparison(order, rows, r => r.RosterIndex);

            var dps = DerivedColumn(rows, "DPS", "Damage per second against a tower (damage ÷ shot interval)", 60,
                row => { var d = (EnemyData)row.Asset; return d.BaseAttackInterval > 0 ? (d.BaseDamage / d.BaseAttackInterval).ToString("0.##") : "–"; },
                row => { var d = (EnemyData)row.Asset; return d.BaseAttackInterval > 0 ? d.BaseDamage / d.BaseAttackInterval : 0f; });

            tableHost.Add(BuildTable(rows, first: order, extra: new List<Column> { dps }));
        };
        rebuildTable();
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

    // ---------------------------------------------------------------- waves

    void BuildWavesSection()
    {
        var wave = FindAssets<WaveBalanceConfig>().FirstOrDefault();
        if (wave == null)
        {
            AddWarnings(new List<(string, Object)> { ("No WaveBalanceConfig asset found.", null) });
            return;
        }

        var scroll = new ScrollView();
        scroll.AddToClassList("scroll");
        content.Add(scroll);

        var panel = Panel("Wave Balance Config", "Every field has a tooltip. Enemy life per wave = base × (1 + growth × (wave − 1)).");
        var so = new SerializedObject(wave);
        var it = so.GetIterator();
        for (bool enter = true; it.NextVisible(enter); enter = false)
            if (it.name != "m_Script") panel.Add(new PropertyField(it.Copy()));
        panel.Bind(so);
        scroll.Add(panel);
    }

    // ---------------------------------------------------------------- loot

    void BuildLootSection()
    {
        var catalog = Catalog();
        if (catalog == null)
        {
            AddWarnings(new List<(string, Object)> { ("No CardCatalog asset: kills drop nothing and there is no starting hand.", null) });
            return;
        }
        AddWarnings(CardWarnings(FindAssets<CardDefinition>(), catalog).Where(w => w.asset is CardDefinition d && !catalog.cards.Contains(d)).ToList());

        var so = new SerializedObject(catalog);
        var scroll = new ScrollView();
        scroll.AddToClassList("scroll");
        content.Add(scroll);

        var chance = Panel("Drop chance", "Chance that a kill drops a card. It grows as the bunker loses life, so a struggling player gets more cards.");
        chance.Add(Bound(new Slider("At full life", 0f, 1f) { showInputField = true }, so, "baseDropChance"));
        chance.Add(Bound(new Slider("Extra per 10 life lost", 0f, 0.2f) { showInputField = true }, so, "dropChancePerMissingTenLife"));
        var example = Label("", "panel__hint");
        example.userData = (Func<string>)(() =>
            $"→ {catalog.baseDropChance * 100f:0.#}% at full life, {Mathf.Clamp01(catalog.baseDropChance + 5 * catalog.dropChancePerMissingTenLife) * 100f:0.#}% at half life, {Mathf.Clamp01(catalog.baseDropChance + 9 * catalog.dropChancePerMissingTenLife) * 100f:0.#}% at 10 life.");
        liveCells.Add(example);
        chance.Add(example);
        scroll.Add(chance);

        var table = Panel("Which card drops", "Type a Drop % and the other cards rescale to keep their proportions. Weight is the raw value behind it.");
        var actions = new VisualElement();
        actions.AddToClassList("panel__row");
        actions.Add(Button("Split evenly", () => EqualizeWeights(catalog), "Give every card in the pool the same drop chance"));
        actions.Add(Button("Add missing cards", () => SyncCatalog(catalog), "Add every CardDefinition in the project to the drop pool"));
        actions.Add(Spacer());
        table.Add(actions);

        var rows = catalog.cards.Where(c => c != null).Distinct()
            .Select(c => new Row { Asset = c, So = new SerializedObject(c) })
            .ToList();
        var typeCol = DerivedColumn(rows, "Type", "Card type", 110, row => Sections.Values.First(s => s.CardType == row.Asset.GetType()).Title, row => 0f);
        var list = BuildTable(rows, first: DropColumn(catalog, rows), extra: new List<Column> { typeCol }, onlyFields: new[] { "displayName", "dropWeight" });
        list.AddToClassList("drop-table");
        table.Add(list);
        scroll.Add(table);

        var hand = Panel("Starting hand", "Dealt in this order when a run starts.");
        hand.Add(new PropertyField(so.FindProperty("startingHand"), "Cards"));
        hand.Bind(so);
        scroll.Add(hand);
    }

    void EqualizeWeights(CardCatalog catalog)
    {
        var pool = catalog.cards.Where(c => c != null).Distinct().ToList();
        if (pool.Count == 0) return;
        Undo.RecordObjects(pool.Cast<Object>().ToArray(), "Split drop chance evenly");
        foreach (var card in pool)
        {
            card.dropWeight = WeightTotal / pool.Count;
            EditorUtility.SetDirty(card);
        }
    }

    void SyncCatalog(CardCatalog catalog)
    {
        Undo.RecordObject(catalog, "Sync card catalog");
        foreach (var def in FindAssets<CardDefinition>())
            if (!catalog.cards.Contains(def)) catalog.cards.Add(def);
        catalog.cards.RemoveAll(c => c == null);
        EditorUtility.SetDirty(catalog);
        Rebuild();
    }

    // Sets one card's share of the drops and rescales the rest so their proportions stay the same
    static void SetDropPercent(CardCatalog catalog, CardDefinition target, float percent)
    {
        var pool = catalog.cards.Where(c => c != null).Distinct().ToList();
        if (!pool.Contains(target)) return;
        percent = Mathf.Clamp(percent, 0f, 100f);

        Undo.RecordObjects(pool.Cast<Object>().ToArray(), "Set drop %");
        var others = pool.Where(c => c != target).ToList();
        int targetWeight = others.Count == 0 ? WeightTotal : Mathf.RoundToInt(percent / 100f * WeightTotal);
        int remaining = WeightTotal - targetWeight;
        int othersTotal = others.Sum(c => c.dropWeight);

        target.dropWeight = targetWeight;
        foreach (var card in others)
        {
            card.dropWeight = othersTotal > 0
                ? Mathf.RoundToInt((float)card.dropWeight / othersTotal * remaining)
                : remaining / others.Count;
            EditorUtility.SetDirty(card);
        }
        // Rounding leftovers go to the biggest other card so the typed % stays exact
        var biggest = others.OrderByDescending(c => c.dropWeight).FirstOrDefault();
        if (biggest != null)
            biggest.dropWeight = Mathf.Max(0, biggest.dropWeight + WeightTotal - pool.Sum(c => c.dropWeight));
        EditorUtility.SetDirty(target);
    }

    // ---------------------------------------------------------------- table

    MultiColumnListView BuildTable(List<Row> rows, Column first, List<Column> extra, string[] onlyFields = null)
    {
        var list = new MultiColumnListView
        {
            itemsSource = rows,
            fixedItemHeight = 28,
            showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
            selectionType = SelectionType.Single,
            sortingMode = ColumnSortingMode.Custom,
            reorderable = false,
            showBorder = false,
            horizontalScrollingEnabled = true,
        };
        list.AddToClassList("table");
        // Sized to its rows so short tables do not leave an empty box; shrinks (and scrolls) when the window is small
        list.style.flexGrow = 0;
        list.style.flexShrink = 1;
        list.style.height = 48 + rows.Count * 28;

        float nameWidth = Mathf.Clamp(24 + 7.5f * rows.Select(r => r.Asset.name.Length).DefaultIfEmpty(8).Max(), 90, 190);
        var name = new Column { name = "asset", title = "Asset", width = nameWidth, minWidth = 90, stretchable = false, makeHeader = Header("Asset", "Asset file; click a row to select it", TextAnchor.MiddleLeft) };
        name.makeCell = () =>
        {
            var cell = CellRow();
            cell.Add(Label("", "cell--name"));
            return cell;
        };
        name.bindCell = (e, i) =>
        {
            var label = e.Q<Label>();
            label.text = rows[i].Asset.name;
            label.tooltip = "Click to select the asset (full inspector, description…)";
        };
        SetComparison(name, rows, r => r.Asset.name);
        list.columns.Add(name);
        if (first != null) list.columns.Add(first);

        if (rows.Count > 0)
        {
            var it = rows[0].So.GetIterator();
            for (bool enter = true; it.NextVisible(enter); enter = false)
            {
                if (HiddenFields.Contains(it.name)) continue;
                if (onlyFields != null && !onlyFields.Contains(it.name)) continue;
                if (onlyFields == null && !showPrefabs && PrefabFields.Contains(it.name)) continue;
                list.columns.Add(FieldColumn(rows, it.Copy()));
            }
        }
        foreach (var column in extra) list.columns.Add(column);

        list.selectionChanged += items =>
        {
            var row = items.OfType<Row>().FirstOrDefault();
            if (row != null) Selection.activeObject = row.Asset;
        };
        list.columnSortingChanged += () =>
        {
            var sort = list.sortedColumns.FirstOrDefault();
            if (sort == null) return;
            var column = list.columns.FirstOrDefault(c => c.name == sort.columnName);
            if (column != null && comparers.TryGetValue(column, out var compare))
            {
                rows.Sort(compare);
                if (sort.direction == SortDirection.Descending) rows.Reverse();
                list.RefreshItems();
            }
        };
        return list;
    }

    Column FieldColumn(List<Row> rows, SerializedProperty template)
    {
        string path = template.propertyPath;
        var targetType = rows[0].Asset.GetType();
        var field = FindField(targetType, template.name);
        var (label, width) = ColumnLabels.TryGetValue(template.name, out var known) ? known : (template.displayName, 80f);
        if (template.name == "displayName")
            width = Mathf.Clamp(24 + 7f * rows.Select(r => r.So.FindProperty(path).stringValue?.Length ?? 0).DefaultIfEmpty(8).Max(), 110, 200);
        string tooltip = !string.IsNullOrEmpty(template.tooltip) ? template.tooltip : template.displayName;
        var propertyType = template.propertyType;

        var column = new Column { name = path, title = label, width = width, minWidth = 40 };
        bool isText = propertyType == SerializedPropertyType.String || propertyType == SerializedPropertyType.ObjectReference;
        column.makeHeader = Header(label, tooltip, isText ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter);
        column.makeCell = () =>
        {
            var cell = CellRow();
            cell.Add(MakeField(propertyType, field));
            return cell;
        };
        column.bindCell = (e, i) =>
        {
            var input = (IBindable)e[0];
            input.BindProperty(rows[i].So.FindProperty(path));
        };
        column.unbindCell = (e, i) => e[0].Unbind();

        switch (propertyType)
        {
            case SerializedPropertyType.Float: SetComparison(column, rows, r => r.So.FindProperty(path).floatValue); break;
            case SerializedPropertyType.Integer: SetComparison(column, rows, r => r.So.FindProperty(path).intValue); break;
            case SerializedPropertyType.Enum: SetComparison(column, rows, r => r.So.FindProperty(path).enumValueIndex); break;
            case SerializedPropertyType.String: SetComparison(column, rows, r => r.So.FindProperty(path).stringValue); break;
            default: column.sortable = false; break;
        }
        return column;
    }

    // Plain compact fields (no sliders); Min/Range attributes are still enforced
    static VisualElement MakeField(SerializedPropertyType type, FieldInfo field)
    {
        var min = field?.GetCustomAttribute<MinAttribute>();
        var range = field?.GetCustomAttribute<RangeAttribute>();
        float lo = range != null ? range.min : min != null ? min.min : float.MinValue;
        float hi = range != null ? range.max : float.MaxValue;

        VisualElement element;
        switch (type)
        {
            case SerializedPropertyType.Float:
            {
                var f = new FloatField { isDelayed = true };
                f.RegisterValueChangedCallback(e => { var c = Mathf.Clamp(e.newValue, lo, hi); if (c != e.newValue) f.value = c; });
                element = f;
                break;
            }
            case SerializedPropertyType.Integer:
            {
                var f = new IntegerField { isDelayed = true };
                f.RegisterValueChangedCallback(e => { int c = (int)Mathf.Clamp(e.newValue, lo, hi); if (c != e.newValue) f.value = c; });
                element = f;
                break;
            }
            case SerializedPropertyType.Enum:
                element = new EnumField((Enum)Activator.CreateInstance(field.FieldType));
                break;
            case SerializedPropertyType.ObjectReference:
                element = new ObjectField { objectType = field != null ? field.FieldType : typeof(Object), allowSceneObjects = false };
                break;
            case SerializedPropertyType.String:
                element = new TextField { isDelayed = true };
                break;
            default:
                element = new PropertyField { label = "" };
                break;
        }
        element.AddToClassList("cell");
        if (type == SerializedPropertyType.Float || type == SerializedPropertyType.Integer)
            element.AddToClassList("cell--number");
        return element;
    }

    Column DropColumn(CardCatalog catalog, List<Row> rows)
    {
        var column = new Column { name = "drop", title = "Drop %", width = 112, minWidth = 100 };
        column.makeHeader = Header("Drop %", "Share of card drops. Type a value: the other cards rescale to keep their proportions.");
        column.makeCell = () =>
        {
            var cell = CellRow();
            cell.AddToClassList("drop");
            var track = new VisualElement();
            track.AddToClassList("drop__bar-track");
            var fill = new VisualElement { name = "fill" };
            fill.AddToClassList("drop__bar-fill");
            track.Add(fill);
            cell.Add(track);
            var field = new FloatField { isDelayed = true };
            field.AddToClassList("drop__field");
            field.AddToClassList("cell--number");
            cell.Add(field);
            cell.Add(Label("%", "drop__suffix"));
            return cell;
        };
        column.bindCell = (e, i) =>
        {
            var def = (CardDefinition)rows[i].Asset;
            var field = e.Q<FloatField>();
            field.userData = def;
            field.RegisterValueChangedCallback(OnDropEdited);
            liveCells.Add(e);
            UpdateDropCell(e, def, catalog);
        };
        column.unbindCell = (e, i) =>
        {
            e.Q<FloatField>().UnregisterValueChangedCallback(OnDropEdited);
            liveCells.Remove(e);
        };
        column.sortable = true;
        comparers[column] = ((a, b) => ((CardDefinition)a.Asset).dropWeight.CompareTo(((CardDefinition)b.Asset).dropWeight));

        void OnDropEdited(ChangeEvent<float> evt)
        {
            if (!(((VisualElement)evt.target).userData is CardDefinition def) || catalog == null) return;
            if (!catalog.cards.Contains(def)) return;
            SetDropPercent(catalog, def, evt.newValue);
        }
        return column;
    }

    static void UpdateDropCell(VisualElement cell, CardDefinition def, CardCatalog catalog)
    {
        var field = cell.Q<FloatField>();
        bool inPool = catalog != null && catalog.cards.Contains(def);
        int total = catalog != null ? catalog.TotalWeight() : 0;
        float percent = inPool && total > 0 ? 100f * def.dropWeight / total : 0f;

        field.SetEnabled(inPool);
        field.tooltip = inPool ? "" : "Not in the CardCatalog: never drops";
        if (!IsFocused(field))
            field.SetValueWithoutNotify(Mathf.Round(percent * 10f) / 10f);
        cell.Q("fill").style.width = Length.Percent(Mathf.Clamp(percent * 2.5f, 0f, 100f));
        cell.EnableInClassList("cell--muted", !inPool);
    }

    Column DerivedColumn(List<Row> rows, string title, string tooltip, float width, Func<Row, string> text, Func<Row, float> sortKey)
    {
        var column = new Column { name = "derived-" + title, title = title, width = width, minWidth = 40 };
        column.makeHeader = Header(title, tooltip);
        column.makeCell = () =>
        {
            var cell = CellRow();
            var label = Label("", "cell--derived");
            label.style.flexGrow = 1;
            cell.Add(label);
            return cell;
        };
        column.bindCell = (e, i) =>
        {
            var row = rows[i];
            var label = e.Q<Label>();
            label.userData = (Func<string>)(() => text(row));
            label.text = text(row);
            liveCells.Add(label);
        };
        column.unbindCell = (e, i) => liveCells.Remove(e.Q<Label>());
        comparers[column] = ((a, b) => sortKey(a).CompareTo(sortKey(b)));
        return column;
    }

    void UpdateLiveCells()
    {
        var catalog = Catalog();
        foreach (var cell in liveCells.ToList())
        {
            if (cell.panel == null) { liveCells.Remove(cell); continue; }
            if (cell is Label label && label.userData is Func<string> text)
                label.text = text();
            else if (cell.Q<FloatField>()?.userData is CardDefinition def)
                UpdateDropCell(cell, def, catalog);
        }
    }

    void SetComparison<T>(Column column, List<Row> rows, Func<Row, T> key) where T : IComparable<T>
    {
        column.sortable = true;
        comparers[column] = ((a, b) => key(a).CompareTo(key(b)));
    }

    // ---------------------------------------------------------------- warnings

    static List<(string message, Object asset)> CardWarnings(List<CardDefinition> all, CardCatalog catalog)
    {
        var warnings = new List<(string, Object)>();
        if (catalog == null) warnings.Add(("No CardCatalog asset: kills drop nothing and there is no starting hand.", null));
        foreach (var def in all)
        {
            if (def.handPrefab == null) { warnings.Add(($"{def.name}: no hand prefab assigned.", def)); continue; }
            var card = def.handPrefab.GetComponent<Card>();
            if (card == null || card.definition != def)
                warnings.Add(($"{def.name}: its hand prefab points to '{(card != null && card.definition != null ? card.definition.name : "nothing")}'.", def));
            if (catalog != null && !catalog.cards.Contains(def)) warnings.Add(($"{def.name}: not in the CardCatalog, it never drops.", def));
            if (def is TowerCardDefinition && (def.placedPrefab == null || def.placedPrefab.GetComponent<TurretCard>() == null))
                warnings.Add(($"{def.name}: the placed prefab needs a TurretCard.", def));
            if (def is ArtilleryTowerCardDefinition && def.placedPrefab != null && def.placedPrefab.GetComponent<ArtilleryAuthoring>() == null)
                warnings.Add(($"{def.name}: the placed prefab needs an ArtilleryAuthoring.", def));
            if ((def is LandMineCardDefinition || def is AirStrikeCardDefinition) && def.placedPrefab == null)
                warnings.Add(($"{def.name}: no placed prefab assigned.", def));
        }
        return warnings;
    }

    void AddWarnings(List<(string message, Object asset)> warnings)
    {
        if (warnings.Count == 0) return;
        var box = new VisualElement();
        box.AddToClassList("warnings");
        box.Add(Label(warnings.Count == 1 ? "1 issue" : $"{warnings.Count} issues", "warnings__title"));
        var icon = EditorGUIUtility.IconContent("console.warnicon.sml").image as Texture2D;
        foreach (var (message, asset) in warnings)
        {
            var row = new VisualElement();
            row.AddToClassList("warning-row");
            var image = new Image { image = icon };
            image.AddToClassList("warning-row__icon");
            row.Add(image);
            row.Add(new Label(message));
            if (asset != null)
            {
                row.tooltip = "Click to select";
                row.RegisterCallback<ClickEvent>(_ => { Selection.activeObject = asset; EditorGUIUtility.PingObject(asset); });
            }
            box.Add(row);
        }
        content.Add(box);
    }

    // ---------------------------------------------------------------- helpers

    // Every column header: bold, vertically centred; text columns align left, the rest centre
    static Func<VisualElement> Header(string title, string tooltip, TextAnchor align = TextAnchor.MiddleCenter)
    {
        return () =>
        {
            var header = Label(title, "header-cell");
            header.tooltip = tooltip;
            header.style.unityTextAlign = align;
            return header;
        };
    }

    bool Matches(Object asset)
    {
        if (string.IsNullOrWhiteSpace(search)) return true;
        string s = search.Trim();
        return asset.name.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0
               || asset is CardDefinition d && d.displayName != null && d.displayName.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    static VisualElement CellRow()
    {
        var cell = new VisualElement();
        cell.style.flexDirection = FlexDirection.Row;
        cell.style.alignItems = Align.Center;
        cell.style.flexGrow = 1;
        cell.style.height = Length.Percent(100);
        return cell;
    }

    static VisualElement Panel(string title, string hint)
    {
        var panel = new VisualElement();
        panel.AddToClassList("panel");
        panel.Add(Label(title, "panel__title"));
        if (!string.IsNullOrEmpty(hint)) panel.Add(Label(hint, "panel__hint"));
        return panel;
    }

    static T Bound<T>(T field, SerializedObject so, string path) where T : BindableElement
    {
        field.BindProperty(so.FindProperty(path));
        return field;
    }

    static Label Label(string text, string className)
    {
        var label = new Label(text);
        label.AddToClassList(className);
        return label;
    }

    static Button Button(string text, Action onClick, string tooltip = null)
    {
        var button = new Button(onClick) { text = text, tooltip = tooltip };
        button.AddToClassList("toolbar__button");
        return button;
    }

    static VisualElement Spacer()
    {
        var spacer = new VisualElement();
        spacer.AddToClassList("toolbar__spacer");
        return spacer;
    }

    static bool IsFocused(VisualElement element)
    {
        var focused = element.panel?.focusController?.focusedElement as VisualElement;
        return focused != null && (focused == element || element.Contains(focused));
    }

    static FieldInfo FindField(Type type, string name)
    {
        for (var t = type; t != null; t = t.BaseType)
        {
            var f = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (f != null) return f;
        }
        return null;
    }

    static CardCatalog Catalog() => FindAssets<CardCatalog>().FirstOrDefault();

    static List<T> FindAssets<T>() where T : Object =>
        AssetDatabase.FindAssets($"t:{typeof(T).Name}")
            .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(a => a != null)
            .OrderBy(a => a.name)
            .ToList();
}
