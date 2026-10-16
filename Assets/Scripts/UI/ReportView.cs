using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gatekeeper
{
    public class ReportView : MonoBehaviour
    {
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text bodyText;
        [SerializeField] Button continueButton;

        void Start() => continueButton.onClick.AddListener(() => GameManager.Instance.ContinueAfterReport());

        public void Refresh(GameManager gm)
        {
            titleText.text = "War report: wave " + gm.WaveNumber;
            bodyText.text = gm.ReportText;
        }
    }
}
