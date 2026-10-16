using System;

namespace Gatekeeper.AI
{
    /// <summary>
    /// Disjoint LinUCB contextual bandit (Li et al., 2010, Algorithm 1). YOUR PART.
    ///
    /// Each arm a keeps:
    ///   A_a   : d x d matrix, starts as the identity
    ///   b_a   : d vector,     starts as zeros
    ///
    /// Estimate: theta_a = A_a^-1 b_a
    /// Score:    p_a = theta_a . x + alpha * sqrt(x^T A_a^-1 x)
    /// Select:   argmax_a p_a (break ties by lowest index)
    /// Update:   A_a += x x^T,   b_a += r x
    ///
    /// Tip: store A_a^-1 directly and update it with Sherman-Morrison so you never invert:
    ///   A^-1 <- A^-1 - (A^-1 x)(A^-1 x)^T / (1 + x^T A^-1 x)
    ///
    /// The tests in Assets/Tests/EditMode/LinUCBTests.cs define "done".
    /// Run them with Window > General > Test Runner > EditMode.
    /// Until this is implemented the game falls back to random plans and logs a warning.
    /// </summary>
    public sealed class LinUCBPolicy : IExplainablePolicy
    {
        readonly int arms;
        readonly int dimensions;
        readonly float alpha;

        public LinUCBPolicy(int arms, int dimensions, float alpha = 1f)
        {
            this.arms = arms;
            this.dimensions = dimensions;
            this.alpha = alpha;
            Reset();
        }

        public string Name => "LinUCB";

        public int UpdatesSeen { get; private set; }

        public void Reset()
        {
            UpdatesSeen = 0;
            // TODO: allocate A_a^-1 (identity) and b_a (zeros) for every arm.
        }

        public int SelectArm(float[] context)
        {
            // TODO: return the arm with the highest ScoreAll(context)[a].Total.
            throw new NotImplementedException("LinUCBPolicy.SelectArm");
        }

        public void Update(int arm, float[] context, float reward)
        {
            // TODO: Sherman-Morrison update of A_arm^-1, then b_arm += reward * x.
            throw new NotImplementedException("LinUCBPolicy.Update");
        }

        public ArmScore[] ScoreAll(float[] context)
        {
            // TODO: for each arm compute Mean = theta_a . x and Bonus = alpha * sqrt(x^T A_a^-1 x).
            throw new NotImplementedException("LinUCBPolicy.ScoreAll");
        }

        public float[] FeatureContributions(int arm, float[] context)
        {
            // TODO: return theta_arm[i] * x[i] for every feature i.
            throw new NotImplementedException("LinUCBPolicy.FeatureContributions");
        }
    }
}
