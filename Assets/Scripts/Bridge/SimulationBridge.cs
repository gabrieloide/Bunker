using System.Collections.Generic;
using Bunker.Simulation;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using EntityView = ViewRegistry.EntityView;

public class SimulationBridge : MonoBehaviour
{
    public static SimulationBridge Instance { get; private set; }
    public static bool IsPreparationPhase => Instance == null || !Instance.ready || SimEventDispatcher.IsPreparationPhase;

    [SerializeField] WaveBalanceConfig balance;

    EntityManager em;
    Entity simEntity;
    EntityQuery needsViewQuery;
    EntityQuery enemyQuery;
    bool ready;

    GameObject[] enemyPrefabs;
    ObjectPooling pool;
    TowerPlayer player;
    LootBag lootBag;
    AllyView[] allyPrefabs = new AllyView[0];
    EnemyBaseView primaryEnemyBase;
    readonly Dictionary<Entity, EnemyBaseView> enemyBaseViews = new Dictionary<Entity, EnemyBaseView>();
    EnemySpawner spawnerComp;
    EnemyMovement pathComp;

    float towerBulletSpeed;
    float towerBulletLifetime;
    float2 towerBulletHalfExtents;
    float damageTextOffsetY;
    float damageTextTime;

    BlobAssetReference<EnemyRosterBlob> rosterBlob;
    BlobAssetReference<EnemyBuffTableBlob> buffBlob;
    BlobAssetReference<AllyRosterBlob> allyRosterBlob;
    Entity allySpawnerEntity;
    RouteOverlay routeOverlay;

    public ViewRegistry Views => viewRegistry;
    private ViewRegistry viewRegistry;
    readonly List<Entity> removals = new List<Entity>();
    readonly List<SimEvent> pendingEvents = new List<SimEvent>();

    enum LocalViewKind { Tower = 100 }

