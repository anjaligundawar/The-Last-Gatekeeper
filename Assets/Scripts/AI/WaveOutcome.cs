namespace Gatekeeper.AI
{
    /// <summary>What happened during one wave, measured by the game.</summary>
    public sealed class WaveOutcome
    {
        public float GateDamage;
        public float GateMaxHp;
        public float HeroDamage;
        public float HeroMaxHp;

        /// <summary>Mean over all spawned enemies of how far along their lane they got (0 = spawn, 1 = gate).</summary>
        public float AvgProgress;

        public int Spawned;
        public int Kills;
        public int ReachedGate;
    }

    /// <summary>
    /// Reward the War Council maximises, in [0, 1].
    /// r = 0.6 * gate damage share + 0.2 * hero damage share + 0.2 * average progress.
    /// Progress gives a learning signal even on waves where nothing reaches the gate.
    /// </summary>
    public static class RewardFunction
    {
        public const float GateWeight = 0.6f;
        public const float HeroWeight = 0.2f;
        public const float ProgressWeight = 0.2f;

        public static float Compute(WaveOutcome o)
        {
            float gate = o.GateMaxHp > 0 ? Clamp01(o.GateDamage / o.GateMaxHp) : 0f;
            float hero = o.HeroMaxHp > 0 ? Clamp01(o.HeroDamage / o.HeroMaxHp) : 0f;
            float progress = Clamp01(o.AvgProgress);
            return GateWeight * gate + HeroWeight * hero + ProgressWeight * progress;
        }

        static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
