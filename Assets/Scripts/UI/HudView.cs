using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gatekeeper
{
    public class HudView : MonoBehaviour
    {
        [SerializeField] TMP_Text scoreText;
        [SerializeField] TMP_Text statsText;
        [SerializeField] Image gateFill;
        [SerializeField] TMP_Text gateText;
        [SerializeField] Image heroFill;
        [SerializeField] TMP_Text heroText;
        [SerializeField] TMP_Text phaseText;
        [SerializeField] TMP_Text infoText;

        public void Refresh(GameManager gm)
        {
            scoreText.text = "Score " + gm.Score + "    Wave " + gm.WaveNumber;
            statsText.text = "Kills " + gm.Kills + "    Survived " + gm.WavesSurvived;

            gateFill.fillAmount = gm.Gate.Health.Fraction;
            gateText.text = "Gate " + Mathf.CeilToInt(gm.Gate.Health.Current) + " / " + gm.Gate.Health.Max;
            heroFill.fillAmount = gm.Hero.Health.Fraction;
            heroText.text = "Hero " + Mathf.CeilToInt(gm.Hero.Health.Current) + " / " + gm.Hero.Health.Max;

            phaseText.text = PhaseName(gm.Phase);
            string timer = gm.Phase == GamePhase.Build || gm.Phase == GamePhase.WeaponPick
                ? "Time left " + Mathf.CeilToInt(Mathf.Max(0f, gm.PhaseTimer)) + "s\n"
                : "";
            infoText.text = timer + "Weapon: " + gm.Hero.Combat.Current.displayName +
                            "\nEnemies alive: " + EnemyRegistry.Count;
        }

        static string PhaseName(GamePhase phase)
        {
            switch (phase)
            {
                case GamePhase.Build: return "Build your defence";
                case GamePhase.WeaponPick: return "Choose a weapon";
                case GamePhase.Wave: return "Hold the gate!";
                case GamePhase.Report: return "War report";
                default: return "Game over";
            }
        }
    }
}
