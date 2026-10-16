using System;
using Gatekeeper.AI;
using UnityEngine;

namespace Gatekeeper
{
    /// <summary>
    /// Game-side wrapper around the ML policy: random warm-up waves, and a safe fallback
    /// to random plans while LinUCB is still a stub.
    /// </summary>
    public class WarCouncil
    {
        public IWavePolicy Policy { get; }
        public int LastArm { get; private set; }
        public float[] LastContext { get; private set; }
        public bool LastWasRandom { get; private set; }

        readonly RandomPolicy fallback;
        readonly int warmupWaves;
        bool warned;

        public WarCouncil(IWavePolicy policy, int warmupWaves, int seed)
        {
            Policy = policy;
            this.warmupWaves = warmupWaves;
            fallback = new RandomPolicy(PlanCatalog.Count, seed + 1);
        }

        public int Choose(float[] context, int waveNumber)
        {
            LastContext = context;
            LastWasRandom = waveNumber <= warmupWaves;
            if (!LastWasRandom)
            {
                try
                {
                    LastArm = Policy.SelectArm(context);
                    return LastArm;
                }
                catch (NotImplementedException)
                {
                    Warn();
                    LastWasRandom = true;
                }
            }
            LastArm = fallback.SelectArm(context);
            return LastArm;
        }

        public void Learn(float reward)
        {
            if (LastContext == null) return;
            try { Policy.Update(LastArm, LastContext, reward); }
            catch (NotImplementedException) { Warn(); }
        }

        void Warn()
        {
            if (warned) return;
            warned = true;
            Debug.LogWarning("[WarCouncil] " + Policy.Name + " is not implemented yet. Falling back to random plans. " +
                             "Implement Assets/Scripts/AI/Policies/LinUCBPolicy.cs and run the EditMode tests.");
        }
    }
}
