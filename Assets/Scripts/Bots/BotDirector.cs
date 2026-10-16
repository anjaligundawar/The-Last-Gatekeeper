using UnityEngine;

namespace Gatekeeper
{
    /// <summary>
    /// Evaluation mode (GameConfig.botMode): bots play run after run at high speed while
    /// telemetry records every wave. Compare policies by running once per policy, then plot the CSV.
    /// </summary>
    public class BotDirector : MonoBehaviour
    {
        [SerializeField] GameManager game;
        [SerializeField] BotPersona[] personas;

        static int runsCompleted;

        BotPersona persona;
        GamePhase lastPhase = (GamePhase)(-1);
        System.Random rng;

        void Start()
        {
            if (!game.Config.botMode || personas == null || personas.Length == 0)
            {
                enabled = false;
                return;
            }

            if (runsCompleted >= game.Config.botRuns)
            {
                Debug.Log("[Bots] Finished " + runsCompleted + " runs. Telemetry: " + Application.persistentDataPath);
                Time.timeScale = 0f;
                enabled = false;
                return;
            }

            persona = personas[runsCompleted % personas.Length];
            rng = new System.Random(runsCompleted * 7919 + 1);
            game.BotPersona = persona.name;
            game.Hero.Input = new BotHeroInput(game.Hero, persona.favouriteLane);
            Time.timeScale = game.Config.botTimeScale;
        }

        void Update()
        {
            if (game.Phase == lastPhase) return;
            lastPhase = game.Phase;

            switch (game.Phase)
            {
                case GamePhase.Build:
                    PlaceDefences();
                    game.FinishBuild();
                    break;
                case GamePhase.WeaponPick:
                    game.PickWeapon(persona.weapon);
                    break;
                case GamePhase.Report:
                    if (game.WavesSurvived >= game.Config.botWavesPerRun) EndRun();
                    else game.ContinueAfterReport();
                    break;
                case GamePhase.GameOver:
                    EndRun();
                    break;
            }
        }

        void PlaceDefences()
        {
            var build = game.Build;
            foreach (var placement in persona.placements)
            {
                int spot = placement.spot;
                var tower = placement.tower;
                if (rng.NextDouble() < persona.noise)
                {
                    spot = rng.Next(MapConstants.SpotCount);
                    tower = (TowerType)rng.Next(MapConstants.TowerTypeCount);
                }
                foreach (var s in build.Spots)
                    if (s.Index == spot) build.TryPlace(s, tower);
            }
        }

        void EndRun()
        {
            runsCompleted++;
            game.Restart();
        }
    }
}
