using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// One-shot: copies every stat from prefabs / scene / old TowersData+Loot assets into the new
// CardDefinition, CardCatalog, EnemyData and WaveBalanceConfig assets. Delete after running.
public static class StatsMigration
{
    const string ScenePath = "Assets/Scenes/SceneGame.unity";
    const string DefinitionsFolder = "Assets/ScriptableObjects/CardDefinitions";
    const string CatalogPath = "Assets/ScriptableObjects/CardCatalog.asset";
    const string ReportPath = "Temp/StatsMigrationReport.txt";

    // Loot asset name -> hand card prefab. Matched explicitly: Loot.indexCard and Loot.loots are out of sync with the deck.
    static readonly (string loot, string hand)[] Cards =
    {
        ("Turret1", "Assets/Prefabs/Card/CardsInHand/Turret/Turret1.prefab"),
        ("Turret2", "Assets/Prefabs/Card/CardsInHand/Turret/Turret2.prefab"),
        ("Turret3", "Assets/Prefabs/Card/CardsInHand/Turret/Turret3.prefab"),
        ("Sniper", "Assets/Prefabs/Card/CardsInHand/Turret/SniperCard.prefab"),
        ("Morter", "Assets/Prefabs/Card/CardsInHand/Turret/Mortar.prefab"),
        ("LandMine", "Assets/Prefabs/Card/CardsInHand/landMinesCard.prefab"),
        ("AirAttack", "Assets/Prefabs/Card/CardsInHand/AirAttackCard.prefab"),
        ("BuffDamage", "Assets/Prefabs/Card/CardsInHand/Buff/DamageBuff.prefab"),
        ("BuffAttackSpeed", "Assets/Prefabs/Card/CardsInHand/Buff/AttackSpeedBuff.prefab"),
        ("BuffBulletPenetration", "Assets/Prefabs/Card/CardsInHand/Buff/BulletPenetration.prefab"),
        ("BuffHalfHearth", "Assets/Prefabs/Card/CardsInHand/Buff/BuffHalfHearth.prefab"),
        ("BuffFullHearth", "Assets/Prefabs/Card/CardsInHand/Buff/BuffFullHearth.prefab"),
    };

    // GameManager used AddCardToHand(3) x3 + (9) x3; in the Loot numbering 3 = BuffDamage, 9 = Turret1
    static readonly string[] StartingHand = { "BuffDamage", "BuffDamage", "BuffDamage", "Turret1", "Turret1", "Turret1" };

    static readonly StringBuilder report = new StringBuilder();

    public static void Run()
    {
        report.Clear();
        if (!AssetDatabase.IsValidFolder(DefinitionsFolder))
            AssetDatabase.CreateFolder("Assets/ScriptableObjects", "CardDefinitions");

        var definitions = new Dictionary<string, CardDefinition>();
        foreach (var (loot, hand) in Cards)
            definitions[loot] = MigrateCard(loot, hand);

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var catalog = AssetDatabase.LoadAssetAtPath<CardCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<CardCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }
        catalog.cards = new List<CardDefinition>(definitions.Values);
        catalog.startingHand = new List<CardDefinition>();
        foreach (var name in StartingHand)
            catalog.startingHand.Add(definitions[name]);

        var lootBag = Object.FindFirstObjectByType<LootBag>();
        var lootSo = new SerializedObject(lootBag);
        catalog.baseDropChance = lootSo.FindProperty("baseDropChance").floatValue;
        catalog.dropChancePerMissingTenLife = lootSo.FindProperty("dropChancePerMissingTenLife").floatValue;
        lootSo.FindProperty("catalog").objectReferenceValue = catalog;
        lootSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(catalog);
        Log($"Catalog: {catalog.cards.Count} cards, drop {catalog.baseDropChance} + {catalog.dropChancePerMissingTenLife}/10 life");

        var gameManager = Object.FindFirstObjectByType<GameManager>();
        var gmSo = new SerializedObject(gameManager);
        gmSo.FindProperty("catalog").objectReferenceValue = catalog;
        gmSo.ApplyModifiedPropertiesWithoutUndo();

        var spawner = Object.FindFirstObjectByType<EnemySpawner>();
        var bridge = Object.FindFirstObjectByType<SimulationBridge>();
        var balance = (WaveBalanceConfig)new SerializedObject(bridge).FindProperty("balance").objectReferenceValue;
        balance.startDelay = spawner.StartEnemySpawner;
        balance.spawnInterval = spawner.EnemyDelay;
        balance.initialEnemyAmount = spawner.EnemyAmount;
        EditorUtility.SetDirty(balance);
        Log($"Waves: start {balance.startDelay}s, every {balance.spawnInterval}s, {balance.initialEnemyAmount} enemies");

