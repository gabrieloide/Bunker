using System;
using System.Reflection;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Bunker.Simulation;

namespace Bunker.Tests
{
    [TestFixture]
    public class WaveFlowTests
    {
        World world;
        EntityManager em;
        SystemHandle waveSystem;
        SystemHandle ecbSystem;
        Entity simEntity;
        BlobAssetReference<EnemyRosterBlob> roster;
        BlobAssetReference<EnemyBuffTableBlob> buffTable;

        [SetUp]
        public void SetUp()
        {
            world = new World("WaveFlowTestWorld");
            em = world.EntityManager;
            ecbSystem = world.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            waveSystem = world.GetOrCreateSystem<WaveSystem>();

            roster = CreateRoster();
            buffTable = CreateNeutralBuffTable();

            simEntity = em.CreateEntity();
            em.AddComponentData(simEntity, new WaveConfig
            {
                SpawnInterval = 1f,
                InitialEnemyAmount = 3,
                WavesPerTier = 10,
                InitialUnlockedTypes = 1,
                MaxUnlockedTypes = 1,
                SpawnPosition = new float3(0f, 20f, 0f),
                Roster = roster,
                BuffTable = buffTable
            });
            em.AddComponentData(simEntity, new WaveState
            {
                Wave = 1,
                Phase = WavePhase.WaitingToStart,
                EnemyAmount = 3,
                UnlockedTypes = 1,
                Timer = 0f,
                StartRequested = false,
                Rng = new Unity.Mathematics.Random(1234)
            });
            em.AddComponentData(simEntity, new GameSession());
            em.AddBuffer<SimEvent>(simEntity);
            em.AddComponent<PrimaryPath>(simEntity);

            var path = em.AddBuffer<PathPoint>(simEntity);
            path.Add(new PathPoint { Value = new float3(0f, 10f, 0f) });
            path.Add(new PathPoint { Value = new float3(0f, 0f, 0f) });
        }

        [TearDown]
        public void TearDown()
        {
            if (roster.IsCreated) roster.Dispose();
            if (buffTable.IsCreated) buffTable.Dispose();
            if (world.IsCreated) world.Dispose();
        }

        [Test]
        public void WaitingToStart_WithoutRequest_StaysWaitingOver600Updates()
        {
            var enemies = em.CreateEntityQuery(ComponentType.ReadOnly<EnemyTag>());

            double elapsed = 0.0;
            for (int i = 0; i < 600; i++)
            {
                elapsed += 0.1;
                world.SetTime(new Unity.Core.TimeData(elapsed, 0.1f));
                waveSystem.Update(world.Unmanaged);
                ecbSystem.Update(world.Unmanaged);
            }

            var state = em.GetComponentData<WaveState>(simEntity);
            Assert.AreEqual(WavePhase.WaitingToStart, state.Phase);
            Assert.AreEqual(0, state.EnemiesAlive);
            Assert.AreEqual(0, enemies.CalculateEntityCount());
            enemies.Dispose();
        }

        [Test]
        public void WaitingToStart_WithStartRequested_EntersSpawningAndClearsFlag()
        {
            var state = em.GetComponentData<WaveState>(simEntity);
            state.StartRequested = true;
            em.SetComponentData(simEntity, state);

            world.SetTime(new Unity.Core.TimeData(0.1, 0.1f));
            waveSystem.Update(world.Unmanaged);
            ecbSystem.Update(world.Unmanaged);

            var updated = em.GetComponentData<WaveState>(simEntity);
            Assert.AreEqual(WavePhase.Spawning, updated.Phase);
            Assert.IsFalse(updated.StartRequested);
        }

