using UnityEngine;
using UnityEngine.InputSystem;

namespace Gatekeeper
{
    /// <summary>Shows the panel that matches the current game phase. Tab toggles the AI brain panel.</summary>
    public class UIManager : MonoBehaviour
    {
        [SerializeField] HudView hud;
        [SerializeField] BuildPanelView buildPanel;
        [SerializeField] WeaponPanelView weaponPanel;
        [SerializeField] IntentBannerView intentBanner;
        [SerializeField] ReportView reportPanel;
        [SerializeField] GameOverView gameOverPanel;
        [SerializeField] BrainPanelView brainPanel;

        bool showBrain;

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame) showBrain = !showBrain;

            var phase = gm.Phase;
            Show(buildPanel, phase == GamePhase.Build);
            Show(weaponPanel, phase == GamePhase.WeaponPick);
            Show(intentBanner, phase == GamePhase.Wave && gm.IntentTimer > 0f);
            Show(reportPanel, phase == GamePhase.Report);
            Show(gameOverPanel, phase == GamePhase.GameOver);
            Show(brainPanel, showBrain);

            hud.Refresh(gm);
            if (buildPanel.gameObject.activeSelf) buildPanel.Refresh(gm);
            if (weaponPanel.gameObject.activeSelf) weaponPanel.Refresh(gm);
            if (intentBanner.gameObject.activeSelf) intentBanner.Refresh(gm);
            if (reportPanel.gameObject.activeSelf) reportPanel.Refresh(gm);
            if (gameOverPanel.gameObject.activeSelf) gameOverPanel.Refresh(gm);
            brainPanel.Refresh(gm, showBrain);
        }

        static void Show(Component panel, bool visible)
        {
            if (panel.gameObject.activeSelf != visible) panel.gameObject.SetActive(visible);
        }
    }
}
