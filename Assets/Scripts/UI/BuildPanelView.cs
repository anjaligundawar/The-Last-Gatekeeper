using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gatekeeper
{
    public class BuildPanelView : MonoBehaviour
    {
        [Tooltip("Indexed by TowerType: Archer, Cannon, Frost.")]
        [SerializeField] Button[] towerButtons = new Button[3];
        [SerializeField] TMP_Text[] towerLabels = new TMP_Text[3];
        [SerializeField] TMP_Text countText;
        [SerializeField] Button readyButton;
        [SerializeField] Color normalColor = new Color(0.22f, 0.24f, 0.28f);
        [SerializeField] Color selectedColor = new Color(0.45f, 0.5f, 0.62f);

        void Start()
        {
            for (int i = 0; i < towerButtons.Length; i++)
            {
                var type = (TowerType)i;
                towerButtons[i].onClick.AddListener(() => GameManager.Instance.Build.Selected = type);
            }
            readyButton.onClick.AddListener(() => GameManager.Instance.FinishBuild());
        }

        public void Refresh(GameManager gm)
        {
            var build = gm.Build;
            for (int i = 0; i < towerButtons.Length; i++)
            {
                var type = (TowerType)i;
                var data = build.DataFor(type);
                towerLabels[i].text = "<b>[" + (i + 1) + "] " + data.displayName + "</b>  " + build.Count(type) + "/" + build.MaxPerType +
                                      "\n<size=75%><color=#" + ColorUtility.ToHtmlStringRGB(data.uiColor) + ">" + data.description + "</color></size>";
                towerButtons[i].interactable = build.CanPlace(type) || build.Selected == type;
                towerButtons[i].image.color = build.Selected == type ? selectedColor : normalColor;
            }
            countText.text = "Defences " + build.Total + "/" + build.MaxTotal +
                             "\n<size=70%>Left-click a spot to place, right-click to remove</size>";
        }
    }
}