    // Entities can't hold GameObject refs: artillery towers register their visuals here and carry the index
    struct ArtilleryVisuals
    {
        public ArtilleryShellView Shell;
        public GameObject ImpactEffect;
        public Sfx ImpactSound;
    }
    readonly List<ArtilleryVisuals> artilleryVisuals = new List<ArtilleryVisuals>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Card.ResetPendingResolveCount();
    }

    void Start()
    {
        TowerLimits.ResetStatics();
        PlacementGrid.Clear();
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
        {
            Debug.LogError("SimulationBridge: no default ECS world.");
            enabled = false;
            return;
        }
        em = world.EntityManager;

        var spawners = FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None);
        var spawner = spawners.Length > 0 ? spawners[0] : FindFirstObjectByType<EnemySpawner>();
        var path = spawner != null && spawner.path != null ? spawner.path : FindFirstObjectByType<EnemyMovement>();
        pool = FindFirstObjectByType<ObjectPooling>();
        player = FindFirstObjectByType<TowerPlayer>();
        lootBag = FindFirstObjectByType<LootBag>();
        var allEnemyBases = FindObjectsByType<EnemyBaseView>(FindObjectsSortMode.None);
        primaryEnemyBase = allEnemyBases.Length > 0 ? allEnemyBases[0] : null;
        var allySpawner = FindFirstObjectByType<AllySpawner>();
        if (LevelSetup.Current != null && LevelSetup.Current.balance != null)
            balance = LevelSetup.Current.balance;

        if (spawner == null || path == null || pool == null || balance == null)
        {
            Debug.LogError("SimulationBridge: missing EnemySpawner, EnemyMovement, ObjectPooling or WaveBalanceConfig.");
            enabled = false;
            return;
        }

        enemyPrefabs = spawner.Enemies;
        viewRegistry = new ViewRegistry(enemyPrefabs);
        ReadBulletPrefabs();
        rosterBlob = BuildRoster();
        buffBlob = BuildBuffTable();

        var enemyBullet = pool.EnemyBulletPrefab.GetComponent<Bullet>();
        ColliderBox(pool.EnemyBulletPrefab, out var enemyBulletHalf, out _);

        spawnerComp = spawner;
        pathComp = path;
        float flagDistance = (spawner != null && path != null)
            ? CalculateFlagZoneDistance(spawner.transform.position, RoutePositions(path))
            : 0f;

        var config = new WaveConfig
        {
            StartDelay = balance.startDelay,
            SpawnInterval = balance.spawnInterval,
            InitialEnemyAmount = balance.initialEnemyAmount,
            AmountIncreasePerTier = balance.amountIncreasePerTier,
            AmountIncreasePerWave = balance.amountIncreasePerWave,
            GroupSize = balance.groupSize,
            GroupGap = balance.groupGap,
            EnemySpeedMultiplier = balance.enemySpeedMultiplier,
            WavesPerTier = balance.wavesPerTier,
            InitialUnlockedTypes = balance.initialUnlockedTypes,
            MaxUnlockedTypes = balance.maxUnlockedTypes,
            LifeGrowthPerWave = balance.lifeGrowthPerWave,
            BossEveryWaves = balance.bossEveryWaves,
            BossTypeIndex = balance.bossTypeIndex,
            PlayerDamageInterval = player != null ? player.DealTime : 1f,
            EnemyProjectileSpeed = enemyBullet.Speed,
            EnemyProjectileLifetime = enemyBullet.Lifetime,
            EnemyProjectileHalfExtents = enemyBulletHalf,
            SpawnPosition = spawner.transform.position,
            Roster = rosterBlob,
            BuffTable = buffBlob,
            FlagZoneDistanceAlongPath = flagDistance,
            ContactDistance = balance.contactDistance,
            UnitSpacing = balance.unitSpacing
        };

        var rng = new Unity.Mathematics.Random((uint)UnityEngine.Random.Range(1, int.MaxValue));
        var state = new WaveState
        {
            Wave = WaveManager.instance != null ? WaveManager.instance.Wave : 1,
            EnemiesAlive = 0,
            SpawnedThisWave = 0,
            EnemyAmount = balance.initialEnemyAmount,
            UnlockedTypes = balance.initialUnlockedTypes,
            Timer = balance.startDelay,
            Phase = WavePhase.WaitingToStart,
            Buff = BalanceMath.PickWeighted(ref buffBlob.Value.Weights, ref rng),
            Rng = rng
        };

        simEntity = em.CreateEntity();
        em.AddComponent<SimulationTag>(simEntity);
        em.AddComponentData(simEntity, config);
        em.AddComponentData(simEntity, state);
        em.AddComponentData(simEntity, new GameSession
        {
            Score = GameManager.instance != null ? GameManager.instance.ActualScore : 0,
            IsGameOver = false
        });
        em.AddBuffer<SimEvent>(simEntity);

        // 1. Create per-spawner path entities first (structural changes must not interleave buffer writes)
        var pathEntities = new Dictionary<EnemyMovement, Entity>();
        // The primary route lives on simEntity; enemies and allies on it must share one entity to keep their spacing
        pathEntities[path] = simEntity;
        FillPath(em.AddBuffer<PathPoint>(simEntity), path);
        AddBranches(simEntity, path, pathEntities);
        foreach (var spawnerComp in spawners)
        {
            var assignedPath = spawnerComp.path != null ? spawnerComp.path : path;
            if (assignedPath != null)
                CreatePathEntity(assignedPath, pathEntities);
        }

        // 2. Configure simEntity primary path tag and buffers
        em.AddComponent<PrimaryPath>(simEntity);

        var spawnerBuffer = em.AddBuffer<SpawnerPoint>(simEntity);
        foreach (var spawnerComp in spawners)
        {
            var assignedPath = spawnerComp.path != null ? spawnerComp.path : path;
            Entity pEntity = Entity.Null;
            if (assignedPath != null)
                pathEntities.TryGetValue(assignedPath, out pEntity);

            spawnerBuffer.Add(new SpawnerPoint
            {
                Position = spawnerComp.transform.position,
                PathEntity = pEntity
            });
        }

        // Initialize Player Bunker Entity in ECS
        var bunkerEntity = em.CreateEntity();
        em.AddComponent<SimulationTag>(bunkerEntity);
        em.AddComponent<BunkerTag>(bunkerEntity);
        Vector3 playerPos = player != null ? player.transform.position : Vector3.zero;
        em.AddComponentData(bunkerEntity, LocalTransform.FromPosition(playerPos));
        float playerStartLife = player != null ? player.life : 100f;
        em.AddComponentData(bunkerEntity, new Health { Value = playerStartLife, Max = 100f });
        em.AddBuffer<DamageRequest>(bunkerEntity);
        if (player != null)
        {
            player.Entity = bunkerEntity;
            ColliderBox(player.gameObject, out var bunkerHalf, out var bunkerOffset);
            em.AddComponentData(bunkerEntity, new HitBox { HalfExtents = bunkerHalf, Offset = bunkerOffset });
        }

        CreateEnemyBases(allEnemyBases);
        CreateAllySpawner(allySpawner);
        routeOverlay = RouteOverlay.Create(pathEntities.Keys);

        needsViewQuery = em.CreateEntityQuery(typeof(NeedsView), typeof(LocalTransform));
        enemyQuery = em.CreateEntityQuery(typeof(EnemyTag), typeof(LocalTransform));
        MirrorWaveState(state.Wave, state.Buff);
        int initialScore = GameManager.instance != null ? GameManager.instance.ActualScore : 0;
        SimEventDispatcher.Seed(initialScore, state.Wave, state.Buff, playerStartLife, 100f, state.Phase, state.Timer);
        FlagTerritory.Changed += OnFlagTerritoryChanged;
        ready = true;
    }

    void ReadBulletPrefabs()
    {
        var towerBullet = pool.TurretBulletPrefab.GetComponent<Bullet>();
        towerBulletSpeed = towerBullet.Speed;
        towerBulletLifetime = towerBullet.Lifetime;
        ColliderBox(pool.TurretBulletPrefab, out towerBulletHalfExtents, out _);

        var text = pool.TurretBulletPrefab.GetComponent<TowerBullet>();
        damageTextOffsetY = text != null ? text.DamageTextOffsetY : -4f;
        damageTextTime = text != null ? text.DamageTextTime : 0.5f;
    }

    BlobAssetReference<EnemyRosterBlob> BuildRoster()
    {
        using var builder = new BlobBuilder(Allocator.Temp);
        ref var root = ref builder.ConstructRoot<EnemyRosterBlob>();
        var types = builder.Allocate(ref root.Types, enemyPrefabs.Length);
        for (int i = 0; i < enemyPrefabs.Length; i++)
        {
            var data = enemyPrefabs[i].GetComponent<Enemy>().Data;
            ColliderBox(enemyPrefabs[i], out var half, out var offset);
            types[i] = new EnemyTypeDef
            {
                Life = data.BaseLife * balance.enemyLifeMultiplier,
                Damage = data.BaseDamage,
                FireInterval = data.BaseAttackInterval,
                Defense = data.BaseDefense,
                MoveSpeed = data.BaseMoveSpeed,
                AttackRange = data.BaseAttackRange,
                HitHalfExtents = half,
                HitOffset = offset,
                Score = data.Score
            };
        }
        return builder.CreateBlobAssetReference<EnemyRosterBlob>(Allocator.Persistent);
    }

    void CreateEnemyBases(EnemyBaseView[] bases)
    {
        enemyBaseViews.Clear();
        if (bases == null) return;
        foreach (var baseView in bases)
        {
            if (baseView == null) continue;
            var entity = em.CreateEntity();
            em.AddComponent<SimulationTag>(entity);
            em.AddComponent<EnemyBaseTag>(entity);
            em.AddComponentData(entity, LocalTransform.FromPosition(baseView.transform.position));
            float life = baseView.StartLife;
            em.AddComponentData(entity, new Health { Value = life, Max = life });
            ColliderBox(baseView.gameObject, out var half, out var offset);
            em.AddComponentData(entity, new HitBox { HalfExtents = half, Offset = offset });
            em.AddBuffer<DamageRequest>(entity);
            baseView.Entity = entity;
            enemyBaseViews[entity] = baseView;
        }
    }

    void CreateAllySpawner(AllySpawner spawner)
    {
        AllyRoadPathEntity = Entity.Null;
        if (spawner == null || spawner.Allies == null || spawner.Allies.Length == 0) return;
        allyPrefabs = spawner.Allies;

        using var builder = new BlobBuilder(Allocator.Temp);
        ref var root = ref builder.ConstructRoot<AllyRosterBlob>();
        var types = builder.Allocate(ref root.Types, allyPrefabs.Length);
        for (int i = 0; i < allyPrefabs.Length; i++)
        {
            var data = allyPrefabs[i].Data;
            if (data == null)
            {
                Debug.LogError($"SimulationBridge: ally prefab '{allyPrefabs[i].name}' has no AllyData.", allyPrefabs[i]);
                data = ScriptableObject.CreateInstance<AllyData>();
            }
            ColliderBox(allyPrefabs[i].gameObject, out var half, out var offset);
            types[i] = new AllyTypeDef
            {
                Life = data.life,
                Damage = data.damage,
                BulletPen = data.bulletPen,
                AttackInterval = data.attackInterval,
                AttackRange = data.attackRange,
                MoveSpeed = data.moveSpeed,
                HitHalfExtents = half,
                HitOffset = offset
            };
        }
        allyRosterBlob = builder.CreateBlobAssetReference<AllyRosterBlob>(Allocator.Persistent);

        allySpawnerEntity = em.CreateEntity();
        em.AddComponent<SimulationTag>(allySpawnerEntity);
        em.AddComponentData(allySpawnerEntity, new AllySpawnConfig
        {
            SpawnPosition = spawner.transform.position,
            Roster = allyRosterBlob
        });
        em.AddBuffer<AllySpawnRequest>(allySpawnerEntity);

        int maxRoadTiles = LevelSetup.Current != null ? LevelSetup.Current.maxAllyRoadTiles : 12;
        AllyRoadNetwork.Initialize(spawner.transform.position, maxRoadTiles);
        AllyRoadOverlay.EnsureExists();
    }

    public Entity AllyRoadPathEntity { get; private set; }

    // Rebuilds the ECS PathPoint buffer for the player-built ally road in reverse order
    // (far tip -> bunker) so AllySystem walks allies outward from the bunker to the tip.
    public void SyncAllyRoadPath()
    {
        if (!ready) return;

        var polyline = AllyRoadNetwork.GetMainPolylineWorld();
        if (polyline.Count < 2) return;

        if (AllyRoadPathEntity == Entity.Null || !em.Exists(AllyRoadPathEntity))
        {
            AllyRoadPathEntity = em.CreateEntity();
            em.AddComponent<SimulationTag>(AllyRoadPathEntity);
            em.AddComponent<AllyRoadTag>(AllyRoadPathEntity);
            em.AddBuffer<PathPoint>(AllyRoadPathEntity);
        }

        var buffer = em.GetBuffer<PathPoint>(AllyRoadPathEntity);
        buffer.Clear();
        for (int i = polyline.Count - 1; i >= 0; i--)
        {
            Vector3 p = polyline[i];
            buffer.Add(new PathPoint { Value = new float3(p.x, p.y, 0f), Radius = 0.25f });
        }
    }

    // An ally card was played: `count` allies leave the bunker on the next simulation step, each a random one of `mix` when it has any
    public void RequestAllies(AllyView prefab, int count, AllyView[] mix, Entity preferredPath = default)
    {
        for (int i = 0; i < count; i++)
            RequestAlly(mix != null && mix.Length > 0 ? mix[UnityEngine.Random.Range(0, mix.Length)] : prefab, preferredPath);
    }

    public void RequestAlly(AllyView prefab, Entity preferredPath = default)
    {
        if (!ready || !em.Exists(allySpawnerEntity)) return;
        if (AllyRoadNetwork.BuiltTileCount == 0 || preferredPath == Entity.Null || !em.Exists(preferredPath) || !em.HasComponent<AllyRoadTag>(preferredPath) || !em.HasBuffer<PathPoint>(preferredPath) || em.GetBuffer<PathPoint>(preferredPath).Length < 2)
            return;
        int index = prefab != null ? System.Array.IndexOf(allyPrefabs, prefab) : -1;
        if (index < 0)
        {
            Debug.LogError($"SimulationBridge: ally '{(prefab != null ? prefab.name : "null")}' is not in the scene's AllySpawner roster.", prefab);
            return;
        }
        em.GetBuffer<AllySpawnRequest>(allySpawnerEntity).Add(new AllySpawnRequest
        {
            TypeIndex = index,
            PreferredPath = preferredPath
        });
    }

    BlobAssetReference<EnemyBuffTableBlob> BuildBuffTable()
    {
        using var builder = new BlobBuilder(Allocator.Temp);
        ref var root = ref builder.ConstructRoot<EnemyBuffTableBlob>();
        var weights = builder.Allocate(ref root.Weights, 6);
        weights[(int)EnemyBuffKind.None] = balance.weightNormal;
        weights[(int)EnemyBuffKind.Attack] = balance.weightAttack;
        weights[(int)EnemyBuffKind.Defense] = balance.weightDefense;
        weights[(int)EnemyBuffKind.Velocity] = balance.weightVelocity;
        weights[(int)EnemyBuffKind.Life] = balance.weightLife;
        weights[(int)EnemyBuffKind.FireRate] = balance.weightFireRate;
        root.LifeMultiplier = balance.lifeMultiplier;
        root.DamageMultiplier = balance.damageMultiplier;
        root.FireRateMultiplier = balance.fireRateMultiplier;
        root.DefenseMultiplier = balance.defenseMultiplier;
        root.SpeedMultiplier = balance.speedMultiplier;
        return builder.CreateBlobAssetReference<EnemyBuffTableBlob>(Allocator.Persistent);
    }

    static void ColliderBox(GameObject go, out float2 halfExtents, out float2 offset)
    {
        var scale = (float2)(Vector2)go.transform.lossyScale;
        if (go.TryGetComponent<BoxCollider2D>(out var box))
        {
            halfExtents = (float2)(Vector2)box.size * 0.5f * math.abs(scale);
            offset = (float2)(Vector2)box.offset * scale;
            return;
        }
        if (go.TryGetComponent<CircleCollider2D>(out var circle))
        {
            float r = circle.radius * math.cmax(math.abs(scale));
            halfExtents = new float2(r, r);
            offset = (float2)(Vector2)circle.offset * scale;
            return;
        }
        halfExtents = new float2(0.5f, 0.5f);
        offset = float2.zero;
    }

    // ---------------------------------------------------------------- public API (game code -> simulation)

    // Places what a card spawns and hands it the card's numbers
    public static GameObject SpawnFromCard(CardDefinition definition, Vector3 position)
    {
        if (definition == null || definition.placedPrefab == null)
        {
            Debug.LogError($"SimulationBridge: card '{(definition != null ? definition.name : "null")}' has no placed prefab.");
            return null;
        }

        if (definition is TowerCardDefinition tower && tower.placedPrefab.GetComponent<TurretCard>() != null
            && Instance != null && Instance.ready)
            return Instance.SpawnTower(tower, position);
        if (definition is HelicopterCardDefinition heli && Instance != null && Instance.ready)
            return Instance.SpawnHelicopter(heli, position);

        var spawned = Instantiate(definition.placedPrefab, position, Quaternion.identity);
        foreach (var configurable in spawned.GetComponentsInChildren<ICardConfigurable>())
            configurable.Configure(definition);
        return spawned;
    }

    public GameObject SpawnTower(TowerCardDefinition def, Vector3 position)
    {
        var view = Instantiate(def.placedPrefab, position, Quaternion.identity);
        var card = view.GetComponent<TurretCard>();
        // Set before Start so the life bar picks up the max life
        card.Definition = def;
        card.Life = def.life;
        ColliderBox(view, out var half, out var offset);

        var entity = em.CreateEntity();
        em.AddComponent<SimulationTag>(entity);
        em.AddComponent<TowerTag>(entity);
        em.AddComponentData(entity, LocalTransform.FromPosition(position));
        em.AddComponentData(entity, new Health { Value = def.life, Max = def.life });
        em.AddComponentData(entity, new HitBox { HalfExtents = half, Offset = offset });
        em.AddComponentData(entity, PlayerWeapon(def.damage, def.bulletPen, def.fireRate, def.range, def.initialCooldown,
            (float3)(card.MuzzlePosition - position), def.burstCount, def.burstInterval));
        if (def is ArtilleryTowerCardDefinition shellStats)
        {
            if (!view.TryGetComponent(out ArtilleryAuthoring artillery))
                Debug.LogError($"SimulationBridge: artillery card '{def.name}' needs an ArtilleryAuthoring on its placed prefab.", view);
            em.AddComponentData(entity, new Artillery
            {
                RiseTime = shellStats.riseTime,
                FallTime = shellStats.fallTime,
                Height = shellStats.height,
                SplashRadius = shellStats.splashRadius,
                ViewId = artillery != null ? RegisterArtillery(artillery) : -1
            });
        }
        em.AddComponentData(entity, new Target { Value = Entity.Null });
        em.AddBuffer<DamageRequest>(entity);

        card.Entity = entity;
        viewRegistry.RegisterView(entity, view, (ViewKind)LocalViewKind.Tower);
        view.AddComponent<TowerDemolition>();
        return view;
    }

    // A player-side gun shooting enemies with the standard tower bullet
    Weapon PlayerWeapon(float damage, float bulletPen, float fireRate, float range, float cooldown, float3 muzzleOffset,
        int burstCount = 1, float burstInterval = 0.12f)
    {
        return new Weapon
        {
            Damage = damage,
            BulletPen = bulletPen,
            FireInterval = fireRate > 0f ? 1f / fireRate : float.MaxValue,
            BurstCount = burstCount,
            BurstInterval = burstInterval,
            Range = range,
            Cooldown = cooldown,
            ProjectileSpeed = towerBulletSpeed,
            ProjectileLifetime = towerBulletLifetime,
            ProjectileHalfExtents = towerBulletHalfExtents,
            MuzzleOffset = muzzleOffset,
            TargetFaction = Faction.Enemy,
            HasBuff = false
        };
    }

    // Leaves from the bunker (where the builders come from) and flies to the drop point
    public GameObject SpawnHelicopter(HelicopterCardDefinition def, Vector3 site)
    {
        Vector3 home = BuilderSquad.Instance != null ? BuilderSquad.Instance.transform.position : site;
        var view = Instantiate(def.placedPrefab, home, Quaternion.identity);
        if (view.TryGetComponent(out Helicopter heli))
            heli.Launch(def, home, site);
        else
            Debug.LogError($"SimulationBridge: helicopter card '{def.name}' needs a Helicopter on its placed prefab.", view);
        return view;
    }

    // While it hovers the helicopter is a tower: it shoots enemies in range and they can shoot it down
    public Entity StartHovering(HelicopterCardDefinition def, GameObject view, Vector3 position)
    {
        if (!ready) return Entity.Null;
        ColliderBox(view, out var half, out var offset);

        var entity = em.CreateEntity();
        em.AddComponent<SimulationTag>(entity);
        em.AddComponent<TowerTag>(entity);
        em.AddComponentData(entity, LocalTransform.FromPosition(position));
        em.AddComponentData(entity, new Health { Value = def.life, Max = def.life });
        em.AddComponentData(entity, new HitBox { HalfExtents = half, Offset = offset });
        var weapon = PlayerWeapon(def.damage, def.bulletPen, def.fireRate, def.range, 0f, float3.zero);
        weapon.BaseDamageCut = 1f - def.baseDamageMultiplier;
        em.AddComponentData(entity, weapon);
        em.AddComponentData(entity, new Target { Value = Entity.Null });
        em.AddBuffer<DamageRequest>(entity);
        viewRegistry.RegisterView(entity, view, (ViewKind)LocalViewKind.Tower);
        return entity;
    }

    // Torn down by the player: the view and its tower entity go away together
    public void DemolishTower(Entity entity, GameObject view)
    {
        if (ready && em.Exists(entity))
        {
            viewRegistry.RemoveView(entity);
            em.DestroyEntity(entity);
        }
        if (view != null)
            Destroy(view);
    }

    // The patrolling helicopter drags its tower along
    public void MoveHovering(Entity entity, Vector3 position)
    {
        if (!ready || !em.Exists(entity)) return;
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
    }

    // Hover time is over: the tower goes away but the view keeps flying (home)
    public void StopHovering(Entity entity)
    {
        viewRegistry.Forget(entity);
        if (ready && em.Exists(entity))
            em.DestroyEntity(entity);
    }

    int RegisterArtillery(ArtilleryAuthoring artillery)
    {
        for (int i = 0; i < artilleryVisuals.Count; i++)
        {
            if (artilleryVisuals[i].Shell == artillery.shellPrefab && artilleryVisuals[i].ImpactEffect == artillery.impactEffect)
                return i;
        }
        artilleryVisuals.Add(new ArtilleryVisuals
        {
            Shell = artillery.shellPrefab,
            ImpactEffect = artillery.impactEffect,
            ImpactSound = artillery.impactSound
        });
        return artilleryVisuals.Count - 1;
    }

    GameObject SpawnShellView(Entity entity, int viewId, Vector3 position)
    {
        if (viewId < 0 || viewId >= artilleryVisuals.Count || artilleryVisuals[viewId].Shell == null) return null;
        var shell = em.GetComponentData<ArtilleryShell>(entity);
        var view = Instantiate(artilleryVisuals[viewId].Shell, position, Quaternion.identity);
        view.Init(shell.Origin, shell.Impact, shell.Height, shell.SplashRadius);
        return view.gameObject;
    }

    void PlayShellImpact(int viewId, Vector3 position)
    {
        if (viewId < 0 || viewId >= artilleryVisuals.Count) return;
        var visuals = artilleryVisuals[viewId];
        SoundBank.Play(visuals.ImpactSound);
        if (visuals.ImpactEffect == null) return;
        Vector3 pos = new Vector3(position.x, position.y, 0f);
        var fx = Instantiate(visuals.ImpactEffect, pos, Quaternion.identity);
        OneShotEffect.EnsureFrontSorting(fx);
    }

    public void SpawnTowerProjectile(Vector3 origin, Vector3 aimPoint, float damage, float bulletPen)
    {
        if (!ready) return;
        Vector3 dir = aimPoint - origin;
        float3 velocity = dir.sqrMagnitude > 1e-8f ? (float3)(dir.normalized * towerBulletSpeed) : new float3(towerBulletSpeed, 0f, 0f);

        var entity = em.CreateEntity();
        em.AddComponent<SimulationTag>(entity);
        em.AddComponentData(entity, LocalTransform.FromPosition(origin));
        em.AddComponentData(entity, new Projectile
        {
            Damage = damage,
            BulletPen = bulletPen,
            Lifetime = towerBulletLifetime,
            Velocity = velocity,
            HalfExtents = towerBulletHalfExtents,
            TargetFaction = Faction.Enemy
        });
        em.AddComponentData(entity, new NeedsView { Kind = ViewKind.TowerProjectile, PrefabId = 0 });
    }

    public static bool AnyEnemyWithin(Vector3 center, float radius)
    {
        if (Instance == null || !Instance.ready) return false;
        using var transforms = Instance.enemyQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
        float2 c = new float2(center.x, center.y);
        float r2 = radius * radius;
        foreach (var t in transforms)
            if (math.distancesq(t.Position.xy, c) <= r2) return true;
        return false;
    }

    // Closest enemy to `from` among those within `radius` of `center`
    public static bool TryGetNearestEnemy(Vector3 center, float radius, Vector3 from, out Vector3 position)
    {
        position = default;
        if (Instance == null || !Instance.ready) return false;
        using var transforms = Instance.enemyQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
        float2 c = new float2(center.x, center.y), f = new float2(from.x, from.y);
        float r2 = radius * radius, best = float.MaxValue;
        bool found = false;
        foreach (var t in transforms)
        {
            if (math.distancesq(t.Position.xy, c) > r2) continue;
            float d = math.distancesq(t.Position.xy, f);
            if (d < best)
            {
                best = d;
                position = t.Position;
                found = true;
            }
        }
        return found;
    }

    public void RequestDamage(Entity entity, float damage, float bulletPen)
    {
        if (!ready || !em.Exists(entity) || !em.HasBuffer<DamageRequest>(entity)) return;
        em.GetBuffer<DamageRequest>(entity).Add(new DamageRequest { Damage = damage, BulletPen = bulletPen });
    }

    // A tower takes one buff card; its numbers are applied once
    public void ApplyBuff(Entity entity, BuffCardDefinition buff)
    {
        if (!ready || !em.Exists(entity) || !em.HasComponent<Weapon>(entity)) return;
        var weapon = em.GetComponentData<Weapon>(entity);
        if (weapon.HasBuff) return;
        var health = em.GetComponentData<Health>(entity);
        switch (buff.stat)
        {
            case TowerStat.Damage: weapon.Damage *= buff.value; break;
            case TowerStat.FireRate: weapon.FireInterval /= Mathf.Max(buff.value, 1e-3f); break;
            case TowerStat.BulletPen: weapon.BulletPen += buff.value; break;
            case TowerStat.Range: weapon.Range *= buff.value; break;
            case TowerStat.Life: health.Value *= buff.value; health.Max *= buff.value; break;
        }
        weapon.HasBuff = true;
        em.SetComponentData(entity, weapon);
        em.SetComponentData(entity, health);
    }

    public void RequestBunkerHeal(float amount)
    {
        if (!ready || player == null || !em.Exists(player.Entity) || !em.HasBuffer<DamageRequest>(player.Entity)) return;
        em.GetBuffer<DamageRequest>(player.Entity).Add(new DamageRequest { Damage = -amount, BulletPen = 0f });
        SimEventDispatcher.RecordHeal(amount);
    }

    public void RequestBunkerDamage(float amount)
    {
        if (!ready || player == null || !em.Exists(player.Entity) || !em.HasBuffer<DamageRequest>(player.Entity)) return;
        em.GetBuffer<DamageRequest>(player.Entity).Add(new DamageRequest { Damage = amount, BulletPen = 0f });
    }

    // ---------------------------------------------------------------- presentation sync

    void LateUpdate()
    {
        if (!ready) return;
        // Leaving play mode can dispose the ECS world before the last LateUpdate
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
        {
            ready = false;
            return;
        }
        DrainEvents();
        SyncPhaseAndCheckEarlyStart();
        UpdateFlagArrival();
        CreateViews();
        SyncViews();
    }

    // Seconds the fastest enemy of this wave needs from its spawner to the player's flags; 0 with no territory
    public float FlagArrivalSeconds { get; private set; }
    // Some enemy of the running wave already stands inside the player's territory
    public bool EnemyInTerritory { get; private set; }

    void UpdateFlagArrival()
    {
        if (!em.Exists(simEntity) || !em.HasComponent<WaveConfig>(simEntity) || !em.HasComponent<WaveState>(simEntity))
            return;
        var config = em.GetComponentData<WaveConfig>(simEntity);
        var state = em.GetComponentData<WaveState>(simEntity);

        float fastest = 0f;
        int unlocked = Mathf.Min(state.UnlockedTypes, enemyPrefabs.Length);
        for (int i = 0; i < unlocked; i++)
        {
            var enemy = enemyPrefabs[i] != null ? enemyPrefabs[i].GetComponent<Enemy>() : null;
            if (enemy != null && enemy.Data != null)
                fastest = Mathf.Max(fastest, enemy.Data.BaseMoveSpeed);
        }
        fastest *= balance.enemySpeedMultiplier;
        if (state.Buff == EnemyBuffKind.Velocity) fastest *= balance.speedMultiplier;
        FlagArrivalSeconds = fastest > 0f ? config.FlagZoneDistanceAlongPath / fastest : 0f;

        EnemyInTerritory = false;
        if (state.Phase == WavePhase.WaitingToStart || !FlagTerritory.HasTerritory) return;
        using var positions = enemyQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
        foreach (var t in positions)
            if (FlagTerritory.Contains(t.Position))
            {
                EnemyInTerritory = true;
                break;
            }
    }

    void SyncPhaseAndCheckEarlyStart()
    {
        if (!em.Exists(simEntity) || !em.HasComponent<WaveState>(simEntity))
            return;

        var state = em.GetComponentData<WaveState>(simEntity);
        // Every card played: start wave now instead of waiting out the countdown
        if (state.Phase == WavePhase.WaitingToStart && state.SpawnedThisWave == 0 && state.Timer > 0f && AllCardsSpent())
        {
            state.Timer = 0f;
            em.SetComponentData(simEntity, state);
        }
        SimEventDispatcher.UpdatePhaseTimer(state.Phase, math.max(0f, state.Timer));
    }

    static bool AllCardsSpent()
    {
        if (GameManager.instance == null || !GameManager.instance.StartingHandDealt || GameManager.instance.onDrag)
            return false;
        // The last card may still be on its way with the builder soldier
        if (Card.PendingResolveCount > 0)
            return false;
        if (Deck.instance == null || Deck.instance.Hand == null)
            return false;
        if (CardDrop.instance != null && CardDrop.instance.HasPlayableCard)
            return false;
        if (Deck.instance.Hand.VisibleCount > 0)
            return false;
        return true;
    }

    void DrainEvents()
    {
        var buffer = em.GetBuffer<SimEvent>(simEntity);
        if (buffer.Length == 0) return;

        pendingEvents.Clear();
        for (int i = 0; i < buffer.Length; i++)
            pendingEvents.Add(buffer[i]);
        buffer.Clear();

        foreach (var ev in pendingEvents)
        {
            switch (ev.Kind)
            {
                case SimEventKind.Fired:
                    if (viewRegistry.TryGetView(ev.Source, out var shooter))
                    {
                        if (shooter.TurretCard != null) shooter.TurretCard.OnFired(ev.Position);
                        else if (shooter.Enemy != null) shooter.Enemy.OnFired();
                    }
                    break;

                case SimEventKind.ProjectileHit:
                    if (viewRegistry.TryGetView(ev.Source, out var bulletView) && bulletView.Bullet != null)
                        bulletView.Bullet.PlayHitSound();
                    if (viewRegistry.TryGetView(ev.Target, out var hitView) && hitView.Enemy != null)
                        hitView.Enemy.OnHit();
                    if ((Faction)ev.IntValue == Faction.Enemy)
                        ShowDamageText(ev.Position, ev.Amount);
                    break;

                case SimEventKind.MeleeHit:
                    // Ally hits show numbers on enemies and the base; enemy hits on allies play the enemy's attack animation only
                    bool enemyAttacker = false;
                    if (viewRegistry.TryGetView(ev.Source, out var attacker))
                    {
                        if (attacker.Ally != null) attacker.Ally.OnAttack();
                        else if (attacker.Enemy != null)
                        {
                            enemyAttacker = true;
                            attacker.Enemy.OnFired();
                        }
                    }
                    if (viewRegistry.TryGetView(ev.Target, out var struck) && struck.Enemy != null)
                        struck.Enemy.OnHit();
                    if (!enemyAttacker)
                        ShowDamageText(ev.Position, ev.Amount);
                    break;

                case SimEventKind.AllyDied:
                    if (viewRegistry.TryGetView(ev.Source, out var deadAlly) && deadAlly.Ally != null)
                        deadAlly.Ally.OnDied();
                    break;

                case SimEventKind.EnemyBaseHit:
                    var hitBase = ResolveEnemyBase(ev.Source);
                    if (hitBase != null)
                    {
                        hitBase.OnHit(ev.IntValue);
                        if (ev.IntValue <= 0)
                            hitBase.OnDestroyed();
                    }
                    break;

                case SimEventKind.Victory:
                    var winBase = ResolveEnemyBase(ev.Source);
                    if (winBase != null) winBase.OnDestroyed();
                    break;

                case SimEventKind.EnemyDied:
                    if (viewRegistry.TryGetView(ev.Source, out var deadView) && deadView.Enemy != null)
                    {
                        if (deadView.Enemy.ExplosionParticle != null)
                        {
                            Vector3 pos = new Vector3(ev.Position.x, ev.Position.y, 0f);
                            var fx = Instantiate(deadView.Enemy.ExplosionParticle, pos, Quaternion.identity);
                            OneShotEffect.EnsureFrontSorting(fx);
                        }
                        deadView.Enemy.OnDied();
                    }
                    if (lootBag != null) lootBag.InstantiateLoot();
                    break;

                case SimEventKind.ScoreChanged:
                    if (GameManager.instance != null) GameManager.instance.ActualScore = ev.IntValue;
                    break;

                case SimEventKind.EnemyReachedEnd:
                    if (viewRegistry.TryGetView(ev.Source, out var endView) && endView.Enemy != null)
                        endView.Enemy.OnReachedEnd();
                    break;

                case SimEventKind.PlayerHit:
                    if (player != null) player.TakeHit(ev.Amount, ev.IntValue);
                    // Only damage to the bunker shakes the camera; combat hits and kills stay still
                    CameraShake.HeavyShake();
                    break;

                case SimEventKind.ShellImpact:
                    PlayShellImpact(ev.IntValue, ev.Position);
                    break;

                case SimEventKind.WaveChanged:
                    MirrorWaveState(ev.IntValue, (EnemyBuffKind)(int)ev.Amount);
                    break;

                case SimEventKind.GameOver:
                    if (WaveManager.instance != null && WaveManager.instance.winScreen != null)
                        WaveManager.instance.winScreen.SetActive(true);
                    break;
            }

            SimEventDispatcher.Dispatch(ev);
        }
    }

    void CreateViews()
    {
        if (needsViewQuery.IsEmptyIgnoreFilter) return;

        using var entities = needsViewQuery.ToEntityArray(Allocator.Temp);
        foreach (var entity in entities)
        {
            var request = em.GetComponentData<NeedsView>(entity);
            Vector3 position = em.GetComponentData<LocalTransform>(entity).Position;
            GameObject view = null;

            switch (request.Kind)
            {
                case ViewKind.Enemy:
                    view = viewRegistry.GetEnemyView(request.PrefabId, position);
                    if (view != null && view.TryGetComponent<Enemy>(out var enemy))
                        enemy.Entity = entity;
                    break;
                case ViewKind.TowerProjectile:
                    view = pool.TurretShoot();
                    break;
                case ViewKind.EnemyProjectile:
                    view = pool.EnemyShoot();
                    break;
                case ViewKind.ArtilleryShell:
                    view = SpawnShellView(entity, request.PrefabId, position);
                    break;
                case ViewKind.Ally:
                    if (request.PrefabId >= 0 && request.PrefabId < allyPrefabs.Length && allyPrefabs[request.PrefabId] != null)
                        view = Instantiate(allyPrefabs[request.PrefabId], position, Quaternion.identity).gameObject;
                    break;
            }

            if (view == null) continue;

            if (request.Kind == ViewKind.TowerProjectile || request.Kind == ViewKind.EnemyProjectile)
            {
                ViewRegistry.MakeKinematic(view);
                var velocity = em.GetComponentData<Projectile>(entity).Velocity;
                view.transform.SetPositionAndRotation(position, RotateObjectTo.FromDirection(velocity));
            }
            viewRegistry.RegisterView(entity, view, request.Kind, request.PrefabId);
        }
        em.RemoveComponent<NeedsView>(needsViewQuery);
    }

    void SyncViews()
    {
        removals.Clear();
        var enumerator = viewRegistry.GetEnumerator();
        while (enumerator.MoveNext())
        {
            var pair = enumerator.Current;
            var entity = pair.Key;
            var view = pair.Value;
            if (view.GameObject == null || !em.Exists(entity))
            {
                removals.Add(entity);
                continue;
            }

            Vector3 position = em.GetComponentData<LocalTransform>(entity).Position;
            if (view.Kind == ViewKind.Enemy || view.Kind == ViewKind.Ally)
            {
                Vector3 targetPos = position + view.VisualOffset;
                float dx = targetPos.x - view.Transform.position.x;
                if (Mathf.Abs(dx) > 1e-4f)
                {
                    var scale = view.Transform.localScale;
                    scale.x = Mathf.Abs(scale.x) * (dx < 0f ? -1f : 1f);
                    view.Transform.localScale = scale;
                }
                view.Transform.position = targetPos;
            }
            else if (view.Kind == (ViewKind)LocalViewKind.Tower)
            {
                if (view.TurretCard != null)
                {
                    var health = em.GetComponentData<Health>(entity);
                    view.TurretCard.Life = health.Value;
                    view.TurretCard.MaxLife = health.Max;
                    view.TurretCard.SetRange(em.GetComponentData<Weapon>(entity).Range);
                }
                view.Transform.position = position;
            }
            else
            {
                view.Transform.position = position;
            }
        }

        foreach (var entity in removals)
            viewRegistry.RemoveView(entity);
    }

    void ShowDamageText(Vector3 position, float amount)
    {
        var text = pool.TextDamage();
        if (text == null) return;
        text.SetActive(false);
        text.SetActive(true);
        if (text.TryGetComponent<DisableTextDamage>(out var damageTextComp))
        {
            damageTextComp.Animate(position, amount);
        }
    }

    static void MirrorWaveState(int wave, EnemyBuffKind buff)
    {
        if (WaveManager.instance == null) return;
        WaveManager.instance.Wave = wave;
        WaveManager.instance.buffEnemyType = (BuffEnemyType)(int)buff;
    }

    void OnFlagTerritoryChanged()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return;

        if (!ready || !em.Exists(simEntity) || !em.HasComponent<WaveConfig>(simEntity))
            return;

        if (spawnerComp == null || pathComp == null)
            return;

        float dist = CalculateFlagZoneDistance(spawnerComp.transform.position, RoutePositions(pathComp));
        var config = em.GetComponentData<WaveConfig>(simEntity);
        config.FlagZoneDistanceAlongPath = dist;
        em.SetComponentData(simEntity, config);
    }

    EnemyBaseView ResolveEnemyBase(Entity baseEntity)
    {
        if (baseEntity != Entity.Null && enemyBaseViews.TryGetValue(baseEntity, out var found))
            return found;
        return primaryEnemyBase;
    }

    // One entity per route, plus every route it forks into. Fills `entities` so a shared branch is created once
    Entity CreatePathEntity(EnemyMovement movement, Dictionary<EnemyMovement, Entity> entities, HashSet<EnemyMovement> visiting = null)
    {
        if (entities.TryGetValue(movement, out var existing))
            return existing;

        visiting ??= new HashSet<EnemyMovement>();
        if (!visiting.Add(movement))
        {
            Debug.LogError($"SimulationBridge: cycle detected in route '{movement.name}'.", movement);
            return Entity.Null;
        }

        Entity entity = em.CreateEntity();
        entities[movement] = entity;
        em.AddComponent<SimulationTag>(entity);
        FillPath(em.AddBuffer<PathPoint>(entity), movement);
        AddBranches(entity, movement, entities, visiting);
        visiting.Remove(movement);
        return entity;
    }

    void AddBranches(Entity entity, EnemyMovement movement, Dictionary<EnemyMovement, Entity> entities, HashSet<EnemyMovement> visiting = null)
    {
        if (!movement.Forks) return;

        visiting ??= new HashSet<EnemyMovement>();
        visiting.Add(movement);

        var children = new List<Entity>();
        foreach (var branch in movement.branches)
        {
            if (branch == null || branch.points == null || branch.points.Length == 0)
            {
                Debug.LogError($"SimulationBridge: route '{movement.name}' has an empty branch; skipping it.", movement);
                continue;
            }
            if (visiting.Contains(branch))
            {
                Debug.LogError($"SimulationBridge: circular branch '{branch.name}' on route '{movement.name}'; skipping it.", movement);
                continue;
            }
            Entity child = CreatePathEntity(branch, entities, visiting);
            if (child == Entity.Null) continue;
            children.Add(child);

            var parents = em.HasBuffer<PathParent>(child) ? em.GetBuffer<PathParent>(child) : em.AddBuffer<PathParent>(child);
            bool alreadyLinked = false;
            for (int i = 0; i < parents.Length; i++)
            {
                if (parents[i].Path == entity) { alreadyLinked = true; break; }
            }
            if (!alreadyLinked)
                parents.Add(new PathParent { Path = entity });
            if (parents.Length > 1 && !em.HasComponent<PathMergeCursor>(child))
                em.AddComponentData(child, new PathMergeCursor());
        }

        if (children.Count == 0) return;
        em.AddComponentData(entity, new PathFork());
        var buffer = em.AddBuffer<PathBranch>(entity);
        foreach (var child in children)
            buffer.Add(new PathBranch { Path = child });
    }

    // Writes the walked route (flags plus L corners) into the simulation's path buffer
    static void FillPath(DynamicBuffer<PathPoint> buffer, EnemyMovement movement)
    {
        foreach (var (position, radius) in movement.BuildRoute())
            buffer.Add(new PathPoint { Value = position, Radius = radius });
    }

    static Vector3[] RoutePositions(EnemyMovement movement)
    {
        var route = movement.BuildRoute();
        var positions = new Vector3[route.Count];
        for (int i = 0; i < positions.Length; i++)
            positions[i] = route[i].position;
        return positions;
    }

    public static float CalculateFlagZoneDistance(Vector3 spawnPos, Vector3[] pathPoints)
    {
        if (!FlagTerritory.HasTerritory)
            return 0f;

        if (FlagTerritory.Contains(spawnPos))
            return 0f;

        if (pathPoints == null || pathPoints.Length == 0)
            return 0f;

        float accumulatedDistance = 0f;
        Vector3 current = spawnPos;

        for (int i = 0; i < pathPoints.Length; i++)
        {
            Vector3 target = pathPoints[i];
            float segmentLength = Vector3.Distance(current, target);
            if (segmentLength > 0.0001f)
            {
                const float step = 0.05f;
                int steps = Mathf.CeilToInt(segmentLength / step);
                float prevDistOnSegment = 0f;

                for (int s = 1; s <= steps; s++)
                {
                    float distOnSegment = Mathf.Min(s * step, segmentLength);
                    Vector3 samplePoint = Vector3.Lerp(current, target, distOnSegment / segmentLength);

                    if (FlagTerritory.Contains(samplePoint))
                    {
                        float low = prevDistOnSegment;
                        float high = distOnSegment;
                        for (int iter = 0; iter < 8; iter++)
                        {
                            float mid = (low + high) * 0.5f;
                            Vector3 midPt = Vector3.Lerp(current, target, mid / segmentLength);
                            if (FlagTerritory.Contains(midPt))
                                high = mid;
                            else
                                low = mid;
                        }
                        return accumulatedDistance + high;
                    }

                    prevDistOnSegment = distOnSegment;
                }
            }

            accumulatedDistance += segmentLength;
            current = target;
        }

        return accumulatedDistance;
    }

    void OnDestroy()
    {
        PlacementGrid.Clear();
        FlagTerritory.Changed -= OnFlagTerritoryChanged;
        if (Instance == this) Instance = null;

        var world = World.DefaultGameObjectInjectionWorld;
        if (ready && world != null && world.IsCreated)
        {
            var query = em.CreateEntityQuery(typeof(SimulationTag));
            em.DestroyEntity(query);
        }
        if (rosterBlob.IsCreated) rosterBlob.Dispose();
        if (buffBlob.IsCreated) buffBlob.Dispose();
        if (allyRosterBlob.IsCreated) allyRosterBlob.Dispose();

        if (viewRegistry != null)
        {
            viewRegistry.Clear();
            viewRegistry = null;
        }
    }
}
