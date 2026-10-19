using UnityEngine;

namespace Gatekeeper
{
    /// <summary>Shows the panel that matches the current game phase.</summary>
    public class UIManager : MonoBehaviour
    {
        [SerializeField] TitleView titlePanel;
        [SerializeField] HudView hud;
        [SerializeField] BuildPanelView buildPanel;
        [SerializeField] WeaponPanelView weaponPanel;
        [SerializeField] IntentBannerView intentBanner;
        [SerializeField] ReportView reportPanel;
        [SerializeField] GameOverView gameOverPanel;

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            var phase = gm.Phase;
            Show(titlePanel, phase == GamePhase.Title);
            Show(hud, phase != GamePhase.Title);
            Show(buildPanel, phase == GamePhase.Build);
            Show(weaponPanel, phase == GamePhase.WeaponPick);
            Show(intentBanner, phase == GamePhase.Wave && gm.IntentTimer > 0f);
            Show(reportPanel, phase == GamePhase.Report);
            Show(gameOverPanel, phase == GamePhase.GameOver);

            if (hud.gameObject.activeSelf) hud.Refresh(gm);
            if (buildPanel.gameObject.activeSelf) buildPanel.Refresh(gm);
            if (weaponPanel.gameObject.activeSelf) weaponPanel.Refresh(gm);
            if (intentBanner.gameObject.activeSelf) intentBanner.Refresh(gm);
            if (reportPanel.gameObject.activeSelf) reportPanel.Refresh(gm);
            if (gameOverPanel.gameObject.activeSelf) gameOverPanel.Refresh(gm);
        }

        static void Show(Component panel, bool visible)
        {
            if (panel != null && panel.gameObject.activeSelf != visible) panel.gameObject.SetActive(visible);
        }
    }
}
