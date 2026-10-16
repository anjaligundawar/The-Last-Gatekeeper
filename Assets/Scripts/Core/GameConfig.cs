using Gatekeeper.AI;
using UnityEngine;

namespace Gatekeeper
{
    /// <summary>Game-wide tunables. Lives at Assets/Data/GameConfig.asset.</summary>
    [CreateAssetMenu(menuName = "Gatekeeper/Game Config", fileName = "GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Phases")]
        public float buildTime = 30f;
        public float weaponPickTime = 10f;
        public float waveTimeout = 120f;
        public float intentBannerTime = 4f;

        [Header("Defences")]
        public int maxDefences = 6;
        public int maxPerType = 3;

        [Header("Between waves")]
        [Range(0f, 1f)] public float heroHealBetweenWaves = 0.5f;

        [Header("Enemy scaling")]
        public int startBudget = 10;
        public int budgetPerWave = 4;
        public float hpScalePerWave = 0.08f;
        public float spawnInterval = 0.6f;

        [Header("War Council (ML)")]
        public PolicyKind policy = PolicyKind.Random;
        public float linUcbAlpha = 1f;
        [Tooltip("Arm used by the Fixed policy (0..15).")]
        public int fixedArm;
        [Tooltip("Waves at the start of a run that use random plans to gather data.")]
        public int randomWarmupWaves = 2;
        [Tooltip("Keep what the Council learned when the player restarts.")]
        public bool keepPolicyOnRestart = true;
        [Tooltip("0 = random seed each run.")]
        public int seed;

        [Header("Telemetry")]
        public bool logTelemetry = true;
        public string telemetryFile = "telemetry.csv";

        [Header("Bot mode (evaluation)")]
        public bool botMode;
        public float botTimeScale = 10f;
        public int botRuns = 20;
        public int botWavesPerRun = 15;
    }
}
