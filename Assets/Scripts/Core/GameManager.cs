using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Gatekeeper
{
    /// <summary>
    /// The game's top-level state machine:
    /// Title (start screen) -> Build (place defences) -> WeaponPick -> Wave (fight) -> Report (war report) -> Build ...
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
        public AttackPlan CurrentPlan { get; private set; }
        public string IntentText { get; private set; }
        public float IntentTimer { get; private set; }
        public string ReportText { get; private set; }
        public string GameOverReason { get; private set; }
        public bool HeroDied { get; private set; }

        // Set by Restart so the reloaded scene skips the start screen.
        static bool skipTitle;

        // The key press that changed the phase must not also act on the new phase.
        int phaseStartFrame = -1;

        System.Random rng;
        float gateHpAtWaveStart;
        float heroHpAtWaveStart;
        int killsAtWaveStart;

        void Awake()
        {
            Instance = this;
            rng = new System.Random(config.seed == 0 ? Environment.TickCount : config.seed);
        }

        void OnEnable() => Enemy.Killed += OnEnemyKilled;
        void OnDisable() => Enemy.Killed -= OnEnemyKilled;

        void Start()
        {
            if (skipTitle)
            {
                skipTitle = false;
                EnterBuild();
            }
            else SetPhase(GamePhase.Title);
        }

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
            phaseStartFrame = Time.frameCount;
        }

        public void StartGame()
        {
            if (Phase == GamePhase.Title) EnterBuild();
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
            // The War Council picks a random plan for now.
            CurrentPlan = PlanCatalog.FromIndex(rng.Next(PlanCatalog.Count));
            int budget = config.startBudget + config.budgetPerWave * (WaveNumber - 1);
            float hpScale = 1f + config.hpScalePerWave * (WaveNumber - 1);
            spawner.Begin(WaveBuilder.Build(CurrentPlan, budget, spawner.CostOf, rng), hpScale);

            IntentText = IntentFor(CurrentPlan);
            IntentTimer = config.intentBannerTime;
            gateHpAtWaveStart = gate.Health.Current;
            heroHpAtWaveStart = hero.Health.Current;
            killsAtWaveStart = Kills;
            WaveElapsed = 0f;
            SetPhase(GamePhase.Wave);
        }

        void EndWave()
        {
            bool gameOver = gate.Health.IsDead || hero.Health.IsDead;
            spawner.Finish();
            var outcome = new WaveOutcome
            {
                GateDamage = gateHpAtWaveStart - gate.Health.Current,
                GateMaxHp = gate.Health.Max,
                HeroDamage = heroHpAtWaveStart - hero.Health.Current,
                HeroMaxHp = hero.Health.Max,
                Spawned = spawner.Spawned,
                Kills = Kills - killsAtWaveStart,
                ReachedGate = spawner.ReachedGate,
            };

            ReportText = ReportFor(CurrentPlan, outcome);

            if (gameOver)
            {
                HeroDied = hero.Health.IsDead;
                GameOverReason = HeroDied ? "The Gatekeeper has fallen." : "The gate has fallen.";
                Shake(0.3f, 0.4f);
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

        /// <summary>Starts a new run straight away, skipping the start screen.</summary>
        public void Restart()
        {
            skipTitle = true;
            ReloadScene();
        }

        /// <summary>Goes back to the start screen.</summary>
        public void BackToTitle()
        {
            skipTitle = false;
            ReloadScene();
        }

        static void ReloadScene()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        // ---- Per-frame -----------------------------------------------------

        void Update()
        {
            if (Time.frameCount == phaseStartFrame) return;

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
                    else if (Pressed(Keyboard.current?.mKey)) BackToTitle();
                    break;
            }
        }

        // ---- War report text -------------------------------------------------

        static string IntentFor(AttackPlan plan)
        {
            string target = plan.Focus == LaneFocus.Even
                ? " across every lane."
                : " at your " + MapConstants.LaneNames[(int)plan.Focus] + " lane.";
            return "The War Council sends a " + PlanCatalog.ArmyName(plan.Army) + target;
        }

        static string ReportFor(AttackPlan plan, WaveOutcome o) =>
            "Plan used: " + plan + "\n" +
            "Gate damage: " + Percent(o.GateDamage, o.GateMaxHp) + "   Hero damage: " + Percent(o.HeroDamage, o.HeroMaxHp) + "\n" +
            "Kills: " + o.Kills + "   Reached the gate: " + o.ReachedGate + " of " + o.Spawned;

        static string Percent(float value, float max) =>
            max <= 0f ? "0%" : Mathf.RoundToInt(100f * value / max) + "%";

        static bool Pressed(UnityEngine.InputSystem.Controls.ButtonControl key) => key != null && key.wasPressedThisFrame;
    }
}
