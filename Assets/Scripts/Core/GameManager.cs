using System;
using Gatekeeper.AI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Gatekeeper
{
    /// <summary>
    /// The game's top-level state machine:
    /// Build (place defences) -> WeaponPick -> Wave (fight) -> Report (war report) -> Build ...
    /// Any wave can end in GameOver when the gate or the hero falls.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        [SerializeField] Hero hero;
        [SerializeField] Gate gate;
        [SerializeField] BuildManager build;
        [SerializeField] WaveSpawner spawner;
        [SerializeField] CameraShake cameraShake;

        public static GameManager Instance { get; private set; }

        public GameConfig Config => config;
        public GamePhase Phase { get; private set; }
        public int WaveNumber { get; private set; } = 1;
        public int WavesSurvived { get; private set; }
        public int Kills { get; private set; }
        public int Score => Kills * 10 + WavesSurvived * 100;
        public float PhaseTimer { get; private set; }
        public float WaveElapsed { get; private set; }

        public Hero Hero => hero;
        public Gate Gate => gate;
        public BuildManager Build => build;
        public LanePath[] Lanes => spawner.Lanes;
        public WarCouncil Council { get; private set; }

        public DefenseSnapshot CurrentSnapshot { get; private set; }
        public float[] CurrentContext { get; private set; }
        public int CurrentArm { get; private set; }
        public string IntentText { get; private set; }
        public float IntentTimer { get; private set; }
        public string ReportText { get; private set; }
        public float LastReward { get; private set; }
        public string GameOverReason { get; private set; }

        /// <summary>Raised when entering a phase. Used by bot mode.</summary>
        public event Action<GamePhase> PhaseEntered;

        /// <summary>Who is playing this run (null = human). Set by bot mode.</summary>
        public string BotPersona { get; set; }

        TelemetryLogger telemetry;
        System.Random rng;
        float[] lastLaneTime = new float[MapConstants.LaneCount];
        float gateHpAtWaveStart;
        float heroHpAtWaveStart;
        int killsAtWaveStart;

        void Awake()
        {
            Instance = this;
            rng = new System.Random(config.seed == 0 ? Environment.TickCount : config.seed);
            Council = new WarCouncil(PolicyStore.Get(config), config.randomWarmupWaves, config.seed);
            if (config.logTelemetry) telemetry = new TelemetryLogger(config.telemetryFile);
        }

        void OnEnable() => Enemy.Killed += OnEnemyKilled;
        void OnDisable() => Enemy.Killed -= OnEnemyKilled;

        void Start() => EnterBuild();

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void OnEnemyKilled(Enemy enemy) => Kills++;

        public void Shake(float amount, float duration)
        {
            if (cameraShake != null) cameraShake.Shake(amount, duration);
        }

        // ---- Phase transitions ----------------------------------------------

        void SetPhase(GamePhase phase)
        {
            Phase = phase;
            PhaseEntered?.Invoke(phase);
        }

        void EnterBuild()
        {
            build.ClearAll();
            PhaseTimer = config.buildTime;
            SetPhase(GamePhase.Build);
        }

        public void FinishBuild()
        {
            if (Phase != GamePhase.Build) return;
            PhaseTimer = config.weaponPickTime;
            SetPhase(GamePhase.WeaponPick);
        }

        public void PickWeapon(WeaponType weapon)
        {
            if (Phase != GamePhase.WeaponPick) return;
            hero.Combat.Weapon = weapon;
            EnterWave();
        }

        void EnterWave()
        {
            CurrentSnapshot = new DefenseSnapshot { Weapon = hero.Combat.Weapon };
            build.WriteSnapshot(CurrentSnapshot);
            Array.Copy(lastLaneTime, CurrentSnapshot.HeroLaneTime, lastLaneTime.Length);

            CurrentContext = ContextEncoder.Encode(CurrentSnapshot);
            CurrentArm = Council.Choose(CurrentContext, WaveNumber);
            var plan = PlanCatalog.FromArm(CurrentArm);
            int budget = config.startBudget + config.budgetPerWave * (WaveNumber - 1);
            float hpScale = 1f + config.hpScalePerWave * (WaveNumber - 1);
            spawner.Begin(WaveBuilder.Build(plan, budget, spawner.CostOf, rng), hpScale);

            IntentText = WarReportWriter.Intent(CurrentArm, CurrentSnapshot, CurrentContext, Council.Policy);
            IntentTimer = config.intentBannerTime;
            gateHpAtWaveStart = gate.Health.Current;
            heroHpAtWaveStart = hero.Health.Current;
            killsAtWaveStart = Kills;
            WaveElapsed = 0f;
            hero.ConsumeLaneTime();
            SetPhase(GamePhase.Wave);
        }

        void EndWave()
        {
            bool gameOver = gate.Health.IsDead || hero.Health.IsDead;
            float avgProgress = spawner.Finish();
            var outcome = new WaveOutcome
            {
                GateDamage = gateHpAtWaveStart - gate.Health.Current,
                GateMaxHp = gate.Health.Max,
                HeroDamage = heroHpAtWaveStart - hero.Health.Current,
                HeroMaxHp = hero.Health.Max,
                AvgProgress = avgProgress,
                Spawned = spawner.Spawned,
                Kills = Kills - killsAtWaveStart,
                ReachedGate = spawner.ReachedGate,
            };

            LastReward = RewardFunction.Compute(outcome);
            Council.Learn(LastReward);
            telemetry?.LogWave(WaveNumber, Council.Policy.Name, Council.LastWasRandom, CurrentArm, LastReward, outcome, CurrentContext, BotPersona);
            lastLaneTime = hero.ConsumeLaneTime();
            ReportText = WarReportWriter.Debrief(CurrentArm, outcome, LastReward, CurrentContext, Council.Policy);

            if (gameOver)
            {
                GameOverReason = gate.Health.IsDead ? "The gate has fallen." : "The Gatekeeper has fallen.";
                SetPhase(GamePhase.GameOver);
                return;
            }

            WavesSurvived++;
            SetPhase(GamePhase.Report);
        }

        public void ContinueAfterReport()
        {
            if (Phase != GamePhase.Report) return;
            WaveNumber++;
            hero.Health.Heal(hero.Health.Max * config.heroHealBetweenWaves);
            EnterBuild();
        }

        public void Restart()
        {
            if (!config.keepPolicyOnRestart) PolicyStore.Reset();
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        // ---- Per-frame -----------------------------------------------------

        void Update()
        {
            switch (Phase)
            {
                case GamePhase.Build:
                    PhaseTimer -= Time.deltaTime;
                    if (PhaseTimer <= 0f || Pressed(Keyboard.current?.enterKey)) FinishBuild();
                    break;

                case GamePhase.WeaponPick:
                    PhaseTimer -= Time.deltaTime;
                    var k = Keyboard.current;
                    if (Pressed(k?.digit1Key)) PickWeapon(WeaponType.Bow);
                    else if (Pressed(k?.digit2Key)) PickWeapon(WeaponType.Sword);
                    else if (Pressed(k?.digit3Key)) PickWeapon(WeaponType.Spear);
                    else if (PhaseTimer <= 0f) PickWeapon(hero.Combat.Weapon);
                    break;

                case GamePhase.Wave:
                    WaveElapsed += Time.deltaTime;
                    if (IntentTimer > 0f) IntentTimer -= Time.deltaTime;
                    if (gate.Health.IsDead || hero.Health.IsDead || spawner.IsComplete || WaveElapsed >= config.waveTimeout)
                        EndWave();
                    break;

                case GamePhase.Report:
                    if (Pressed(Keyboard.current?.enterKey) || Pressed(Keyboard.current?.spaceKey)) ContinueAfterReport();
                    break;

                case GamePhase.GameOver:
                    if (Pressed(Keyboard.current?.rKey)) Restart();
                    break;
            }
        }

        static bool Pressed(UnityEngine.InputSystem.Controls.ButtonControl key) => key != null && key.wasPressedThisFrame;
    }
}
