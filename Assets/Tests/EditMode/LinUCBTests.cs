using System;
using NUnit.Framework;

namespace Gatekeeper.AI.Tests
{
    /// <summary>
    /// These fail until you implement LinUCBPolicy. Make them green.
    /// </summary>
    public class LinUCBTests
    {
        const float Tolerance = 1e-3f;

        [Test]
        public void FreshModelScoresEveryArmTheSame()
        {
            var policy = new LinUCBPolicy(arms: 4, dimensions: 3, alpha: 1f);
            var x = new[] { 1f, 0f, 1f };
            var scores = policy.ScoreAll(x);

            Assert.AreEqual(4, scores.Length);
            foreach (var s in scores)
            {
                Assert.AreEqual(0f, s.Mean, Tolerance);
                Assert.AreEqual(Math.Sqrt(2), s.Bonus, Tolerance); // sqrt(x^T I x)
            }
            Assert.AreEqual(0, policy.SelectArm(x), "ties break to the lowest index");
        }

        [Test]
        public void TriedArmLosesItsExplorationBonus()
        {
            var policy = new LinUCBPolicy(arms: 3, dimensions: 2, alpha: 1f);
            var x = new[] { 1f, 1f };
            for (int i = 0; i < 5; i++) policy.Update(0, x, 0f);

            Assert.AreEqual(5, policy.UpdatesSeen);
            Assert.AreNotEqual(0, policy.SelectArm(x), "an arm that keeps scoring 0 should stop being picked");
        }

        [Test]
        public void MeanAndBonusMatchRidgeRegression()
        {
            var policy = new LinUCBPolicy(arms: 1, dimensions: 3, alpha: 0.5f);
            var rng = new Random(7);
            var X = new double[12, 3];
            var y = new double[12];
            for (int n = 0; n < 12; n++)
            {
                var x = new[] { (float)rng.NextDouble(), (float)rng.NextDouble(), 1f };
                float r = (float)rng.NextDouble();
                for (int j = 0; j < 3; j++) X[n, j] = x[j];
                y[n] = r;
                policy.Update(0, x, r);
            }

            // Naive reference: A = I + X^T X, b = X^T y, theta = A^-1 b.
            var A = new double[3, 3];
            var b = new double[3];
            for (int i = 0; i < 3; i++)
            {
                A[i, i] = 1;
                for (int j = 0; j < 3; j++)
                    for (int n = 0; n < 12; n++) A[i, j] += X[n, i] * X[n, j];
                for (int n = 0; n < 12; n++) b[i] += X[n, i] * y[n];
            }
            var Ainv = Invert3(A);

            var probe = new[] { 0.3f, 0.9f, 1f };
            double mean = 0, quad = 0;
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                {
                    mean += probe[i] * Ainv[i, j] * b[j];
                    quad += probe[i] * Ainv[i, j] * probe[j];
                }

            var score = policy.ScoreAll(probe)[0];
            Assert.AreEqual(mean, score.Mean, Tolerance);
            Assert.AreEqual(0.5 * Math.Sqrt(quad), score.Bonus, Tolerance);
        }

        [Test]
        public void ContributionsSumToMean()
        {
            var policy = new LinUCBPolicy(arms: 2, dimensions: 3, alpha: 1f);
            policy.Update(1, new[] { 1f, 0f, 1f }, 0.8f);
            policy.Update(1, new[] { 0f, 1f, 1f }, 0.2f);

            var x = new[] { 1f, 1f, 1f };
            var contributions = policy.FeatureContributions(1, x);
            float sum = 0;
            foreach (var c in contributions) sum += c;
            Assert.AreEqual(policy.ScoreAll(x)[1].Mean, sum, Tolerance);
        }

        [Test]
        public void LearnsTheBestArmForEachContext()
        {
            // Context A = (1, 0, bias): arm 2 is best. Context B = (0, 1, bias): arm 0 is best.
            var contextA = new[] { 1f, 0f, 1f };
            var contextB = new[] { 0f, 1f, 1f };
            float TrueReward(int arm, float[] x) =>
                x[0] > 0 ? new[] { 0.2f, 0.3f, 0.9f, 0.1f }[arm] : new[] { 0.8f, 0.4f, 0.1f, 0.3f }[arm];

            var policy = new LinUCBPolicy(arms: 4, dimensions: 3, alpha: 0.5f);
            var rng = new Random(42);
            for (int t = 0; t < 400; t++)
            {
                var x = rng.Next(2) == 0 ? contextA : contextB;
                int arm = policy.SelectArm(x);
                float noise = (float)(rng.NextDouble() - 0.5) * 0.1f;
                policy.Update(arm, x, TrueReward(arm, x) + noise);
            }

            Assert.AreEqual(2, policy.SelectArm(contextA));
            Assert.AreEqual(0, policy.SelectArm(contextB));
        }

        [Test]
        public void ResetForgetsEverything()
        {
            var policy = new LinUCBPolicy(arms: 2, dimensions: 2, alpha: 1f);
            policy.Update(0, new[] { 1f, 1f }, 1f);
            policy.Reset();

            Assert.AreEqual(0, policy.UpdatesSeen);
            Assert.AreEqual(0f, policy.ScoreAll(new[] { 1f, 1f })[0].Mean, Tolerance);
        }

        static double[,] Invert3(double[,] m)
        {
            double det = m[0, 0] * (m[1, 1] * m[2, 2] - m[1, 2] * m[2, 1])
                       - m[0, 1] * (m[1, 0] * m[2, 2] - m[1, 2] * m[2, 0])
                       + m[0, 2] * (m[1, 0] * m[2, 1] - m[1, 1] * m[2, 0]);
            var inv = new double[3, 3];
            inv[0, 0] = (m[1, 1] * m[2, 2] - m[1, 2] * m[2, 1]) / det;
            inv[0, 1] = (m[0, 2] * m[2, 1] - m[0, 1] * m[2, 2]) / det;
            inv[0, 2] = (m[0, 1] * m[1, 2] - m[0, 2] * m[1, 1]) / det;
            inv[1, 0] = (m[1, 2] * m[2, 0] - m[1, 0] * m[2, 2]) / det;
            inv[1, 1] = (m[0, 0] * m[2, 2] - m[0, 2] * m[2, 0]) / det;
            inv[1, 2] = (m[0, 2] * m[1, 0] - m[0, 0] * m[1, 2]) / det;
            inv[2, 0] = (m[1, 0] * m[2, 1] - m[1, 1] * m[2, 0]) / det;
            inv[2, 1] = (m[0, 1] * m[2, 0] - m[0, 0] * m[2, 1]) / det;
            inv[2, 2] = (m[0, 0] * m[1, 1] - m[0, 1] * m[1, 0]) / det;
            return inv;
        }
    }
}
