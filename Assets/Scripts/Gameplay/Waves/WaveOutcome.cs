namespace Gatekeeper
{
    /// <summary>What happened during one wave, shown on the war report.</summary>
    public sealed class WaveOutcome
    {
        public float GateDamage;
        public float GateMaxHp;
        public float HeroDamage;
        public float HeroMaxHp;
        public int Spawned;
        public int Kills;
        public int ReachedGate;
    }
}
