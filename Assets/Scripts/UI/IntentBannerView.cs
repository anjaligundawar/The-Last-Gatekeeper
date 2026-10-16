using TMPro;
using UnityEngine;

namespace Gatekeeper
{
    public class IntentBannerView : MonoBehaviour
    {
        [SerializeField] TMP_Text text;

        public void Refresh(GameManager gm) => text.text = gm.IntentText;
    }
}
