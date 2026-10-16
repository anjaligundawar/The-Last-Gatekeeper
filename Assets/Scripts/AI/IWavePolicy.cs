namespace Gatekeeper.AI
{
    /// <summary>A strategy for choosing which attack plan (arm) to send against a context.</summary>
    public interface IWavePolicy
    {
        string Name { get; }

        /// <summary>Pick an arm in [0, PlanCatalog.Count) for this context.</summary>
        int SelectArm(float[] context);

        /// <summary>Learn from the reward the chosen arm earned in this context.</summary>
        void Update(int arm, float[] context, float reward);

        /// <summary>Forget everything learned.</summary>
        void Reset();
    }

    public struct ArmScore
    {
        /// <summary>Expected reward, theta_a . x</summary>
        public float Mean;

        /// <summary>Exploration bonus, alpha * sqrt(x^T A_a^-1 x)</summary>
        public float Bonus;

        public float Total => Mean + Bonus;
    }

    /// <summary>
    /// A policy that can show its reasoning. The brain panel and the war report light up
    /// automatically for any policy that implements this.
    /// </summary>
    public interface IExplainablePolicy : IWavePolicy
    {
        /// <summary>How many updates (waves) the model has learned from.</summary>
        int UpdatesSeen { get; }

        /// <summary>Score every arm for this context.</summary>
        ArmScore[] ScoreAll(float[] context);

        /// <summary>Per-feature contribution to the arm's mean: theta_a[i] * x[i]. Sums to the mean.</summary>
        float[] FeatureContributions(int arm, float[] context);
    }
}
