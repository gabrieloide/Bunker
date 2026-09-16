using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Bunker.Simulation;

namespace Bunker.Tests
{
    [TestFixture]
    public class TowerBuffSystemTests
    {
        private World world;
        private EntityManager em;
        private SystemHandle towerBuffSystemHandle;

        [SetUp]
        public void SetUp()
        {
            world = new World("TestWorld");
            em = world.EntityManager;

            // Required simulation singleton
            var configEntity = em.CreateEntity();
            em.AddComponentData(configEntity, new WaveConfig());

            towerBuffSystemHandle = world.GetOrCreateSystem<TowerBuffSystem>();
        }

        [TearDown]
        public void TearDown()
        {
            if (world.IsCreated)
            {
                world.Dispose();
            }
        }

        [Test]
        public void TowerBuff_AttackMultiplier_AppliesDamageIncreaseAndSetsHasBuff()
        {
            // Arrange
            Entity tower = em.CreateEntity();
            em.AddComponentData(tower, new TowerTag());
            em.AddComponentData(tower, new Weapon
            {
                Damage = 10f,
                FireInterval = 1.0f,
                BulletPen = 1f,
                HasBuff = false
            });
            var buffBuffer = em.AddBuffer<TowerBuffRequest>(tower);
            buffBuffer.Add(new TowerBuffRequest { Kind = TowerBuffKind.Attack, Multiplier = 1.5f });

            // Act
            towerBuffSystemHandle.Update(world.Unmanaged);

            // Assert
            var weapon = em.GetComponentData<Weapon>(tower);
            Assert.AreEqual(15f, weapon.Damage);
            Assert.IsTrue(weapon.HasBuff);
            Assert.AreEqual(0, buffBuffer.Length);
        }

        [Test]
        public void TowerBuff_SpeedMultiplier_DecreasesFireInterval()
        {
            // Arrange
            Entity tower = em.CreateEntity();
            em.AddComponentData(tower, new TowerTag());
            em.AddComponentData(tower, new Weapon
            {
                Damage = 10f,
                FireInterval = 1.0f,
                BulletPen = 1f,
                HasBuff = false
            });
            var buffBuffer = em.AddBuffer<TowerBuffRequest>(tower);
            buffBuffer.Add(new TowerBuffRequest { Kind = TowerBuffKind.Speed, Multiplier = 2.0f });

            // Act
            towerBuffSystemHandle.Update(world.Unmanaged);

            // Assert
            var weapon = em.GetComponentData<Weapon>(tower);
            Assert.AreEqual(0.5f, weapon.FireInterval, 0.001f);
            Assert.IsTrue(weapon.HasBuff);
        }

        [Test]
        public void TowerBuff_WhenAlreadyBuffed_IgnoresSubsequentBuffRequests()
        {
            // Arrange
            Entity tower = em.CreateEntity();
            em.AddComponentData(tower, new TowerTag());
            em.AddComponentData(tower, new Weapon
            {
                Damage = 10f,
                FireInterval = 1.0f,
                BulletPen = 1f,
                HasBuff = true
            });
            var buffBuffer = em.AddBuffer<TowerBuffRequest>(tower);
            buffBuffer.Add(new TowerBuffRequest { Kind = TowerBuffKind.Attack, Multiplier = 2.0f });

            // Act
            towerBuffSystemHandle.Update(world.Unmanaged);

            // Assert
            var weapon = em.GetComponentData<Weapon>(tower);
            Assert.AreEqual(10f, weapon.Damage);
            Assert.IsTrue(weapon.HasBuff);
            Assert.AreEqual(0, buffBuffer.Length);
        }
    }
}
