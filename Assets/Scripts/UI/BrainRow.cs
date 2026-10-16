using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gatekeeper
{
    /// <summary>One plan in the brain panel: solid bar = expected reward, light bar = exploration bonus.</summary>
    public class BrainRow : MonoBehaviour
    {
        [SerializeField] TMP_Text label;
        [SerializeField] Image meanBar;
        [SerializeField] Image bonusBar;
        [SerializeField] Color normalColor = new Color(0.4f, 0.7f, 1f);
        [SerializeField] Color chosenColor = new Color(1f, 0.45f, 0.3f);

        public void Set(string planName, float mean, float bonus, float max, bool chosen)
        {
            label.text = chosen ? "<b>> " + planName + "</b>" : planName;
            float m = Mathf.Clamp01(Mathf.Max(0f, mean) / max);
            float b = Mathf.Clamp01(bonus / max);
            SetSpan(meanBar.rectTransform, 0f, m);
            SetSpan(bonusBar.rectTransform, m, Mathf.Min(1f, m + b));

            var color = chosen ? chosenColor : normalColor;
            meanBar.color = color;
            bonusBar.color = new Color(color.r, color.g, color.b, 0.35f);
        }

        static void SetSpan(RectTransform rect, float from, float to)
        {
            rect.anchorMin = new Vector2(from, 0f);
            rect.anchorMax = new Vector2(to, 1f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
