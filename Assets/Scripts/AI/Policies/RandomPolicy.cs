using System;

namespace Gatekeeper.AI
{
    /// <summary>Baseline: picks a uniformly random plan every wave.</summary>
    public sealed class RandomPolicy : IWavePolicy
    {
        readonly int arms;
        readonly int seed;
        Random rng;

        public RandomPolicy(int arms = PlanCatalog.Count, int seed = 0)
        {
            this.arms = arms;
            this.seed = seed;
            rng = new Random(seed);
        }

        public string Name => "Random";
        public int SelectArm(float[] context) => rng.Next(arms);
        public void Update(int arm, float[] context, float reward) { }
        public void Reset() => rng = new Random(seed);
    }
}
