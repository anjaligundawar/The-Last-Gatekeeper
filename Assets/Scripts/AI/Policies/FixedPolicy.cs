namespace Gatekeeper.AI
{
    /// <summary>Baseline: always sends the same plan, like a scripted AI.</summary>
    public sealed class FixedPolicy : IWavePolicy
    {
        readonly int arm;

        public FixedPolicy(int arm) => this.arm = arm;

        public string Name => "Fixed(" + PlanCatalog.Describe(arm) + ")";
        public int SelectArm(float[] context) => arm;
        public void Update(int arm, float[] context, float reward) { }
        public void Reset() { }
    }
}
