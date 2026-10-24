using NUnit.Framework;
using Bunker.Simulation;

namespace Bunker.Tests
{
    [TestFixture]
    public class PrdTests
    {
        [Test]
        public void Constant_MatchesKnownTable()
        {
            // Reference values of the Dota-style PRD table
            Assert.AreEqual(0.0847f, BalanceMath.PrdConstant(0.25f), 5e-4f);
            Assert.AreEqual(0.3022f, BalanceMath.PrdConstant(0.5f), 5e-4f);
        }

        [TestCase(0.05f)]
        [TestCase(0.25f)]
        [TestCase(0.6f)]
        [TestCase(0.95f)]
        public void Average_EqualsRequestedChance(float chance)
        {
            Assert.AreEqual(chance, BalanceMath.PrdAverage(BalanceMath.PrdConstant(chance)), 1e-3f);
        }

        [Test]
        public void Chance_GrowsWithMissesAndCapsAtOne()
        {
            float first = BalanceMath.PrdChance(0.25f, 0);
            Assert.Less(first, 0.25f, "right after a drop the next one is less likely");
            Assert.Greater(BalanceMath.PrdChance(0.25f, 5), 0.25f, "after a dry run the next one is more likely");
            Assert.AreEqual(1f, BalanceMath.PrdChance(0.25f, 20), 1e-6f, "a drought always ends");
        }

        [Test]
        public void Chance_Extremes()
        {
            Assert.AreEqual(0f, BalanceMath.PrdChance(0f, 10));
            Assert.AreEqual(1f, BalanceMath.PrdChance(1f, 0));
        }
    }
}
