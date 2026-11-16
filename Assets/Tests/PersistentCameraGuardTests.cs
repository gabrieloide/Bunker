using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Bunker.Tests
{
    [TestFixture]
    public class PersistentCameraGuardTests
    {
        readonly List<GameObject> createdObjects = new List<GameObject>();
        readonly List<RenderTexture> createdTextures = new List<RenderTexture>();

        Type guardType;
        MethodInfo shouldEnableMethod;
        MethodInfo countOtherMethod;

        [SetUp]
        public void SetUp()
        {
            guardType = Type.GetType("PersistentCameraGuard, Assembly-CSharp");
            Assert.IsNotNull(guardType, "PersistentCameraGuard type should exist in Assembly-CSharp");

            shouldEnableMethod = guardType.GetMethod("ShouldEnableFallback", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(shouldEnableMethod, "ShouldEnableFallback static method should exist");

            countOtherMethod = guardType.GetMethod("CountOtherScreenCameras", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(countOtherMethod, "CountOtherScreenCameras static method should exist");
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                if (createdObjects[i] != null)
                    UnityEngine.Object.DestroyImmediate(createdObjects[i]);
            }
            createdObjects.Clear();

            for (int i = createdTextures.Count - 1; i >= 0; i--)
            {
                if (createdTextures[i] != null)
                {
                    createdTextures[i].Release();
                    UnityEngine.Object.DestroyImmediate(createdTextures[i]);
                }
            }
            createdTextures.Clear();
        }

        [Test]
        public void ShouldEnableFallback_ReturnsTrueWhenZeroOrNegative_AndFalseWhenPositive()
        {
            Assert.IsTrue((bool)shouldEnableMethod.Invoke(null, new object[] { 0 }));
            Assert.IsFalse((bool)shouldEnableMethod.Invoke(null, new object[] { 1 }));
            Assert.IsTrue((bool)shouldEnableMethod.Invoke(null, new object[] { -1 }));
        }

        [Test]
        public void CountOtherScreenCameras_ExcludesSelfAndRenderTextureCameras()
        {
            var selfGo = new GameObject("TestSelfCamera");
            createdObjects.Add(selfGo);
            var selfCam = selfGo.AddComponent<Camera>();

            int countWithSelf = (int)countOtherMethod.Invoke(null, new object[] { selfCam });
            int countWithoutSelf = (int)countOtherMethod.Invoke(null, new object[] { null });
            Assert.AreEqual(countWithSelf + 1, countWithoutSelf, "Self camera should be excluded when passed as self");

            var screenGo = new GameObject("TestScreenCamera");
            createdObjects.Add(screenGo);
            screenGo.AddComponent<Camera>();

            int afterScreenCam = (int)countOtherMethod.Invoke(null, new object[] { selfCam });
            Assert.AreEqual(countWithSelf + 1, afterScreenCam, "Active screen camera should be counted");

            var rt = new RenderTexture(64, 64, 0);
            createdTextures.Add(rt);
            var rtGo = new GameObject("TestRenderTextureCamera");
            createdObjects.Add(rtGo);
            var rtCam = rtGo.AddComponent<Camera>();
            rtCam.targetTexture = rt;

            int afterRtCam = (int)countOtherMethod.Invoke(null, new object[] { selfCam });
            Assert.AreEqual(afterScreenCam, afterRtCam, "Camera with a RenderTexture target should be excluded");
        }
    }
}
