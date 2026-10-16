using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gatekeeper
{
    public class WeaponPanelView : MonoBehaviour
    {
        [Tooltip("Indexed by WeaponType: Bow, Sword, Spear.")]
        [SerializeField] Button[] weaponButtons = new Button[3];
        [SerializeField] TMP_Text[] weaponLabels = new TMP_Text[3];

        void Start()
        {
            for (int i = 0; i < weaponButtons.Length; i++)
            {
                var type = (WeaponType)i;
                weaponButtons[i].onClick.AddListener(() => GameManager.Instance.PickWeapon(type));
            }
        }

        public void Refresh(GameManager gm)
        {
            for (int i = 0; i < weaponLabels.Length; i++)
            {
                var data = gm.Hero.Combat.Get((WeaponType)i);
                weaponLabels[i].text = "<b>[" + (i + 1) + "] " + data.displayName + "</b>\n<size=75%>" + data.description + "</size>";
            }
        }
    }
}
