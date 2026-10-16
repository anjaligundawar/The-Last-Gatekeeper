using NUnit.Framework;

namespace Gatekeeper.AI.Tests
{
    public class ContextEncoderTests
    {
        [Test]
        public void HasThirtyFourDimensions()
        {
            Assert.AreEqual(34, ContextEncoder.Dimensions);
            Assert.AreEqual(34, ContextEncoder.Encode(new DefenseSnapshot()).Length);
        }

        [Test]
        public void EmptyLayoutSetsOnlyWeaponAndBias()
        {
            var x = ContextEncoder.Encode(new DefenseSnapshot { Weapon = WeaponType.Spear });
            float sum = 0;
            foreach (var v in x) sum += v;
            Assert.AreEqual(2f, sum);
            Assert.AreEqual(1f, x[ContextEncoder.WeaponOffset + (int)WeaponType.Spear]);
            Assert.AreEqual(1f, x[ContextEncoder.BiasIndex]);
        }

        [Test]
        public void TowerOnSpotSetsOneHot()
        {
            var snapshot = new DefenseSnapshot();
            snapshot.Spots[4] = TowerType.Frost;
            var x = ContextEncoder.Encode(snapshot);
            Assert.AreEqual(1f, x[4 * 3 + (int)TowerType.Frost]);
            Assert.AreEqual(0f, x[4 * 3 + (int)TowerType.Archer]);
        }

        [Test]
        public void HeroLaneTimeIsCopied()
        {
            var snapshot = new DefenseSnapshot();
            snapshot.HeroLaneTime[2] = 0.75f;
            Assert.AreEqual(0.75f, ContextEncoder.Encode(snapshot)[ContextEncoder.HeroLaneOffset + 2]);
        }

        [Test]
        public void FeatureNamesAreReadable()
        {
            Assert.AreEqual("Cannon @ Middle 2", ContextEncoder.FeatureName(ContextEncoder.SpotFeatureIndex(4, TowerType.Cannon)));
            Assert.AreEqual("Weapon: Sword", ContextEncoder.FeatureName(ContextEncoder.WeaponOffset + 1));
            Assert.AreEqual("Bias", ContextEncoder.FeatureName(ContextEncoder.BiasIndex));
        }
    }
}