        [Test]
        public void WaitingForClear_WithNoEnemiesAlive_AdvancesWaveToWaitingToStart()
        {
            var state = em.GetComponentData<WaveState>(simEntity);
            state.Wave = 1;
            state.Phase = WavePhase.WaitingForClear;
            state.EnemiesAlive = 0;
            state.StartRequested = true;
            em.SetComponentData(simEntity, state);

            world.SetTime(new Unity.Core.TimeData(0.1, 0.1f));
            waveSystem.Update(world.Unmanaged);
            ecbSystem.Update(world.Unmanaged);

            var updated = em.GetComponentData<WaveState>(simEntity);
            Assert.AreEqual(WavePhase.WaitingToStart, updated.Phase);
            Assert.AreEqual(2, updated.Wave);
            Assert.AreEqual(0f, updated.Timer, 0.0001f);
            Assert.IsFalse(updated.StartRequested);
        }

        [Test]
        public void PlayableIn_AllCardTypes_ArePreparationOnly()
        {
            Assembly gameAssembly = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.GetName().Name == "Assembly-CSharp")
                {
                    gameAssembly = asm;
                    break;
                }
            }
            Assert.IsNotNull(gameAssembly, "Assembly-CSharp should be loaded in EditMode tests");

            Type cardType = gameAssembly.GetType("Card");
            Assert.IsNotNull(cardType, "Card type should exist in Assembly-CSharp");

            MethodInfo playableIn = cardType.GetMethod("PlayableIn", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(playableIn, "Card.PlayableIn static method should exist");

            string[] definitionNames =
            {
                "TowerCardDefinition",
                "BuffCardDefinition",
                "HelicopterCardDefinition",
                "AirStrikeCardDefinition",
            };

            var defs = new ScriptableObject[definitionNames.Length];
            try
            {
                for (int i = 0; i < definitionNames.Length; i++)
                {
                    Type defType = gameAssembly.GetType(definitionNames[i]);
                    Assert.IsNotNull(defType, $"{definitionNames[i]} should exist in Assembly-CSharp");
                    defs[i] = ScriptableObject.CreateInstance(defType);

                    Assert.IsTrue((bool)playableIn.Invoke(null, new object[] { defs[i], true }),
                        $"{definitionNames[i]} should be playable in preparation");
                    Assert.IsFalse((bool)playableIn.Invoke(null, new object[] { defs[i], false }),
                        $"{definitionNames[i]} should not be playable in battle");
                }
            }
            finally
            {
                foreach (var def in defs)
                {
                    if (def != null)
                        UnityEngine.Object.DestroyImmediate(def);
                }
            }
        }

