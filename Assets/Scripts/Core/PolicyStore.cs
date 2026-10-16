using Gatekeeper.AI;

namespace Gatekeeper
{
    /// <summary>
    /// Holds the War Council's policy across scene reloads, so the enemy can remember
    /// the player between restarts (see GameConfig.keepPolicyOnRestart).
    /// </summary>
    public static class PolicyStore
    {
        static IWavePolicy current;
        static PolicyKind currentKind;

        public static IWavePolicy Get(GameConfig config)
        {
            if (current == null || currentKind != config.policy)
            {
                current = PolicyFactory.Create(config.policy, config.linUcbAlpha, config.fixedArm, config.seed);
                currentKind = config.policy;
            }
            return current;
        }

        public static void Reset() => current?.Reset();
    }
}
