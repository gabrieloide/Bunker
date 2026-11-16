using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Bunker.Tests
{
    [TestFixture]
    public class AirSupportQueueTests
    {
        Type queueType;
        PropertyInfo countProperty;
        MethodInfo enqueueMethod;
        MethodInfo launchAllMethod;
        MethodInfo clearMethod;
        Type cardDefinitionType;
        ScriptableObject testDefinition;

        static Type FindType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullName, throwOnError: false);
                if (type != null)
                    return type;
            }
            return null;
        }

        [OneTimeSetUp]
        public void ResolveTypes()
        {
            queueType = FindType("AirSupportQueue");
            Assert.IsNotNull(queueType, "AirSupportQueue type should exist in loaded assemblies.");

            countProperty = queueType.GetProperty("Count", BindingFlags.Public | BindingFlags.Static);
            enqueueMethod = queueType.GetMethod("Enqueue", BindingFlags.Public | BindingFlags.Static);
            launchAllMethod = queueType.GetMethod("LaunchAll", BindingFlags.Public | BindingFlags.Static);
            clearMethod = queueType.GetMethod("Clear", BindingFlags.Public | BindingFlags.Static);

            Assert.IsNotNull(countProperty, "AirSupportQueue.Count property should exist.");
            Assert.IsNotNull(enqueueMethod, "AirSupportQueue.Enqueue method should exist.");
            Assert.IsNotNull(launchAllMethod, "AirSupportQueue.LaunchAll method should exist.");
            Assert.IsNotNull(clearMethod, "AirSupportQueue.Clear method should exist.");

            cardDefinitionType = FindType("AirStrikeCardDefinition") ?? FindType("CardDefinition");
            Assert.IsNotNull(cardDefinitionType, "CardDefinition type should exist in loaded assemblies.");
        }

        [SetUp]
        public void SetUp()
        {
            clearMethod.Invoke(null, null);
            testDefinition = ScriptableObject.CreateInstance(cardDefinitionType);
        }

        [TearDown]
        public void TearDown()
        {
            clearMethod.Invoke(null, null);
            if (testDefinition != null)
            {
                Object.DestroyImmediate(testDefinition);
                testDefinition = null;
            }
        }

        int GetCount() => (int)countProperty.GetValue(null);

        [Test]
        public void Enqueue_IncrementsCount()
        {
            Assert.AreEqual(0, GetCount());

            enqueueMethod.Invoke(null, new object[] { testDefinition, new Vector3(1f, 2f, 0f) });
            Assert.AreEqual(1, GetCount());

            enqueueMethod.Invoke(null, new object[] { testDefinition, new Vector3(3f, 4f, 0f) });
            Assert.AreEqual(2, GetCount());
        }

        [Test]
        public void Clear_ResetsCountToZero()
        {
            enqueueMethod.Invoke(null, new object[] { testDefinition, Vector3.zero });
            enqueueMethod.Invoke(null, new object[] { testDefinition, Vector3.one });
            Assert.AreEqual(2, GetCount());

            clearMethod.Invoke(null, null);
            Assert.AreEqual(0, GetCount());
        }

        [Test]
        public void LaunchAll_WithNoBridge_EmptiesQueueWithoutThrowing()
        {
            enqueueMethod.Invoke(null, new object[] { testDefinition, new Vector3(2f, 5f, 0f) });
            Assert.AreEqual(1, GetCount());

            Assert.DoesNotThrow(() => launchAllMethod.Invoke(null, null));
            Assert.AreEqual(0, GetCount());
        }
    }
}
