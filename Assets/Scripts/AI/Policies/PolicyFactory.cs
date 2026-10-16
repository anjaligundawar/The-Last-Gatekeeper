namespace Gatekeeper.AI
{
    public enum PolicyKind
    {
        Random,
        Fixed,
        LinUCB,
    }

    public static class PolicyFactory
    {
        public static IWavePolicy Create(PolicyKind kind, float alpha = 1f, int fixedArm = 0, int seed = 0)
        {
            switch (kind)
            {
                case PolicyKind.Fixed: return new FixedPolicy(fixedArm);
                case PolicyKind.LinUCB: return new LinUCBPolicy(PlanCatalog.Count, ContextEncoder.Dimensions, alpha);
                default: return new RandomPolicy(PlanCatalog.Count, seed);
            }
        }
    }
}