        [Test]
        public void ShouldPromptFlag_TruthTable_MatchesAllFiveConditions()
        {
            Type uiManagerType = GetAssemblyCSharpType("UIManager");
            MethodInfo method = uiManagerType.GetMethod("ShouldPromptFlag", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(method, "UIManager.ShouldPromptFlag static method should exist");

            for (int mask = 0; mask < 32; mask++)
            {
                bool simReady = (mask & 1) != 0;
                bool transitioning = (mask & 2) != 0;
                bool prepPhase = (mask & 4) != 0;
                bool gameOver = (mask & 8) != 0;
                bool hasTerritory = (mask & 16) != 0;

                bool expected = simReady && !transitioning && prepPhase && !gameOver && !hasTerritory;
                bool actual = (bool)method.Invoke(null, new object[] { simReady, transitioning, prepPhase, gameOver, hasTerritory });
                Assert.AreEqual(expected, actual,
                    $"Mismatch for simReady={simReady}, transitioning={transitioning}, prepPhase={prepPhase}, gameOver={gameOver}, hasTerritory={hasTerritory}");
            }
        }

        [Test]
        public void BannerOpenScale_EndpointsMonotonicityAndSmoothness()
        {
            Type uiManagerType = GetAssemblyCSharpType("UIManager");
            MethodInfo method = uiManagerType.GetMethod("BannerOpenScale", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(method, "UIManager.BannerOpenScale static method should exist");

            const float minLineScale = 0.04f;
            float[] durations = { 0.15f, 0.12f };

            foreach (float duration in durations)
            {
                float atZero = (float)method.Invoke(null, new object[] { 0f, duration });
                float belowZero = (float)method.Invoke(null, new object[] { -0.1f, duration });
                float atEnd = (float)method.Invoke(null, new object[] { duration, duration });
                float pastEnd = (float)method.Invoke(null, new object[] { duration + 0.5f, duration });

                Assert.AreEqual(minLineScale, atZero, 0.0001f, $"Scale at t=0 for duration={duration}");
                Assert.AreEqual(minLineScale, belowZero, 0.0001f, $"Scale at t<0 for duration={duration}");
                Assert.AreEqual(1f, atEnd, 0.0001f, $"Scale at t=duration for duration={duration}");
                Assert.AreEqual(1f, pastEnd, 0.0001f, $"Scale at t>duration for duration={duration}");

                const float dt = 1f / 60f;
                float prev = atZero;
                for (float t = dt; t <= duration + 0.0001f; t += dt)
                {
                    float current = (float)method.Invoke(null, new object[] { t, duration });
                    Assert.GreaterOrEqual(current, prev - 0.0001f,
                        $"Non-monotonic at t={t} for duration={duration}");
                    Assert.LessOrEqual(current - prev, 0.5f,
                        $"Step > 0.5 at t={t} for duration={duration}");
                    prev = current;
                }
            }
        }

        [Test]
        public void ClampBannerDelta_ClampsNegativeAndHitchFrames()
        {
            Type uiManagerType = GetAssemblyCSharpType("UIManager");
            MethodInfo method = uiManagerType.GetMethod("ClampBannerDelta", BindingFlags.Public | BindingFlags.Static);
            FieldInfo maxDeltaField = uiManagerType.GetField("MaxBannerDelta", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(method, "UIManager.ClampBannerDelta static method should exist");
            Assert.IsNotNull(maxDeltaField, "UIManager.MaxBannerDelta const should exist");

            float maxDelta = (float)maxDeltaField.GetValue(null);
            Assert.LessOrEqual(maxDelta, 1f / 30f + 0.0001f);

            Assert.AreEqual(0f, (float)method.Invoke(null, new object[] { 0f }), 0.0001f);
            Assert.AreEqual(0f, (float)method.Invoke(null, new object[] { -1f }), 0.0001f);
            Assert.AreEqual(0.016f, (float)method.Invoke(null, new object[] { 0.016f }), 0.0001f);

            float clampedHitch = (float)method.Invoke(null, new object[] { 2.0f });
            Assert.AreEqual(maxDelta, clampedHitch, 0.0001f);
            Assert.LessOrEqual(clampedHitch, 1f / 30f + 0.0001f);
            Assert.Less(clampedHitch, 0.15f);
        }

        [Test]
        public void NextStableFrameCount_IncrementsOnStableFrameAndResetsOnHitchOrFalseCondition()
        {
            Type uiManagerType = GetAssemblyCSharpType("UIManager");
            MethodInfo method = uiManagerType.GetMethod("NextStableFrameCount", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(method, "UIManager.NextStableFrameCount static method should exist");

            Assert.AreEqual(6, (int)method.Invoke(null, new object[] { 5, true, 0.016f }));
            Assert.AreEqual(0, (int)method.Invoke(null, new object[] { 5, true, 0.5f }));
            Assert.AreEqual(0, (int)method.Invoke(null, new object[] { 5, false, 0.016f }));
        }

        [Test]
        public void ShouldStartFlagBanner_TruthTableAndBoundary()
        {
            Type uiManagerType = GetAssemblyCSharpType("UIManager");
            MethodInfo method = uiManagerType.GetMethod("ShouldStartFlagBanner", BindingFlags.Public | BindingFlags.Static);
            FieldInfo requiredField = uiManagerType.GetField("FlagPromptStableFramesRequired", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(method, "UIManager.ShouldStartFlagBanner static method should exist");
            Assert.IsNotNull(requiredField, "UIManager.FlagPromptStableFramesRequired const should exist");

            int required = (int)requiredField.GetValue(null);

            Assert.IsFalse((bool)method.Invoke(null, new object[] { true, false, required - 1 }));
            Assert.IsTrue((bool)method.Invoke(null, new object[] { true, false, required }));
            Assert.IsFalse((bool)method.Invoke(null, new object[] { true, true, required }));
            Assert.IsFalse((bool)method.Invoke(null, new object[] { false, false, required }));
            Assert.IsFalse((bool)method.Invoke(null, new object[] { false, true, required }));
            Assert.IsFalse((bool)method.Invoke(null, new object[] { true, true, required - 1 }));
        }

        [Test]
        public void FlagBannerSequence_WaitsForTenStableFramesAfterHitches()
        {
            Type uiManagerType = GetAssemblyCSharpType("UIManager");
            MethodInfo nextStable = uiManagerType.GetMethod("NextStableFrameCount", BindingFlags.Public | BindingFlags.Static);
            MethodInfo shouldStart = uiManagerType.GetMethod("ShouldStartFlagBanner", BindingFlags.Public | BindingFlags.Static);
            FieldInfo requiredField = uiManagerType.GetField("FlagPromptStableFramesRequired", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(nextStable, "UIManager.NextStableFrameCount static method should exist");
            Assert.IsNotNull(shouldStart, "UIManager.ShouldStartFlagBanner static method should exist");
            Assert.IsNotNull(requiredField, "UIManager.FlagPromptStableFramesRequired const should exist");

            int required = (int)requiredField.GetValue(null);
            Assert.AreEqual(10, required);

            float[] dts =
            {
                2.0f, 1.5f,
                0.016f, 0.016f, 0.016f, 0.016f, 0.016f,
                0.016f, 0.016f, 0.016f, 0.016f, 0.016f
            };

            int stableFrames = 0;
            bool shown = false;
            int startedAtIndex = -1;

            for (int i = 0; i < dts.Length; i++)
            {
                stableFrames = (int)nextStable.Invoke(null, new object[] { stableFrames, true, dts[i] });
                bool startNow = (bool)shouldStart.Invoke(null, new object[] { true, shown, stableFrames });
                if (i < 2)
                {
                    Assert.AreEqual(0, stableFrames, $"Hitch at frame {i} should reset stableFrames");
                    Assert.IsFalse(startNow, $"Banner must not start during hitch at frame {i}");
                }
                if (startNow)
                {
                    Assert.AreEqual(-1, startedAtIndex, "Banner should start only once");
                    startedAtIndex = i;
                    shown = true;
                }
            }

            Assert.AreEqual(2 + required - 1, startedAtIndex, "Banner should start on the 10th stable frame");
        }

        static Type GetAssemblyCSharpType(string fullName)
        {
            Assembly gameAssembly = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.GetName().Name == "Assembly-CSharp")
                {
                    gameAssembly = asm;
                    break;
                }
            }
            Assert.IsNotNull(gameAssembly, "Assembly-CSharp should be loaded in EditMode tests");
            Type type = gameAssembly.GetType(fullName);
            Assert.IsNotNull(type, $"{fullName} type should exist in Assembly-CSharp");
            return type;
        }

        static BlobAssetReference<EnemyRosterBlob> CreateRoster()
        {
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<EnemyRosterBlob>();
            var types = builder.Allocate(ref root.Types, 1);
            types[0] = new EnemyTypeDef
            {
                Life = 10f,
                MoveSpeed = 1f,
                Damage = 1f,
                FireInterval = 1f,
                AttackRange = 1f,
                Score = 1,
                HitHalfExtents = new float2(0.5f, 0.5f)
            };
            return builder.CreateBlobAssetReference<EnemyRosterBlob>(Allocator.Persistent);
        }

        static BlobAssetReference<EnemyBuffTableBlob> CreateNeutralBuffTable()
        {
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<EnemyBuffTableBlob>();
            var weights = builder.Allocate(ref root.Weights, 6);
            for (int i = 0; i < 6; i++) weights[i] = i == 0 ? 100f : 0f;
            root.LifeMultiplier = 1f;
            root.DamageMultiplier = 1f;
            root.FireRateMultiplier = 1f;
            root.DefenseMultiplier = 1f;
            root.SpeedMultiplier = 1f;
            return builder.CreateBlobAssetReference<EnemyBuffTableBlob>(Allocator.Persistent);
        }
    }
}