        foreach (var prefab in spawner.Enemies)
            MigrateEnemy(prefab);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        System.IO.File.WriteAllText(ReportPath, report.ToString());
        Debug.Log("[StatsMigration] Done\n" + report);
    }

    static CardDefinition MigrateCard(string lootName, string handPath)
    {
        var handGo = AssetDatabase.LoadAssetAtPath<GameObject>(handPath);
        var card = handGo.GetComponent<Card>();
        var oldData = card.towerData;
        var placed = oldData != null ? oldData.CardToInstantiate : null;
        var loot = AssetDatabase.LoadAssetAtPath<Loot>($"Assets/ScriptableObjects/DropCards/{lootName}.asset");

        CardDefinition def;
        string stats;
        switch (card)
        {
            case NormalCard _:
            {
                var turret = placed.GetComponent<TurretCard>();
                TowerCardDefinition tower;
                if (placed.TryGetComponent(out ArtilleryAuthoring art))
                {
                    var artillery = ScriptableObject.CreateInstance<ArtilleryTowerCardDefinition>();
                    artillery.splashRadius = art.splashRadius;
                    artillery.riseTime = art.riseTime;
                    artillery.fallTime = art.fallTime;
                    artillery.height = art.height;
                    tower = artillery;
                }
                else
                    tower = ScriptableObject.CreateInstance<TowerCardDefinition>();

                tower.life = turret.Life;
                tower.damage = turret.damage;
                tower.bulletPen = turret.bulletPen;
                tower.fireRate = turret.LegacyFireRate;
                tower.burstCount = turret.LegacyBurstCount;
                tower.burstInterval = turret.LegacyBurstInterval;
                tower.range = turret.LegacyRange;
                tower.initialCooldown = turret.LegacyFireRateCountDown;
                def = tower;
                stats = $"life {tower.life} dmg {tower.damage} pen {tower.bulletPen} rate {tower.fireRate} burst {tower.burstCount}x{tower.burstInterval} range {tower.range} cd {tower.initialCooldown}";
                break;
            }
            case SpawnCardOnMouse _:
            {
                var mine = placed.GetComponent<LandMines>();
                var landMine = ScriptableObject.CreateInstance<LandMineCardDefinition>();
                landMine.damage = mine.LegacyDamage;
                landMine.bulletPen = mine.LegacyPenArmor;
                def = landMine;
                stats = $"dmg {landMine.damage} pen {landMine.bulletPen}";
                break;
            }
            case AirAttackCard _:
            {
                var manager = placed.GetComponent<AirAttackManager>();
                var air = ScriptableObject.CreateInstance<AirStrikeCardDefinition>();
                air.damage = manager.damage;
                air.bulletPen = manager.bulletPen;
                air.shotCount = 5;
                air.shotInterval = manager.LegacyShotInterval;
                def = air;
                stats = $"dmg {air.damage} pen {air.bulletPen} shots {air.shotCount}x{air.shotInterval}";
                break;
            }
            case BuffCard buffCard:
            {
                var buff = ScriptableObject.CreateInstance<BuffCardDefinition>();
                buff.buffType = buffCard.LegacyBuffType;
                buff.amount = buffCard.LegacyAmount;
                def = buff;
                placed = null;
                stats = $"{buff.buffType} {buff.amount}";
                break;
            }
            case BuffHealth heal:
            {
                var healDef = ScriptableObject.CreateInstance<HealCardDefinition>();
                healDef.healAmount = heal.LegacyHealAmount;
                def = healDef;
                placed = null;
                stats = $"heal {healDef.healAmount}";
                break;
            }
            default:
                throw new System.InvalidOperationException($"Unknown card type on {handPath}: {card.GetType().Name}");
        }

        def.displayName = oldData != null ? oldData.Name : lootName;
        def.description = oldData != null ? oldData.Description : "";
        def.handPrefab = handGo.GetComponent<CardIndex>();
        def.placedPrefab = placed;
        def.dropWeight = loot != null ? loot.weight : 0;

        string path = $"{DefinitionsFolder}/{lootName}.asset";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(def, path);

        var cardSo = new SerializedObject(card);
        cardSo.FindProperty("definition").objectReferenceValue = def;
        cardSo.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SavePrefabAsset(handGo);

        Log($"{def.GetType().Name,-30} {lootName,-22} '{def.displayName}' weight {def.dropWeight} | {stats}");
        return def;
    }

    static void MigrateEnemy(GameObject prefab)
    {
        var enemy = prefab.GetComponent<Enemy>();
        var so = new SerializedObject(enemy.Data);
        so.FindProperty("_attackRange").floatValue = enemy.LegacyAttackRadius;
        so.FindProperty("_attackInterval").floatValue = enemy.LegacyFireInterval;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(enemy.Data);
        Log($"Enemy {enemy.Data.name,-10} range {enemy.LegacyAttackRadius} interval {enemy.LegacyFireInterval}s");
    }

    static void Log(string line) => report.AppendLine(line);
}
