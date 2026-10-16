using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gatekeeper
{
    public class GameOverView : MonoBehaviour
    {
        [SerializeField] TMP_Text summaryText;
        [SerializeField] TMP_Text reportText;
        [SerializeField] Button restartButton;

        void Start() => restartButton.onClick.AddListener(() => GameManager.Instance.Restart());

        public void Refresh(GameManager gm)
        {
            summaryText.text = gm.GameOverReason + "\n<b>Score " + gm.Score + "</b>  (waves survived " + gm.WavesSurvived + ", kills " + gm.Kills + ")";
            reportText.text = gm.ReportText;
        }
    }
}
