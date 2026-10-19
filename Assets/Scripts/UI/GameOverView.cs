using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gatekeeper
{
    /// <summary>Death screen. Fades in over the battlefield after the hero or the gate falls.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class GameOverView : MonoBehaviour
    {
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text summaryText;
        [SerializeField] TMP_Text statsText;
        [SerializeField] TMP_Text reportText;
        [SerializeField] Button restartButton;
        [SerializeField] Button menuButton;

        [Tooltip("Seconds to wait before the screen starts fading in, so the player sees the final blow.")]
        [SerializeField] float delay = 0.6f;
        [SerializeField] float fadeTime = 0.5f;

        CanvasGroup group;
        float timer;

        void Awake() => group = GetComponent<CanvasGroup>();

        void Start()
        {
            restartButton.onClick.AddListener(() => GameManager.Instance.Restart());
            menuButton.onClick.AddListener(() => GameManager.Instance.BackToTitle());
        }

        void OnEnable()
        {
            timer = 0f;
            if (group == null) group = GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = group.blocksRaycasts = false;
        }

        void Update()
        {
            timer += Time.unscaledDeltaTime;
            group.alpha = Mathf.Clamp01((timer - delay) / fadeTime);
            group.interactable = group.blocksRaycasts = group.alpha >= 1f;
        }

        public void Refresh(GameManager gm)
        {
            titleText.text = gm.HeroDied ? "You have fallen" : "The gate has fallen";
            summaryText.text = gm.HeroDied
                ? "The Gatekeeper is down, and the gate stands alone."
                : "The War Council's army broke through.";
            statsText.text = "<b>Score " + gm.Score + "</b>     Waves survived " + gm.WavesSurvived + "     Kills " + gm.Kills;
            reportText.text = "<b>Last wave</b>\n" + gm.ReportText;
        }
    }
}
