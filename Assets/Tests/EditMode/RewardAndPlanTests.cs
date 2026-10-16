using NUnit.Framework;

namespace Gatekeeper.AI.Tests
{
    public class RewardAndPlanTests
    {
        [Test]
        public void NothingHappenedIsZero()
        {
            var o = new WaveOutcome { GateMaxHp = 500, HeroMaxHp = 100 };
            Assert.AreEqual(0f, RewardFunction.Compute(o));
        }

        [Test]
        public void EverythingDestroyedIsOne()
        {
            var o = new WaveOutcome { GateDamage = 500, GateMaxHp = 500, HeroDamage = 100, HeroMaxHp = 100, AvgProgress = 1 };
            Assert.AreEqual(1f, RewardFunction.Compute(o), 1e-5f);
        }

        [Test]
        public void RewardIsClamped()
        {
            var o = new WaveOutcome { GateDamage = 9999, GateMaxHp = 500, HeroMaxHp = 100, AvgProgress = 3 };
            Assert.AreEqual(0.8f, RewardFunction.Compute(o), 1e-5f);
        }

        [Test]
        public void ArmsRoundTrip()
        {
            Assert.AreEqual(16, PlanCatalog.Count);
            for (int arm = 0; arm < PlanCatalog.Count; arm++)
                Assert.AreEqual(arm, PlanCatalog.ToArm(PlanCatalog.FromArm(arm)));
        }
    }
}
