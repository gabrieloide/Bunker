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
    EnemyBaseView enemyBase;

    float towerBulletSpeed;
    float towerBulletLifetime;
    float2 towerBulletHalfExtents;
    float damageTextOffsetY;
    float damageTextTime;

    BlobAssetReference<EnemyRosterBlob> rosterBlob;
    BlobAssetReference<EnemyBuffTableBlob> buffBlob;
    BlobAssetReference<AllyRosterBlob> allyRosterBlob;

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
        public AK.Wwise.Event ImpactSound;
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
    }

    void Start()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
        {
            Debug.LogError("SimulationBridge: no default ECS world.");
            enabled = false;
            return;
        }
        em = world.EntityManager;

        var spawner = FindFirstObjectByType<EnemySpawner>();
        var path = FindFirstObjectByType<EnemyMovement>();
        pool = FindFirstObjectByType<ObjectPooling>();
        player = FindFirstObjectByType<TowerPlayer>();
        lootBag = FindFirstObjectByType<LootBag>();
        enemyBase = FindFirstObjectByType<EnemyBaseView>();
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

        var config = new WaveConfig
        {
            StartDelay = balance.startDelay,
            SpawnInterval = balance.spawnInterval,
            InitialEnemyAmount = balance.initialEnemyAmount,
            AmountIncreasePerTier = balance.amountIncreasePerTier,
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
            BuffTable = buffBlob
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
        var pathBuffer = em.AddBuffer<PathPoint>(simEntity);
        foreach (var point in path.points)
            pathBuffer.Add(new PathPoint { Value = point });

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

        CreateEnemyBase();
        CreateAllySpawner(allySpawner);

        needsViewQuery = em.CreateEntityQuery(typeof(NeedsView), typeof(LocalTransform));
        enemyQuery = em.CreateEntityQuery(typeof(EnemyTag), typeof(LocalTransform));
        MirrorWaveState(state.Wave, state.Buff);
        int initialScore = GameManager.instance != null ? GameManager.instance.ActualScore : 0;
        SimEventDispatcher.Seed(initialScore, state.Wave, state.Buff, playerStartLife, 100f);
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
                Life = data.BaseLife,
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

    void CreateEnemyBase()
    {
        if (enemyBase == null) return;
        var entity = em.CreateEntity();
        em.AddComponent<SimulationTag>(entity);
        em.AddComponent<EnemyBaseTag>(entity);
        em.AddComponentData(entity, LocalTransform.FromPosition(enemyBase.transform.position));
        float life = enemyBase.StartLife;
        em.AddComponentData(entity, new Health { Value = life, Max = life });
        ColliderBox(enemyBase.gameObject, out var half, out var offset);
        em.AddComponentData(entity, new HitBox { HalfExtents = half, Offset = offset });
        em.AddBuffer<DamageRequest>(entity);
        enemyBase.Entity = entity;
    }

    void CreateAllySpawner(AllySpawner spawner)
    {
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

        var entity = em.CreateEntity();
        em.AddComponent<SimulationTag>(entity);
        em.AddComponentData(entity, new AllySpawnConfig
        {
            Interval = spawner.Interval,
            SpawnPosition = spawner.transform.position,
            Roster = allyRosterBlob
        });
        em.AddComponentData(entity, new AllySpawnState { Timer = spawner.FirstDelay, NextType = 0 });
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
        em.AddComponentData(entity, new Weapon
        {
            Damage = def.damage,
            BulletPen = def.bulletPen,
            FireInterval = def.fireRate > 0f ? 1f / def.fireRate : float.MaxValue,
            BurstCount = def.burstCount,
            BurstInterval = def.burstInterval,
            Range = def.range,
            Cooldown = def.initialCooldown,
            ProjectileSpeed = towerBulletSpeed,
            ProjectileLifetime = towerBulletLifetime,
            ProjectileHalfExtents = towerBulletHalfExtents,
            MuzzleOffset = (float3)(card.MuzzlePosition - position),
            TargetFaction = Faction.Enemy,
            HasBuff = false
        });
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
        return view;
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
        if (visuals.ImpactEffect == null) return;
        var fx = Instantiate(visuals.ImpactEffect, position, Quaternion.identity);
        if (visuals.ImpactSound != null && visuals.ImpactSound.IsValid())
            visuals.ImpactSound.Post(fx);
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
        weapon.Damage *= buff.damageMultiplier;
        weapon.FireInterval /= Mathf.Max(buff.fireRateMultiplier, 1e-3f);
        weapon.Range *= buff.rangeMultiplier;
        weapon.BulletPen += buff.bulletPenBonus;
        weapon.HasBuff = true;
        em.SetComponentData(entity, weapon);

        var health = em.GetComponentData<Health>(entity);
        health.Value *= buff.lifeMultiplier;
        health.Max *= buff.lifeMultiplier;
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
        CreateViews();
        SyncViews();
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
                    if (viewRegistry.TryGetView(ev.Source, out var attacker) && attacker.Ally != null)
                        attacker.Ally.OnAttack();
                    if (viewRegistry.TryGetView(ev.Target, out var struck) && struck.Enemy != null)
                        struck.Enemy.OnHit();
                    ShowDamageText(ev.Position, ev.Amount);
                    break;

                case SimEventKind.AllyDied:
                    if (viewRegistry.TryGetView(ev.Source, out var deadAlly) && deadAlly.Ally != null)
                        deadAlly.Ally.OnDied();
                    break;

                case SimEventKind.EnemyBaseHit:
                    if (enemyBase != null) enemyBase.OnHit(ev.IntValue);
                    break;

                case SimEventKind.Victory:
                    if (enemyBase != null) enemyBase.OnDestroyed();
                    break;

                case SimEventKind.EnemyDied:
                    if (viewRegistry.TryGetView(ev.Source, out var deadView) && deadView.Enemy != null)
                    {
                        if (deadView.Enemy.ExplosionParticle != null)
                            Instantiate(deadView.Enemy.ExplosionParticle, (Vector3)ev.Position, Quaternion.identity);
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
                float dx = position.x - view.Transform.position.x;
                if (Mathf.Abs(dx) > 1e-4f)
                {
                    var scale = view.Transform.localScale;
                    scale.x = Mathf.Abs(scale.x) * (dx < 0f ? -1f : 1f);
                    view.Transform.localScale = scale;
                }
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
            }
            view.Transform.position = position;
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

    void OnDestroy()
    {
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
